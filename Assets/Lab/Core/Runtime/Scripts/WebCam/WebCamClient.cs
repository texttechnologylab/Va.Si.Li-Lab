using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.WebRTC;
using UnityEngine;
using VaSiLi.Janus;
using VaSiLi.Janus.VideoRoom;
using VaSiLi.Janus.WebRTC;
#if !UNITY_WEBGL
using Ubiq.Voip.Implementations.Dotnet;
using SIPSorceryMedia.Abstractions;
#endif

namespace VaSiLi.WebCam
{
    /// <summary>
    /// Demo client that joins (or creates) a Janus VideoRoom, publishes the local
    /// audio/video, and renders the first remote feed it receives. Built on the
    /// VaSiLi.Janus library; auto-reconnects and re-publishes on disconnect.
    /// </summary>
    public class WebCamClient : MonoBehaviour
    {
        [Header("Janus")]
        public string ServerUrl = "wss://janus.faces.texttechnologylab.org";
        public string SubProtocol = "janus-protocol";
        public long Room = 1234;
        public string DisplayName = "test-user";

        [Header("ICE")]
        public string TurnUrl = "turn:turn.vasililab.texttechnologylab.org:3478";
        public string TurnUsername = "testuser";
        public string TurnCredential = "";

        [Header("Capture")]
        public Camera captureCamera;
        public int captureWidth = 1280;
        public int captureHeight = 720;
        public int maxBitrate = 1_000_000;
        public int minBitrate = 250_000;
        public int maxFramerate = 30;

        [Header("Playback")]
        public MeshRenderer frame;
        public AudioSource output;

        JanusConnection _connection;
        JanusSession _session;
        VideoRoomHandle _publisherHandle;
        VideoRoomHandle _subscriberHandle;
        JanusVideoRoomPublisher _publisher;
        JanusVideoRoomSubscriber _subscriber;

#if !UNITY_WEBGL
        private IVoipSource _micInput;
#endif
        private AudioStreamTrack _audioTrack;

        readonly Dictionary<long, RemotePublisher> _knownPublishers = new Dictionary<long, RemotePublisher>();

        async void Start()
        {
            StartCoroutine(WebRTC.Update());

            try
            {
                await ConnectAndJoin();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[WebCamClient] Bootstrap failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

#if !UNITY_WEBGL
        void Update()
        {
            if (_micInput == null)
            {
                var micObj = GameObject.Find("Microphone Dotnet Voip Source");
                if (micObj != null)
                {
                    _micInput = micObj.GetComponent<IVoipSource>();
                    if (_micInput is MicrophoneVoipSource micSource)
                    {
                        micSource.OnAudioPcmSample += OnAudioPcmSample;
                    }
                }
            }
        }

        void OnAudioPcmSample(AudioSamplingRatesEnum samplingRate, float[] sample)
        {
            if (sample == null || sample.Length == 0 || _audioTrack == null) return;
            _audioTrack.SetData(sample, 1, (int)samplingRate);
        }
#endif

        async Task ConnectAndJoin()
        {
            var transport = new JanusWebSocketTransport(new Uri(ServerUrl), SubProtocol);
            _connection = new JanusConnection(transport, new JanusConnectionOptions
            {
                KeepaliveIntervalMs = 25_000,
                Reconnect = new JanusReconnectOptions { Enabled = true }
            });

            _connection.Connected    += (_, __) => Debug.Log("[Janus] connected");
            _connection.Disconnected += (_, r)  => Debug.LogWarning($"[Janus] disconnected: {r}");
            _connection.Reconnecting += (_, __) => Debug.Log("[Janus] reconnecting…");
            _connection.Reconnected  += (_, __) => Debug.Log("[Janus] reconnected, sessions reclaimed");
            _connection.ReconnectFailed += (_, r) => Debug.LogError($"[Janus] reconnect gave up: {r}");
            _connection.InternalError += (_, ex) => Debug.LogError($"[Janus] internal error: {ex.Message}");

            await _connection.OpenAsync();
            _session = await _connection.CreateSessionAsync();

            _publisherHandle = await _session.AttachAsync<VideoRoomHandle>("vasili-publisher");
            _publisherHandle.PublishersAdded += OnPublishersAdded;
            _publisherHandle.PublisherLeft += (_, e) => Debug.Log($"[VideoRoom] publisher {e.Id} left");
            _publisherHandle.PublisherUnpublished += (_, e) => Debug.Log($"[VideoRoom] publisher {e.Id} unpublished");
            _publisherHandle.RoomDestroyed += (_, e) => Debug.LogWarning($"[VideoRoom] room {e.Room} destroyed");

            var joined = await _publisherHandle.JoinOrCreateAsync(
                Room,
                DisplayName,
                new CreateRoomRequest
                {
                    Description = "VaSiLi room " + Room,
                    Publishers = 12,
                    Bitrate = 0,
                    FirFreq = 10,
                    AudioCodec = "opus",
                    VideoCodec = "vp8,h264,vp9",
                    NotifyJoining = false
                });
            Debug.Log($"[VideoRoom] joined room {Room} as id={joined.Id}, private_id={joined.PrivateId}");

            await StartPublishing();

            if (joined.Publishers != null && joined.Publishers.Count > 0)
                await SubscribeToAvailable(joined.Publishers);
        }

        RTCConfiguration BuildRtcConfiguration() => new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer
                {
                    urls = new[] { TurnUrl },
                    credential = TurnCredential,
                    username = TurnUsername
                }
            }
        };

