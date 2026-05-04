using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using SyncCtx = System.Threading.SynchronizationContext;

namespace VaSiLi.Janus
{
    public class JanusReconnectOptions
    {
        public bool Enabled = true;
        public int InitialBackoffMs = 500;
        public int MaxBackoffMs = 10000;
        public double BackoffFactor = 2.0;
        public int MaxAttempts = 0; // 0 = forever
    }

    public class JanusConnectionOptions
    {
        public int KeepaliveIntervalMs = 25000;
        public JanusReconnectOptions Reconnect = new JanusReconnectOptions();
        public string Token;
        public string ApiSecret;
    }

    public class JanusConnection : IDisposable
    {
        public IJanusTransport Transport { get; }
        public JanusConnectionOptions Options { get; }
        public bool IsConnected => Transport.State == JanusTransportState.Open;

        public event EventHandler Connected;
        public event EventHandler<string> Disconnected;
        public event EventHandler Reconnecting;
        public event EventHandler Reconnected;
        public event EventHandler<string> ReconnectFailed;
        public event EventHandler<Exception> InternalError;

        readonly TransactionRegistry _transactions = new TransactionRegistry();
        readonly Dictionary<long, JanusSession> _sessions = new Dictionary<long, JanusSession>();
        readonly object _sessionsLock = new object();
        readonly SyncCtx _eventContext;

        CancellationTokenSource _keepaliveCts;
        bool _disposed;
        bool _userClosed;

        public JanusConnection(IJanusTransport transport, JanusConnectionOptions options = null)
        {
            Transport = transport ?? throw new ArgumentNullException(nameof(transport));
            Options = options ?? new JanusConnectionOptions();
            _eventContext = SyncCtx.Current;

            Transport.Message += HandleMessage;
            Transport.Closed += HandleClosed;
            Transport.Error += HandleError;
        }

        /// <summary>
        /// Posts the action to the SynchronizationContext captured when this connection was constructed
        /// (Unity main thread, when constructed from a MonoBehaviour). Falls back to direct invocation.
        /// </summary>
        internal void Dispatch(Action action)
        {
            if (action == null) return;
            if (_eventContext != null && _eventContext != SyncCtx.Current)
                _eventContext.Post(_ => action(), null);
            else
                action();
        }

        public async Task OpenAsync(CancellationToken cancellation = default)
        {
            await Transport.OpenAsync(cancellation);
            StartKeepalive();
            Dispatch(() => Connected?.Invoke(this, EventArgs.Empty));
        }

        public async Task<JanusSession> CreateSessionAsync(CancellationToken cancellation = default)
        {
            var tx = JanusProtocol.NewTransactionId();
            var task = _transactions.Track(tx);
            await SendRawAsync(new JanusRequest { Janus = JanusVerbs.Create, Transaction = tx }, cancellation);
            var env = await task.ConfigureAwait(false);
            if (env.Janus != JanusVerbs.Success || env.Data == null)
                throw new JanusException("Failed to create session: " + env.Reason);
            var sessionId = env.Data["id"]?.Value<long>() ?? 0;
            var session = new JanusSession(this, sessionId);
            lock (_sessionsLock) _sessions[sessionId] = session;
            return session;
        }

        public async Task CloseAsync()
        {
            _userClosed = true;
            StopKeepalive();
            JanusSession[] sessions;
            lock (_sessionsLock)
            {
                sessions = new JanusSession[_sessions.Count];
                _sessions.Values.CopyTo(sessions, 0);
            }
            foreach (var s in sessions)
            {
                try { await s.DestroyAsync().ConfigureAwait(false); }
                catch (Exception ex)
                {
                    var captured = ex;
                    Dispatch(() => InternalError?.Invoke(this, captured));
                }
            }
            await Transport.CloseAsync().ConfigureAwait(false);
        }

        internal async Task<JanusEnvelope> SendRequestAsync(JanusRequest request, CancellationToken cancellation = default)
        {
            if (string.IsNullOrEmpty(request.Transaction))
                request.Transaction = JanusProtocol.NewTransactionId();
            var task = _transactions.Track(request.Transaction);
            await SendRawAsync(request, cancellation);
            using (cancellation.Register(() => _transactions.TryFail(request.Transaction, new OperationCanceledException(cancellation))))
            {
                return await task.ConfigureAwait(false);
            }
        }

        internal Task SendRawAsync(JanusRequest request, CancellationToken cancellation = default)
        {
            var json = JsonConvert.SerializeObject(request, JanusProtocol.JsonSettings);
            return Transport.SendAsync(json, cancellation);
        }

        internal void RemoveSession(long sessionId)
        {
            lock (_sessionsLock) _sessions.Remove(sessionId);
        }

        void HandleMessage(object sender, string text)
        {
            JanusEnvelope env;
            try
            {
                env = JsonConvert.DeserializeObject<JanusEnvelope>(text, JanusProtocol.JsonSettings);
            }
            catch (Exception ex)
            {
                var captured = ex;
                Dispatch(() => InternalError?.Invoke(this, captured));
                return;
            }
            if (env == null) return;
            DispatchEnvelope(env);
        }

