using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using TMPro;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VaSiLi.Janus;
using VaSiLi.Janus.TextRoom;
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

        [Header("Chat (optional)")]
        [Tooltip("If set, the client attaches a textroom plugin handle and joins the same room id.")]
        public bool enableChat = false;
        public string chatUsername = "";
        [Tooltip("TMP_Text where incoming/outgoing messages are appended.")]
        public TMP_Text chatMessages;
        public TMP_InputField chatInput;
        public Button chatSendButton;
        public int chatMaxLines = 50;

        [Header("Recording (optional)")]
        [Tooltip("Button toggles recording state via the videoroom plugin.")]
        public Button recordButton;
        [Tooltip("Enabled when recording is on, disabled when off.")]
        public GameObject recordIndicator;
        [Tooltip("Optional renderer whose material color is swapped between idle and recording.")]
        public Renderer recordButtonRenderer;
        public Color recordIdleColor = Color.white;
        public Color recordActiveColor = Color.red;
        public TMP_Text recordButtonLabel;
        public string recordIdleText = "Record";
        public string recordActiveText = "Stop";
        public UnityEvent<bool> OnRecordingChanged;

        [Header("Answer Options (optional)")]
        [Tooltip("If set, the client listens for OnAnswerOptionsReceived and forwards them over the textroom data channel.")]
        public WebViewReceiver answerOptionsSource;

        public JanusConnection Connection => _connection;
        public JanusSession Session => _session;
        public VideoRoomHandle PublisherHandle => _publisherHandle;
        public JanusTextRoomClient Chat => _chat;

        JanusConnection _connection;
        JanusSession _session;
        VideoRoomHandle _publisherHandle;
        VideoRoomHandle _subscriberHandle;
        JanusVideoRoomPublisher _publisher;
        JanusVideoRoomSubscriber _subscriber;
        TextRoomHandle _textHandle;
        JanusTextRoomClient _chat;
        bool _chatReady;
        string[] _pendingAnswerOptions;

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
            _publisherHandle.RecordingChanged += (_, on) => UpdateRecordingUI(on);

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

            if (enableChat)
                await SetupChatAsync();

            WireRecordingButton();
            WireAnswerOptionsSource();
        }

        // ---------- Chat (textroom plugin over data channel) ----------

        async Task SetupChatAsync()
        {
            _textHandle = await _session.AttachAsync<TextRoomHandle>("vasili-textroom");
            _chat = new JanusTextRoomClient(this, _textHandle, BuildRtcConfiguration());
            _chat.ConnectionStateChanged += s => Debug.Log("[TextRoom] PC: " + s);
            _chat.MessageReceived += OnChatMessage;
            _chat.Announcement += (_, text) => AppendChatLine($"<i>{text}</i>");
            _chat.ParticipantJoined += (_, p) => AppendChatLine($"<i>{p.Display ?? p.Username} joined</i>");
            _chat.ParticipantLeft += (_, p) => AppendChatLine($"<i>{p.Display ?? p.Username} left</i>");
            _chat.Ready += async (_, __) =>
            {
                try
                {
                    await _chat.EnsureRoomAsync(Room, "VaSiLi room " + Room);
                    var username = string.IsNullOrEmpty(chatUsername) ? DisplayName + UnityEngine.Random.Range(1000, 9999) : chatUsername;
                    await _chat.JoinAsync(Room, username, DisplayName);
                    _chatReady = true;
                    AppendChatLine("<i>Chat ready</i>");
                    if (_pendingAnswerOptions != null)
                    {
                        SendAnswerOptions(_pendingAnswerOptions);
                        _pendingAnswerOptions = null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[TextRoom] Join failed: {ex.Message}");
                }
            };

            await _chat.SetupAsync();
            WireChatInput();
        }

        void WireChatInput()
        {
            if (chatSendButton != null)
                chatSendButton.onClick.AddListener(SendChatFromInput);
            if (chatInput != null)
                chatInput.onSubmit.AddListener(_ => SendChatFromInput());
        }

        void SendChatFromInput()
        {
            if (chatInput == null || !_chatReady) return;
            var text = (chatInput.text ?? "").Trim();
            if (text.Length == 0) return;
            _ = SendChatTextAsync(text);
            chatInput.text = "";
            chatInput.ActivateInputField();
        }

        public async Task SendChatTextAsync(string text)
        {
            if (_chat == null || !_chatReady) return;
            try
            {
                await _chat.SendChatAsync(text);
                AppendChatLine($"<b>You:</b> {text}");
            }
            catch (Exception ex) { Debug.LogError($"[TextRoom] Send failed: {ex.Message}"); }
        }

        void OnChatMessage(object _, TextRoomMessageEventArgs e)
        {
            // Skip our own echo (textroom acks back our own messages).
            if (_chat != null && e.From == _chat.Username) return;

            // Inspect for structured payloads (answer_options) sent as JSON in the text field.
            if (!string.IsNullOrEmpty(e.Text) && e.Text.StartsWith("{"))
            {
                try
                {
                    var probe = JsonConvert.DeserializeObject<AnswerOptionsPayload>(e.Text);
                    if (probe != null && probe.kind == "answer_options")
                    {
                        Debug.Log($"[TextRoom] answer_options received ({probe.data?.Length ?? 0} items)");
                        // Mirror them onto WebViewReceiver if there is one local.
                        if (answerOptionsSource != null && probe.data != null)
                            answerOptionsSource.OnAnswerOptionsReceived?.Invoke(probe.data);
                        return;
                    }
                }
                catch { /* not a structured payload — fall through */ }
            }

            AppendChatLine($"<b>{e.Display ?? e.From}:</b> {e.Text}");
        }

        void AppendChatLine(string line)
        {
            if (chatMessages == null) return;
            chatMessages.text += line + "\n";
            // Trim to last N lines to keep TMP from getting huge.
            var lines = chatMessages.text.Split('\n');
            if (lines.Length > chatMaxLines)
                chatMessages.text = string.Join("\n", lines, lines.Length - chatMaxLines, chatMaxLines);
        }

        // ---------- Recording ----------

        void WireRecordingButton()
        {
            if (recordButton != null)
                recordButton.onClick.AddListener(ToggleRecording);
            UpdateRecordingUI(false);
        }

        public async void ToggleRecording()
        {
            if (_publisherHandle == null) return;
            var next = !(_publisherHandle.IsRecording ?? false);
            try
            {
                await _publisherHandle.EnableRecordingAsync(Room, next);
                Debug.Log($"[VideoRoom] enable_recording sent (record={next})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VideoRoom] enable_recording failed: {ex.Message}");
            }
        }

        void UpdateRecordingUI(bool on)
        {
            if (recordIndicator != null) recordIndicator.SetActive(on);
            if (recordButtonRenderer != null && recordButtonRenderer.material != null)
            {
                var color = on ? recordActiveColor : recordIdleColor;
                recordButtonRenderer.material.color = color;
                recordButtonRenderer.material.SetColor("_BaseColor", color); // URP Lit
            }
            if (recordButtonLabel != null)
                recordButtonLabel.text = on ? recordActiveText : recordIdleText;
            OnRecordingChanged?.Invoke(on);
        }

        // ---------- Answer options forwarding ----------

        [Serializable]
        class AnswerOptionsPayload
        {
            public string kind;
            public string[] data;
        }

        void WireAnswerOptionsSource()
        {
            if (answerOptionsSource == null) return;
            if (answerOptionsSource.OnAnswerOptionsReceived == null)
                answerOptionsSource.OnAnswerOptionsReceived = new WebViewReceiver.AnswerOptionsEvent();
            answerOptionsSource.OnAnswerOptionsReceived.AddListener(SendAnswerOptions);
        }

        public void SendAnswerOptions(string[] options)
        {
            if (options == null || options.Length == 0) return;
            if (_chat == null || !_chatReady)
            {
                _pendingAnswerOptions = options;
                return;
            }
            var payload = JsonConvert.SerializeObject(new AnswerOptionsPayload { kind = "answer_options", data = options });
            _ = SendStructuredAsync(payload);
        }

        async Task SendStructuredAsync(string payload)
        {
            try { await _chat.SendChatAsync(payload); }
            catch (Exception ex) { Debug.LogError($"[TextRoom] answer_options send failed: {ex.Message}"); }
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
                if (answerOptionsSource != null && answerOptionsSource.OnAnswerOptionsReceived != null)
                    answerOptionsSource.OnAnswerOptionsReceived.RemoveListener(SendAnswerOptions);

                _chat?.Dispose();
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
