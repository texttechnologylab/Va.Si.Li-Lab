using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using VaSiLi.Janus.VideoRoom;

namespace VaSiLi.Janus.WebRTC
{
    public class RemoteTrackEventArgs : EventArgs
    {
        public MediaStreamTrack Track;
        public string Mid;
        public RemoteStream StreamInfo;
    }

    /// <summary>
    /// High-level helper: owns an RTCPeerConnection and drives the subscriber SDP/ICE flow
    /// on top of a VideoRoomHandle. Re-subscribes automatically when the handle is restored.
    /// </summary>
    public class JanusVideoRoomSubscriber : IDisposable
    {
        public RTCPeerConnection PeerConnection { get; private set; }
        public VideoRoomHandle Handle { get; }
        public bool TrickleEnabled { get; set; } = true;

        public event EventHandler<RemoteTrackEventArgs> RemoteTrackAdded;
        public event Action<RTCPeerConnectionState> ConnectionStateChanged;
        public event Action<RTCIceConnectionState> IceStateChanged;

        readonly MonoBehaviour _runner;
        readonly RTCConfiguration _config;
        readonly Dictionary<string, RemoteStream> _streamsByMid = new Dictionary<string, RemoteStream>();

        long _room;
        long? _privateId;
        readonly List<StreamSubscription> _activeSubscriptions = new List<StreamSubscription>();
        bool _disposed;

        public JanusVideoRoomSubscriber(MonoBehaviour runner, VideoRoomHandle handle, RTCConfiguration config)
        {
            _runner = runner != null ? runner : throw new ArgumentNullException(nameof(runner));
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _config = config;
            CreatePeerConnection();
            handle.Restored += OnHandleRestored;
            handle.SubscriberAttached += OnSubscriberAttached;
        }

        void CreatePeerConnection()
        {
            var cfg = _config;
            PeerConnection = new RTCPeerConnection(ref cfg);
            PeerConnection.OnIceCandidate = HandleIceCandidate;
            PeerConnection.OnIceConnectionChange += s => IceStateChanged?.Invoke(s);
            PeerConnection.OnConnectionStateChange += s => ConnectionStateChanged?.Invoke(s);
            PeerConnection.OnTrack = HandleTrack;
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
                Debug.LogWarning($"[JanusSubscriber] Trickle failed: {ex.Message}");
            }
        }

        void HandleTrack(RTCTrackEvent ev)
        {
            string mid = null;
            try { mid = ev.Transceiver?.Mid; } catch { }
            _streamsByMid.TryGetValue(mid ?? string.Empty, out var info);
            RemoteTrackAdded?.Invoke(this, new RemoteTrackEventArgs
            {
                Track = ev.Track,
                Mid = mid,
                StreamInfo = info
            });
        }

        void OnSubscriberAttached(object sender, VideoRoomEventData data)
        {
            if (data?.Streams == null) return;
            foreach (var s in data.Streams)
                if (!string.IsNullOrEmpty(s.Mid)) _streamsByMid[s.Mid] = s;
        }

        public async Task SubscribeAsync(long room, List<StreamSubscription> streams, long? privateId = null, CancellationToken cancellation = default)
        {
            _room = room;
            _privateId = privateId;
            _activeSubscriptions.Clear();
            _activeSubscriptions.AddRange(streams);

            var offer = await Handle.JoinSubscriberAsync(room, streams, privateId, cancellation).ConfigureAwait(true);
            var sd = offer.ToSessionDescription();
            await PeerConnection.SetRemoteDescription(ref sd).AsTask(_runner).ConfigureAwait(true);
            var answerOp = PeerConnection.CreateAnswer();
            var answerDesc = await answerOp.AsTask(_runner).ConfigureAwait(true);
            await PeerConnection.SetLocalDescription(ref answerDesc).AsTask(_runner).ConfigureAwait(true);
            await Handle.StartAsync(answerDesc.ToJsep(), cancellation).ConfigureAwait(true);
        }

        public Task UpdateAsync(List<StreamSubscription> add, List<StreamSubscription> remove, CancellationToken cancellation = default)
        {
            if (add != null)
                foreach (var s in add) _activeSubscriptions.Add(s);
            if (remove != null)
                _activeSubscriptions.RemoveAll(s => remove.Exists(r => r.Feed == s.Feed && r.Mid == s.Mid));
            return Handle.UpdateSubscriptionAsync(add, remove, cancellation);
        }

        async void OnHandleRestored(object sender, EventArgs args)
        {
            if (_disposed || _activeSubscriptions.Count == 0) return;
            try
            {
                ClosePeerConnection();
                CreatePeerConnection();
                await SubscribeAsync(_room, new List<StreamSubscription>(_activeSubscriptions), _privateId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JanusSubscriber] Restore failed: {ex.Message}");
            }
        }

        void ClosePeerConnection()
        {
            if (PeerConnection == null) return;
            try { PeerConnection.Close(); } catch { }
            try { PeerConnection.Dispose(); } catch { }
            PeerConnection = null;
            _streamsByMid.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Handle.Restored -= OnHandleRestored;
            Handle.SubscriberAttached -= OnSubscriberAttached;
            ClosePeerConnection();
        }
    }
}
