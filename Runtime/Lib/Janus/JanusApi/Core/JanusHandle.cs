using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VaSiLi.Janus
{
    public class JanusPluginEvent
    {
        public JObject Data;
        public Jsep Jsep;
        public string Transaction;
    }

    public class JanusPluginResponse<TData>
    {
        public TData Data;
        public Jsep Jsep;
        public string Transaction;
    }

    public abstract class JanusHandle
    {
        public string PluginName { get; protected set; }
        public string OpaqueId { get; private set; }
        public long Id { get; private set; }
        public JanusSession Session { get; private set; }
        public bool IsAttached => Id != 0 && Session != null && Session.IsValid;

        public event EventHandler<JanusPluginEvent> Message;
        public event EventHandler Webrtcup;
        public event EventHandler<string> HungUp;
        public event EventHandler Detached;
        public event EventHandler<JObject> SlowLink;
        public event EventHandler<JObject> Media;
        public event EventHandler<JObject> TrickleReceived;
        public event EventHandler Restored;

        internal void Initialize(JanusSession session, string opaqueId)
        {
            Session = session;
            OpaqueId = opaqueId;
        }

        internal void SetHandleId(long id) { Id = id; }

        public async Task<JanusPluginEvent> SendMessageAsync(object body, Jsep jsep = null, CancellationToken cancellation = default)
        {
            if (!IsAttached) throw new InvalidOperationException("Handle not attached");
            var env = await Session.Connection.SendRequestAsync(new JanusRequest
            {
                Janus = JanusVerbs.Message,
                SessionId = Session.Id,
                HandleId = Id,
                Body = body,
                Jsep = jsep
            }, cancellation).ConfigureAwait(false);
            return new JanusPluginEvent
            {
                Data = env.PluginData?.Data,
                Jsep = env.Jsep,
                Transaction = env.Transaction
            };
        }

        public async Task<JanusPluginResponse<TResponse>> SendMessageAsync<TResponse>(object body, Jsep jsep = null, CancellationToken cancellation = default)
        {
            var ev = await SendMessageAsync(body, jsep, cancellation).ConfigureAwait(false);
            return new JanusPluginResponse<TResponse>
            {
                Data = ev.Data == null ? default : ev.Data.ToObject<TResponse>(),
                Jsep = ev.Jsep,
                Transaction = ev.Transaction
            };
        }

        public Task SendTrickleAsync(JanusCandidate candidate, CancellationToken cancellation = default)
        {
            if (!IsAttached) return Task.CompletedTask;
            return Session.Connection.SendRawAsync(new JanusRequest
            {
                Janus = JanusVerbs.Trickle,
                SessionId = Session.Id,
                HandleId = Id,
                Candidate = candidate,
                Transaction = JanusProtocol.NewTransactionId()
            }, cancellation);
        }

        public Task SendTrickleCompletedAsync(CancellationToken cancellation = default)
            => SendTrickleAsync(new JanusCandidate { Completed = true }, cancellation);

        public async Task HangupAsync(CancellationToken cancellation = default)
        {
            if (!IsAttached) return;
            await Session.Connection.SendRequestAsync(new JanusRequest
            {
                Janus = JanusVerbs.Hangup,
                SessionId = Session.Id,
                HandleId = Id
            }, cancellation).ConfigureAwait(false);
        }

        public async Task DetachAsync(CancellationToken cancellation = default)
        {
            if (!IsAttached) return;
            try
            {
                await Session.Connection.SendRequestAsync(new JanusRequest
                {
                    Janus = JanusVerbs.Detach,
                    SessionId = Session.Id,
                    HandleId = Id
                }, cancellation).ConfigureAwait(false);
            }
            catch { /* ignored */ }
            Session.RemoveHandle(Id);
            Id = 0;
        }

        internal void HandleEnvelope(JanusEnvelope env)
        {
            // Hop to the main-thread dispatcher so consumers of events can safely use Unity APIs.
            Session?.Connection?.Dispatch(() => HandleEnvelopeOnDispatcher(env));
        }

        void HandleEnvelopeOnDispatcher(JanusEnvelope env)
        {
            switch (env.Janus)
            {
                case JanusVerbs.Event:
                    OnMessage(env);
                    Message?.Invoke(this, new JanusPluginEvent
                    {
                        Data = env.PluginData?.Data,
                        Jsep = env.Jsep,
                        Transaction = env.Transaction
                    });
                    break;
                case JanusVerbs.Success:
                    Message?.Invoke(this, new JanusPluginEvent
                    {
                        Data = env.PluginData?.Data,
                        Jsep = env.Jsep,
                        Transaction = env.Transaction
                    });
                    break;
                case JanusVerbs.Webrtcup:
                    Webrtcup?.Invoke(this, EventArgs.Empty);
                    break;
                case JanusVerbs.HangupIn:
                    HungUp?.Invoke(this, env.Reason);
                    break;
                case JanusVerbs.Detached:
                    Session.RemoveHandle(Id);
                    Detached?.Invoke(this, EventArgs.Empty);
                    break;
                case JanusVerbs.Slowlink:
                    SlowLink?.Invoke(this, env.Data);
                    break;
                case JanusVerbs.Media:
                    Media?.Invoke(this, env.Data);
                    break;
                case JanusVerbs.Trickle:
                    TrickleReceived?.Invoke(this, env.Candidate);
                    break;
            }
        }

        /// <summary>
        /// Override to handle plugin-specific incoming events (deserialize plugindata.data into a typed payload).
        /// Called before the Message event fires.
        /// </summary>
        protected virtual void OnMessage(JanusEnvelope env) { }

        /// <summary>
        /// Override to re-establish state after a reconnect/reattach (e.g. re-issue a join request).
        /// </summary>
        public virtual Task RestoreAsync(CancellationToken cancellation = default)
        {
            Session?.Connection?.Dispatch(() => Restored?.Invoke(this, EventArgs.Empty));
            return Task.CompletedTask;
        }

        /// <summary>Lightweight non-typed handle for plugins without a dedicated wrapper.</summary>
        public sealed class Generic : JanusHandle
        {
            public Generic() { }
            public Generic(string plugin) { PluginName = plugin; }
        }
    }
}