        void DispatchEnvelope(JanusEnvelope env)
        {
            switch (env.Janus)
            {
                case JanusVerbs.Keepalive:
                    return;
                case JanusVerbs.Ack:
                    return; // ack is informational; transaction stays open
                case JanusVerbs.Error:
                    if (!string.IsNullOrEmpty(env.Transaction))
                    {
                        var code = env.Error?.Code ?? 0;
                        var reason = env.Error?.Reason ?? "Unknown error";
                        _transactions.TryFail(env.Transaction, new JanusException(code, reason));
                    }
                    return;
                case JanusVerbs.Success:
                    if (!string.IsNullOrEmpty(env.Transaction))
                    {
                        if (_transactions.TryComplete(env.Transaction, env)) return;
                    }
                    // Fall through: success addressed at handle level
                    DispatchToHandle(env);
                    return;
                case JanusVerbs.Timeout:
                    HandleTimeout(env);
                    return;
                default:
                    DispatchToHandle(env);
                    if (!string.IsNullOrEmpty(env.Transaction))
                        _transactions.TryComplete(env.Transaction, env);
                    return;
            }
        }

        void DispatchToHandle(JanusEnvelope env)
        {
            JanusSession session;
            lock (_sessionsLock)
            {
                if (env.SessionId == 0) return;
                if (!_sessions.TryGetValue(env.SessionId, out session)) return;
            }
            session.DispatchToHandle(env);
        }

        void HandleTimeout(JanusEnvelope env)
        {
            if (env.SessionId != 0)
            {
                lock (_sessionsLock)
                {
                    if (_sessions.TryGetValue(env.SessionId, out var s))
                        s.MarkTimedOut();
                }
            }
        }

        void HandleError(object sender, string message)
        {
            Dispatch(() => InternalError?.Invoke(this, new JanusTransportException(message)));
        }

        void HandleClosed(object sender, string reason)
        {
            StopKeepalive();
            _transactions.FailAll(new JanusTransportException("Connection closed: " + reason));
            Dispatch(() => Disconnected?.Invoke(this, reason));
            if (_userClosed || !Options.Reconnect.Enabled) return;
            _ = Task.Run(() => ReconnectLoop(reason));
        }

        async Task ReconnectLoop(string lastReason)
        {
            Dispatch(() => Reconnecting?.Invoke(this, EventArgs.Empty));
            var attempt = 0;
            var delay = Options.Reconnect.InitialBackoffMs;
            while (!_disposed && !_userClosed)
            {
                attempt++;
                try
                {
                    await Task.Delay(delay).ConfigureAwait(false);
                    await Transport.OpenAsync().ConfigureAwait(false);
                    StartKeepalive();
                    JanusSession[] sessions;
                    lock (_sessionsLock)
                    {
                        sessions = new JanusSession[_sessions.Count];
                        _sessions.Values.CopyTo(sessions, 0);
                    }
                    foreach (var s in sessions)
                        await s.ReclaimAsync().ConfigureAwait(false);
                    Dispatch(() => Reconnected?.Invoke(this, EventArgs.Empty));
                    return;
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Janus] Reconnect attempt {attempt} failed: {ex.Message}");
                    if (Options.Reconnect.MaxAttempts > 0 && attempt >= Options.Reconnect.MaxAttempts)
                    {
                        Dispatch(() => ReconnectFailed?.Invoke(this, ex.Message));
                        return;
                    }
                    delay = (int)Math.Min(delay * Options.Reconnect.BackoffFactor, Options.Reconnect.MaxBackoffMs);
                }
            }
        }

        void StartKeepalive()
        {
            StopKeepalive();
            _keepaliveCts = new CancellationTokenSource();
            var token = _keepaliveCts.Token;
            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await Task.Delay(Options.KeepaliveIntervalMs, token).ConfigureAwait(false);
                        if (token.IsCancellationRequested) break;
                        JanusSession[] sessions;
                        lock (_sessionsLock)
                        {
                            sessions = new JanusSession[_sessions.Count];
                            _sessions.Values.CopyTo(sessions, 0);
                        }
                        foreach (var s in sessions)
                        {
                            try
                            {
                                await SendRawAsync(new JanusRequest
                                {
                                    Janus = JanusVerbs.Keepalive,
                                    SessionId = s.Id,
                                    Transaction = JanusProtocol.NewTransactionId()
                                }, token);
                            }
                            catch (Exception ex)
                            {
                                Debug.LogWarning($"[Janus] Keepalive failed: {ex.Message}");
                            }
                        }
                    }
                    catch (OperationCanceledException) { break; }
                }
            }, token);
        }

        void StopKeepalive()
        {
            _keepaliveCts?.Cancel();
            _keepaliveCts = null;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _userClosed = true;
            StopKeepalive();
            try { Transport.CloseAsync().Wait(500); }
            catch { /* ignore */ }
        }
    }

    public static class JanusProtocol
    {
        public static JsonSerializerSettings JsonSettings { get; } = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Include
        };

        static readonly char[] _alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();
        static readonly System.Random _rng = new System.Random();
        static readonly object _rngLock = new object();

        public static string NewTransactionId(int length = 12)
        {
            var buf = new char[length];
            lock (_rngLock)
                for (int i = 0; i < length; i++)
                    buf[i] = _alphabet[_rng.Next(_alphabet.Length)];
            return new string(buf);
        }
    }
}
