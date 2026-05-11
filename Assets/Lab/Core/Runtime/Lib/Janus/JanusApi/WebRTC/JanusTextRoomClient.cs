using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Unity.WebRTC;
using UnityEngine;
using VaSiLi.Janus.TextRoom;

namespace VaSiLi.Janus.WebRTC
{
    public class TextRoomMessageEventArgs : EventArgs
    {
        public string From;
        public string Display;
        public string Text;
        public bool Whisper;
        public string Date;
    }

    public class TextRoomParticipantEventArgs : EventArgs
    {
        public string Username;
        public string Display;
    }

    /// <summary>
    /// High-level helper: owns an RTCPeerConnection + the textroom data channel and drives the
    /// SDP setup/ack flow on top of a TextRoomHandle. Re-establishes automatically after reconnect.
    /// </summary>
    public class JanusTextRoomClient : IDisposable
    {
        public RTCPeerConnection PeerConnection { get; private set; }
        public TextRoomHandle Handle { get; }
        public bool TrickleEnabled { get; set; } = true;
        public bool IsReady => _channel != null && _channel.ReadyState == RTCDataChannelState.Open;
        public long? Room { get; private set; }
        public string Username { get; private set; }
        public string Display { get; private set; }

        public event EventHandler Ready;
        public event EventHandler<TextRoomMessageEventArgs> MessageReceived;
        public event EventHandler<TextRoomParticipantEventArgs> ParticipantJoined;
        public event EventHandler<TextRoomParticipantEventArgs> ParticipantLeft;
        public event EventHandler<string> Announcement;
        public event Action<RTCPeerConnectionState> ConnectionStateChanged;

        readonly MonoBehaviour _runner;
        readonly RTCConfiguration _config;
        readonly ConcurrentDictionary<string, TaskCompletionSource<TextRoomEventData>> _pending
            = new ConcurrentDictionary<string, TaskCompletionSource<TextRoomEventData>>();
        readonly object _lock = new object();

        RTCDataChannel _channel;
        bool _setupDone;
        bool _disposed;

        public JanusTextRoomClient(MonoBehaviour runner, TextRoomHandle handle, RTCConfiguration config)
        {
            _runner = runner != null ? runner : throw new ArgumentNullException(nameof(runner));
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _config = config;
            CreatePeerConnection();
            handle.Restored += OnHandleRestored;
        }

        void CreatePeerConnection()
        {
            var cfg = _config;
            PeerConnection = new RTCPeerConnection(ref cfg);
            PeerConnection.OnIceCandidate = HandleIceCandidate;
            PeerConnection.OnConnectionStateChange += s => ConnectionStateChanged?.Invoke(s);
            PeerConnection.OnDataChannel = HandleDataChannel;
        }

        void HandleIceCandidate(RTCIceCandidate candidate)
        {
            if (!TrickleEnabled || !Handle.IsAttached) return;
            try
            {
                if (candidate == null || string.IsNullOrEmpty(candidate.Candidate))
                {
                    _ = Handle.SendTrickleCompletedAsync();
                    return;
                }
                _ = Handle.SendTrickleAsync(new JanusCandidate
                {
                    Candidate = candidate.Candidate,
                    SdpMid = candidate.SdpMid,
                    SdpMLineIndex = candidate.SdpMLineIndex
                });
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[JanusTextRoom] Trickle failed: {ex.Message}");
            }
        }

        void HandleDataChannel(RTCDataChannel channel)
        {
            lock (_lock) { _channel = channel; }
            channel.OnOpen = () => Ready?.Invoke(this, EventArgs.Empty);
            channel.OnMessage = HandleChannelBytes;
        }

        void HandleChannelBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0) return;
            string raw;
            try { raw = Encoding.UTF8.GetString(bytes); }
            catch (Exception ex) { Debug.LogWarning($"[JanusTextRoom] decode failed: {ex.Message}"); return; }

            TextRoomEventData ev;
            try { ev = JsonConvert.DeserializeObject<TextRoomEventData>(raw); }
            catch (Exception ex) { Debug.LogWarning($"[JanusTextRoom] parse failed: {ex.Message} ({raw})"); return; }
            if (ev == null) return;

            if (!string.IsNullOrEmpty(ev.Transaction) && _pending.TryRemove(ev.Transaction, out var tcs))
            {
                if (ev.Action == TextRoomActions.Error)
                    tcs.TrySetException(new JanusException($"TextRoom error {ev.ErrorCode}: {ev.Error}"));
                else
                    tcs.TrySetResult(ev);
                return;
            }

