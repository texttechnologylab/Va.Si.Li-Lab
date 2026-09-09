using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
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
        readonly RemoteCandidateApplier _remoteCandidates = new RemoteCandidateApplier("[JanusSubscriber]");

        long _room;
        long? _privateId;
        readonly List<StreamSubscription> _activeSubscriptions = new List<StreamSubscription>();
        bool _disposed;
        bool _joined;

        public bool IsJoined => _joined;

        public JanusVideoRoomSubscriber(MonoBehaviour runner, VideoRoomHandle handle, RTCConfiguration config)
        {
            _runner = runner != null ? runner : throw new ArgumentNullException(nameof(runner));
            Handle = handle ?? throw new ArgumentNullException(nameof(handle));
            _config = config;
            CreatePeerConnection();
            handle.Restored += OnHandleRestored;
            handle.SubscriberAttached += OnSubscriberAttached;
            handle.TrickleReceived += OnRemoteTrickle;
        }

        void OnRemoteTrickle(object sender, JObject candidate) => _remoteCandidates.Receive(candidate);

        void CreatePeerConnection()
        {
            var cfg = _config;
            PeerConnection = new RTCPeerConnection(ref cfg);
            PeerConnection.OnIceCandidate = HandleIceCandidate;
            PeerConnection.OnIceConnectionChange += s => IceStateChanged?.Invoke(s);
            PeerConnection.OnConnectionStateChange += s => ConnectionStateChanged?.Invoke(s);
            PeerConnection.OnIceGatheringStateChange += s => Debug.Log("[JanusSubscriber] ICE gathering: " + s);
            PeerConnection.OnTrack = HandleTrack;
            _remoteCandidates.Bind(PeerConnection);
        }

        void HandleIceCandidate(RTCIceCandidate candidate)
        {
            if (!TrickleEnabled || !Handle.IsAttached) return;
            try
            {
                if (candidate == null || string.IsNullOrEmpty(candidate.Candidate))
                {
                    Debug.Log("[JanusSubscriber] local ICE gathering complete (end-of-candidates)");
                    _ = Handle.SendTrickleCompletedAsync();
                    return;
                }
                Debug.Log($"[JanusSubscriber] local ICE candidate: typ={UnityWebRtcExtensions.IceCandidateType(candidate.Candidate)} mid={candidate.SdpMid}");
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

        static bool _loggedDecoders;

        /// <summary>
        /// Dumps the video codecs this Unity build can RECEIVE/decode. If a remote (e.g. a
        /// browser) offers a codec/profile that isn't in this list, CreateAnswer() rejects the
        /// video m-line (port 0) and no frames arrive — compare this against "Offer codecs".
        /// </summary>
        static void LogSupportedVideoDecoders()
        {
            if (_loggedDecoders) return;
            _loggedDecoders = true;
            try
            {
                var caps = RTCRtpReceiver.GetCapabilities(TrackKind.Video);
                // Single line on purpose: Android logcat drops everything after the first '\n'
                // in a message, so multi-line dumps lose their body when captured via adb.
                var sb = new System.Text.StringBuilder("[JanusSubscriber] Supported video decoders:");
                if (caps.codecs == null || caps.codecs.Length == 0)
                    sb.Append(" <none>");
                else
                    foreach (var c in caps.codecs)
                        sb.Append(" | ").Append(c.mimeType)
                          .Append(string.IsNullOrEmpty(c.sdpFmtpLine) ? "" : " [" + c.sdpFmtpLine + "]");
                Debug.Log(sb.ToString());
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[JanusSubscriber] Could not query video decoder capabilities: " + ex.Message);
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

            Debug.Log($"[JanusSubscriber] SubscribeAsync: joining room {room} with {streams.Count} streams (private_id={privateId})");
            LogSupportedVideoDecoders();
            var offer = await Handle.JoinSubscriberAsync(room, streams, privateId, cancellation).ConfigureAwait(true);
            Debug.Log("[JanusSubscriber] Offer SDP m= lines: " + UnityWebRtcExtensions.MLineSummary(offer.Sdp));
            Debug.Log("[JanusSubscriber] Offer codecs: " + UnityWebRtcExtensions.CodecSummary(offer.Sdp));
            // Browsers offer H.264 Baseline (profile-iop "00"); the Quest decoder only advertises
            // Constrained Baseline ("e0"). They are decode-compatible, but WebRTC's exact-string
            // profile match would otherwise reject the video m-line. Normalize before negotiating.
            offer.Sdp = UnityWebRtcExtensions.NormalizeH264ProfileLevelId(offer.Sdp);
            var sd = offer.ToSessionDescription();
            await PeerConnection.SetRemoteDescription(ref sd).AsTask(_runner).ConfigureAwait(true);
            _remoteCandidates.MarkReady();
            Debug.Log("[JanusSubscriber] ✓ Remote description set");
            var answerOp = PeerConnection.CreateAnswer();
            var answerDesc = await answerOp.AsTask(_runner).ConfigureAwait(true);
            Debug.Log("[JanusSubscriber] Answer SDP m= lines: " + UnityWebRtcExtensions.MLineSummary(answerDesc.sdp));
            Debug.Log("[JanusSubscriber] Answer codecs: " + UnityWebRtcExtensions.CodecSummary(answerDesc.sdp));
            await PeerConnection.SetLocalDescription(ref answerDesc).AsTask(_runner).ConfigureAwait(true);
            Debug.Log("[JanusSubscriber] ✓ Local description set, sending start to Janus");
            await Handle.StartAsync(answerDesc.ToJsep(), cancellation).ConfigureAwait(true);
            Debug.Log("[JanusSubscriber] ✓ Start sent — PC waiting for ICE to connect");
            _joined = true;
        }

        public async Task UpdateAsync(List<StreamSubscription> add, List<StreamSubscription> remove, CancellationToken cancellation = default)
        {
            if (add != null)
                foreach (var s in add) _activeSubscriptions.Add(s);
            if (remove != null)
                _activeSubscriptions.RemoveAll(s => remove.Exists(r => r.Feed == s.Feed && r.Mid == s.Mid));

            var ev = await Handle.UpdateSubscriptionAsync(add, remove, cancellation).ConfigureAwait(true);

            // Janus sends a new SDP offer whenever streams are added (renegotiation).
            // Without completing the exchange the new tracks never arrive.
            if (ev?.Jsep != null && ev.Jsep.Type == "offer")
            {
                Debug.Log("[JanusSubscriber] UpdateAsync: renegotiating SDP after update");
                ev.Jsep.Sdp = UnityWebRtcExtensions.NormalizeH264ProfileLevelId(ev.Jsep.Sdp);
                var sd = ev.Jsep.ToSessionDescription();
                await PeerConnection.SetRemoteDescription(ref sd).AsTask(_runner).ConfigureAwait(true);
                _remoteCandidates.MarkReady();
                var answerOp = PeerConnection.CreateAnswer();
                var answerDesc = await answerOp.AsTask(_runner).ConfigureAwait(true);
                await PeerConnection.SetLocalDescription(ref answerDesc).AsTask(_runner).ConfigureAwait(true);
                await Handle.StartAsync(answerDesc.ToJsep(), cancellation).ConfigureAwait(true);
            }
        }

        async void OnHandleRestored(object sender, EventArgs args)
        {
            if (_disposed || _activeSubscriptions.Count == 0) return;
            _joined = false;
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
            _joined = false;
            Handle.Restored -= OnHandleRestored;
            Handle.SubscriberAttached -= OnSubscriberAttached;
            Handle.TrickleReceived -= OnRemoteTrickle;
            ClosePeerConnection();
        }
    }
}
