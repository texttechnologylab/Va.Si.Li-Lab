using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using VaSiLi.Janus.VideoRoom;

namespace VaSiLi.Janus.WebRTC
{
    /// <summary>
    /// High-level helper: owns an RTCPeerConnection and drives the publisher SDP/ICE flow
    /// on top of a VideoRoomHandle. Re-publishes automatically when the handle is restored.
    /// </summary>
    public class JanusVideoRoomPublisher : IDisposable
    {
        public RTCPeerConnection PeerConnection { get; private set; }
        public VideoRoomHandle Handle { get; }
        public bool TrickleEnabled { get; set; } = true;

        readonly MonoBehaviour _runner;
        readonly RTCConfiguration _config;
        readonly List<(MediaStreamTrack track, MediaStream stream)> _tracks = new List<(MediaStreamTrack, MediaStream)>();
        readonly List<RTCRtpSender> _senders = new List<RTCRtpSender>();
        bool _published;
        bool _disposed;
        ConfigureRequest _lastConfigure;

        public event Action<RTCPeerConnectionState> ConnectionStateChanged;
        public event Action<RTCIceConnectionState> IceStateChanged;

        public JanusVideoRoomPublisher(MonoBehaviour runner, VideoRoomHandle handle, RTCConfiguration config)
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
            PeerConnection.OnIceConnectionChange += s => IceStateChanged?.Invoke(s);
            PeerConnection.OnConnectionStateChange += s => ConnectionStateChanged?.Invoke(s);
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
                Debug.LogWarning($"[JanusPublisher] Trickle failed: {ex.Message}");
            }
        }

        public RTCRtpSender AddTrack(MediaStreamTrack track, MediaStream stream)
        {
            _tracks.Add((track, stream));
            var sender = PeerConnection.AddTrack(track, stream);
            _senders.Add(sender);
            return sender;
        }

        public async Task PublishAsync(ConfigureRequest options = null, CancellationToken cancellation = default)
        {
            _lastConfigure = options;
            var offerOp = PeerConnection.CreateOffer();
            var offer = await offerOp.AsTask(_runner).ConfigureAwait(true);
            await PeerConnection.SetLocalDescription(ref offer).AsTask(_runner).ConfigureAwait(true);
            var jsepOffer = offer.ToJsep();
            var jsepAnswer = await Handle.PublishAsync(jsepOffer, options, cancellation).ConfigureAwait(true);
            var answer = jsepAnswer.ToSessionDescription();
            await PeerConnection.SetRemoteDescription(ref answer).AsTask(_runner).ConfigureAwait(true);
            _published = true;
        }

        public async Task UnpublishAsync(CancellationToken cancellation = default)
        {
            if (!_published) return;
            await Handle.UnpublishAsync(cancellation).ConfigureAwait(false);
            _published = false;
        }

        async void OnHandleRestored(object sender, EventArgs args)
        {
            if (!_published || _disposed) return;
            try
            {
                // Old PC is dead from the server's POV — rebuild and re-publish.
                ClosePeerConnection();
                CreatePeerConnection();
                foreach (var (track, stream) in _tracks)
                    _senders.Add(PeerConnection.AddTrack(track, stream));
                await PublishAsync(_lastConfigure).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[JanusPublisher] Restore failed: {ex.Message}");
            }
        }

        void ClosePeerConnection()
        {
            if (PeerConnection == null) return;
            try { PeerConnection.Close(); } catch { /* ignore */ }
            try { PeerConnection.Dispose(); } catch { /* ignore */ }
            PeerConnection = null;
            _senders.Clear();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Handle.Restored -= OnHandleRestored;
            ClosePeerConnection();
        }
    }
}