        async Task StartPublishing()
        {
            var rtcConfig = BuildRtcConfiguration();
            _publisher = new JanusVideoRoomPublisher(this, _publisherHandle, rtcConfig);
            _publisher.IceStateChanged += s => Debug.Log("[Publisher] ICE: " + s);
            _publisher.ConnectionStateChanged += s => Debug.Log("[Publisher] PC: " + s);

            var sendStream = new MediaStream();
            _audioTrack = new AudioStreamTrack();
            _publisher.AddTrack(_audioTrack, sendStream);

            var videoTrack = captureCamera.CaptureStreamTrack(captureWidth, captureHeight);
            var videoSender = _publisher.AddTrack(videoTrack, sendStream);

            var parameters = videoSender.GetParameters();
            foreach (var encoding in parameters.encodings)
            {
                encoding.maxFramerate = (uint)maxFramerate;
                encoding.maxBitrate = (ulong)maxBitrate;
                encoding.minBitrate = (ulong)minBitrate;
            }
            videoSender.SetParameters(parameters);

            await _publisher.PublishAsync(new ConfigureRequest { Audio = true, Video = true });
            Debug.Log("[Publisher] published");
        }

        async void OnPublishersAdded(object sender, PublishersAddedEventArgs e)
        {
            try { await SubscribeToAvailable(e.Publishers); }
            catch (Exception ex) { Debug.LogError($"[Subscriber] subscribe failed: {ex.Message}"); }
        }

        async Task SubscribeToAvailable(List<RemotePublisher> publishers)
        {
            var newStreams = new List<StreamSubscription>();
            foreach (var p in publishers)
            {
                if (p.Dummy == true) continue;
                if (_knownPublishers.ContainsKey(p.Id)) continue;
                _knownPublishers[p.Id] = p;
                if (p.Streams == null) continue;
                foreach (var s in p.Streams)
                    newStreams.Add(new StreamSubscription { Feed = p.Id, Mid = s.Mid });
            }
            if (newStreams.Count == 0) return;

            if (_subscriber == null)
            {
                _subscriberHandle = await _session.AttachAsync<VideoRoomHandle>("vasili-subscriber");
                _subscriber = new JanusVideoRoomSubscriber(this, _subscriberHandle, BuildRtcConfiguration());
                _subscriber.RemoteTrackAdded += OnRemoteTrack;
                _subscriber.IceStateChanged += s => Debug.Log("[Subscriber] ICE: " + s);
                _subscriber.ConnectionStateChanged += s => Debug.Log("[Subscriber] PC: " + s);
                await _subscriber.SubscribeAsync(Room, newStreams, _publisherHandle.PrivateId);
            }
            else
            {
                await _subscriber.UpdateAsync(newStreams, null);
            }
        }

        void OnRemoteTrack(object sender, RemoteTrackEventArgs e)
        {
            Debug.Log($"[WebCam] RemoteTrack: type={(e.Track != null ? e.Track.GetType().Name : "null")} mid={e.Mid} feed={e.StreamInfo?.FeedId}");
            if (e.Track is AudioStreamTrack audio)
            {
                if (output != null)
                {
                    output.SetTrack(audio);
                    output.loop = true;
                    output.Play();
                }
            }
            else if (e.Track is VideoStreamTrack video)
            {
                Debug.Log($"[WebCam] VideoTrack received, frame={(frame != null ? frame.name : "NULL")}");
                video.OnVideoReceived += texture =>
                {
                    Debug.Log($"[WebCam] OnVideoReceived: {(texture != null ? $"{texture.width}x{texture.height}" : "null")}");
                    if (frame != null)
                    {
                        frame.material.mainTexture = texture;
                        frame.material.SetTexture("_BaseMap", texture); // URP Lit shader
                    }
                };
            }
        }

        async void OnDestroy()
        {
            try
            {
#if !UNITY_WEBGL
                if (_micInput is MicrophoneVoipSource micSource)
                    micSource.OnAudioPcmSample -= OnAudioPcmSample;
#endif
                _publisher?.Dispose();
                _subscriber?.Dispose();
                if (_connection != null) await _connection.CloseAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WebCamClient] Cleanup error: {ex.Message}");
            }
        }
    }
}