            switch (ev.Action)
            {
                case TextRoomActions.Message:
                    MessageReceived?.Invoke(this, new TextRoomMessageEventArgs
                    {
                        From = ev.From,
                        Display = ev.Display,
                        Text = ev.Text,
                        Whisper = ev.Whisper ?? false,
                        Date = ev.Date
                    });
                    break;
                case TextRoomActions.Announcement:
                    Announcement?.Invoke(this, ev.Text);
                    break;
                case TextRoomActions.Join:
                    ParticipantJoined?.Invoke(this, new TextRoomParticipantEventArgs
                    {
                        Username = ev.Username,
                        Display = ev.Display
                    });
                    break;
                case TextRoomActions.Leave:
                    ParticipantLeft?.Invoke(this, new TextRoomParticipantEventArgs
                    {
                        Username = ev.Username,
                        Display = ev.Display
                    });
                    break;
            }
        }

        public async Task SetupAsync(CancellationToken cancellation = default)
        {
            if (_setupDone) return;
            var offer = await Handle.SetupAsync(cancellation).ConfigureAwait(true);
            var sd = offer.ToSessionDescription();
            await PeerConnection.SetRemoteDescription(ref sd).AsTask(_runner).ConfigureAwait(true);
            var answerOp = PeerConnection.CreateAnswer();
            var answerDesc = await answerOp.AsTask(_runner).ConfigureAwait(true);
            await PeerConnection.SetLocalDescription(ref answerDesc).AsTask(_runner).ConfigureAwait(true);
            await Handle.AckAsync(answerDesc.ToJsep(), cancellation).ConfigureAwait(true);
            _setupDone = true;
        }

        /// <summary>Idempotent: tries to create the textroom; swallows "room exists" (error 486).</summary>
        public async Task EnsureRoomAsync(long room, string description = null, CancellationToken cancellation = default)
        {
            var tx = JanusProtocol.NewTransactionId();
            try
            {
                await SendOnChannelAsync(tx, new TextRoomCreateMessage
                {
                    Transaction = tx,
                    Room = room,
                    Description = description,
                    History = 0
                }, cancellation).ConfigureAwait(true);
            }
            catch (JanusException ex) when (ex.Message.Contains("486"))
            {
                // Room already exists — fine.
            }
        }

        public async Task JoinAsync(long room, string username, string display, CancellationToken cancellation = default)
        {
            var tx = JanusProtocol.NewTransactionId();
            await SendOnChannelAsync(tx, new TextRoomJoinMessage
            {
                Transaction = tx,
                Room = room,
                Username = username,
                Display = display
            }, cancellation).ConfigureAwait(true);
            Room = room;
            Username = username;
            Display = display;
        }

        public Task LeaveAsync(CancellationToken cancellation = default)
        {
            if (!Room.HasValue) return Task.CompletedTask;
            var tx = JanusProtocol.NewTransactionId();
            return SendOnChannelAsync(tx, new TextRoomLeaveMessage { Transaction = tx, Room = Room.Value }, cancellation);
        }

        public Task SendChatAsync(string text, CancellationToken cancellation = default)
        {
            if (!Room.HasValue) throw new InvalidOperationException("Not joined to a textroom");
            var tx = JanusProtocol.NewTransactionId();
            return SendOnChannelAsync(tx, new TextRoomSendMessage
            {
                Transaction = tx,
                Room = Room.Value,
                Text = text,
                Ack = false
            }, cancellation);
        }

        public Task WhisperAsync(string toUsername, string text, CancellationToken cancellation = default)
        {
            if (!Room.HasValue) throw new InvalidOperationException("Not joined to a textroom");
            var tx = JanusProtocol.NewTransactionId();
            return SendOnChannelAsync(tx, new TextRoomSendMessage
            {
                Transaction = tx,
                Room = Room.Value,
                Text = text,
                Ack = false,
                To = toUsername
            }, cancellation);
        }

        Task<TextRoomEventData> SendOnChannelAsync(string transaction, object body, CancellationToken cancellation)
        {
            if (_channel == null || _channel.ReadyState != RTCDataChannelState.Open)
                throw new InvalidOperationException("TextRoom data channel not open");

            var json = JsonConvert.SerializeObject(body, Formatting.None,
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });

            var tcs = new TaskCompletionSource<TextRoomEventData>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[transaction] = tcs;
            cancellation.Register(() => { if (_pending.TryRemove(transaction, out var c)) c.TrySetCanceled(); });

            try { _channel.Send(json); }
            catch (Exception ex)
            {
                _pending.TryRemove(transaction, out _);
                tcs.TrySetException(ex);
            }
            return tcs.Task;
        }

        async void OnHandleRestored(object sender, EventArgs args)
        {
            if (_disposed) return;
            try
            {
                ClosePeerConnection();
                CreatePeerConnection();
                _setupDone = false;
                await SetupAsync().ConfigureAwait(false);
                if (Room.HasValue && !string.IsNullOrEmpty(Username))
                    await JoinAsync(Room.Value, Username, Display).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JanusTextRoom] Restore failed: {ex.Message}");
            }
        }

        void ClosePeerConnection()
        {
            if (_channel != null) { try { _channel.Close(); } catch { } _channel = null; }
            if (PeerConnection != null)
            {
                try { PeerConnection.Close(); } catch { }
                try { PeerConnection.Dispose(); } catch { }
                PeerConnection = null;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Handle.Restored -= OnHandleRestored;
            ClosePeerConnection();
            foreach (var kv in _pending) kv.Value.TrySetCanceled();
            _pending.Clear();
        }
    }
}
