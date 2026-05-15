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

namespace VaSiLi.WebCam
{
    /// <summary>
    /// Janus VideoRoom client: publishes local audio/video, subscribes to remote feeds,
    /// and hosts the textroom data channel for chat and structured interviewer commands
    /// (answer options, gaze overlay, follow-up survey, remote logs).
    /// </summary>
    public class WebCamClient : MonoBehaviour
    {
        [Header("Janus")]
        public string ServerUrl = "wss://janus.faces.texttechnologylab.org";
        public string SubProtocol = "janus-protocol";
        public long Room = 1234;
        public string DisplayName = "test-user";
        [Tooltip("Base recording directory on the Janus server. Room ID is appended automatically. Leave empty to use Janus default.")]
        public string RecordingPath = "/mnt/recordings/";

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

        [Header("Microphone (Janus)")]
        [Tooltip("JanusMicrophoneSource component that owns the AudioSource fed into the WebRTC track. " +
                 "Create a child GameObject with AudioSource + JanusMicrophoneSource and assign it here.")]
        public JanusMicrophoneSource microphoneSource;

        [Header("Chat (optional)")]
        [Tooltip("If set, the client attaches a textroom plugin handle and joins the same room id.")]
        public bool enableChat = false;
        public string chatUsername = "";
        [Tooltip("TMP_Text where incoming/outgoing messages are appended.")]
        public TMP_Text chatMessages;
        public TMP_InputField chatInput;
        public Button chatSendButton;
        public int chatMaxLines = 50;
        [Tooltip("Fired with the current unread count whenever it changes. Use to drive a badge counter.")]
        public UnityEvent<int> OnUnreadCountChanged;

        [Header("Recording (optional)")]
        [Tooltip("Button toggles recording state via the videoroom plugin.")]
        public Button recordButton;
        [Tooltip("Activated when recording is on.")]
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

        [Header("Gaze Overlay (Interviewer)")]
        [Tooltip("Button that toggles the gaze calibration overlay on the remote side.")]
        public Button gazeOverlayButton;
        [Tooltip("Text shown at the top-left of the interviewee gaze overlay.")]
        public string gazeTextTopLeft = "Bitte lesen Sie diesen Text laut vor.";
        [Tooltip("Text shown at the bottom-right of the interviewee gaze overlay.")]
        public string gazeTextBottomRight = "Bitte lesen Sie auch diesen Satz laut vor.";
        [Tooltip("Fired on this client when a gaze_overlay command is received from the interviewer. Args: show, textTopLeft, textBottomRight.")]
        public GazeOverlayEvent OnGazeOverlayReceived;

        [Header("Follow-up Survey (Interviewer)")]
        [Tooltip("Button that sends the follow-up survey URL to the remote participant.")]
        public Button followupButton;
        [Tooltip("URL sent to the interviewee when the follow-up button is pressed.")]
        public string followupUrl = "";
        [Tooltip("Fired on this client when a followup_survey URL is received. Arg: url.")]
        public UnityEvent<string> OnFollowupSurveyReceived;

        [Header("Remote Logs (Interviewee)")]
        [Tooltip("Periodically forwards Unity log output to the interviewer via the textroom.")]
        public bool enableRemoteLogs = false;
        [Tooltip("How often (in seconds) to batch and send buffered log lines.")]
        public float remoteLogIntervalSeconds = 5f;
        [Tooltip("Fired on this client when a remote-log batch is received from the interviewee.")]
        public UnityEvent<string> OnRemoteLogReceived;

        // ---------- Serializable event types ----------

        [Serializable] public class GazeOverlayEvent : UnityEvent<bool, string, string> { }

        // ---------- Public accessors ----------

        public JanusConnection Connection => _connection;
        public JanusSession Session => _session;
        public VideoRoomHandle PublisherHandle => _publisherHandle;
        public JanusTextRoomClient Chat => _chat;
        public int UnreadChatCount => _unreadCount;

        // ---------- Private state ----------

        JanusConnection _connection;
        JanusSession _session;
        VideoRoomHandle _publisherHandle;
        VideoRoomHandle _subscriberHandle;
        JanusVideoRoomPublisher _publisher;
        JanusVideoRoomSubscriber _subscriber;
        TextRoomHandle _textHandle;
        JanusTextRoomClient _chat;
        bool _chatReady;
        bool _chatOpen;
        int _unreadCount;
        string[] _pendingAnswerOptions;
        bool _gazeOverlayOn;
        readonly List<string> _pendingLogLines = new List<string>();
        bool _buttonsWired;

        AudioStreamTrack _audioTrack;

        readonly Dictionary<long, RemotePublisher> _knownPublishers = new Dictionary<long, RemotePublisher>();
        readonly Dictionary<long, List<StreamSubscription>> _publisherStreams = new Dictionary<long, List<StreamSubscription>>();

        // ---------- State ----------

        public bool IsConnected => _connection != null && _connection.IsConnected;

        // ---------- Unity lifecycle ----------

        void Start()
        {
            StartCoroutine(WebRTC.Update());

            // Apply room ID from the token manager if available.
            if (InterviewTokenManager.Instance != null && InterviewTokenManager.Instance.RoomId != 0)
                Room = InterviewTokenManager.Instance.RoomId;
        }


        void OnDestroy()
        {
            Disconnect();
        }

        // ---------- Connection ----------

        /// <summary>Start the Janus connection. Safe to call when already connected (no-op).</summary>
        public async void Connect()
        {
            if (IsConnected) { Debug.Log("[WebCamClient] Connect() called but already connected — ignoring"); return; }
            if (InterviewTokenManager.Instance != null && InterviewTokenManager.Instance.RoomId != 0)
                Room = InterviewTokenManager.Instance.RoomId;
            Debug.Log($"[WebCamClient] Connecting to room {Room} ({ServerUrl})");
            try { await ConnectAndJoin(); }
            catch (Exception ex) { Debug.LogError($"[WebCamClient] Connect failed: {ex.Message}\n{ex.StackTrace}"); }
        }

        /// <summary>Stop and clean up the Janus connection.</summary>
        public async void Disconnect()
        {
            Debug.Log("[WebCamClient] Disconnecting");
            try
            {
                Application.logMessageReceived -= BufferLogMessage;
                CancelInvoke(nameof(FlushRemoteLogs));
                if (answerOptionsSource != null && answerOptionsSource.OnAnswerOptionsReceived != null)
                    answerOptionsSource.OnAnswerOptionsReceived.RemoveListener(SendAnswerOptions);

                _chat?.Dispose();  _chat = null;  _chatReady = false;
                _publisher?.Dispose();  _publisher = null;
                _subscriber?.Dispose();  _subscriber = null;  _subscriberHandle = null;
                _audioTrack?.Dispose();  _audioTrack = null;
                _knownPublishers.Clear();
                _publisherStreams.Clear();
                if (_connection != null) { await _connection.CloseAsync(); _connection = null; }
            }
            catch (Exception ex) { Debug.LogWarning($"[WebCamClient] Disconnect error: {ex.Message}"); }
        }

        async Task ConnectAndJoin()
        {
            var transport = new JanusWebSocketTransport(new Uri(ServerUrl), SubProtocol);
            _connection = new JanusConnection(transport, new JanusConnectionOptions
            {
                KeepaliveIntervalMs = 25_000,
                Reconnect = new JanusReconnectOptions { Enabled = true }
            });

            _connection.Connected       += (_, __) => Debug.Log("[Janus] connected");
            _connection.Disconnected    += (_, r)  => Debug.LogWarning($"[Janus] disconnected: {r}");
            _connection.Reconnecting    += (_, __) => Debug.Log("[Janus] reconnecting…");
            _connection.Reconnected     += (_, __) => Debug.Log("[Janus] reconnected, sessions reclaimed");
            _connection.ReconnectFailed += (_, r)  => Debug.LogError($"[Janus] reconnect gave up: {r}");
            _connection.InternalError   += (_, ex) => Debug.LogError($"[Janus] internal error: {ex.Message}");

            await _connection.OpenAsync();
            _session = await _connection.CreateSessionAsync();

            _publisherHandle = await _session.AttachAsync<VideoRoomHandle>("vasili-publisher");
            _publisherHandle.RoomJoined         += (_, __) => { _knownPublishers.Clear(); _publisherStreams.Clear(); };
            _publisherHandle.PublishersAdded    += OnPublishersAdded;
            _publisherHandle.PublisherLeft      += OnPublisherLeft;
            _publisherHandle.PublisherUnpublished += OnPublisherLeft;
            _publisherHandle.RoomDestroyed      += (_, e) => Debug.LogWarning($"[VideoRoom] room {e.Room} destroyed");
            _publisherHandle.RecordingChanged   += (_, on) => UpdateRecordingUI(on);

            // Build rec_dir using the original token string when available (matches website behaviour),
            // falling back to the numeric room ID.
            var tokenLabel = (InterviewTokenManager.Instance != null && !string.IsNullOrEmpty(InterviewTokenManager.Instance.Token))
                ? InterviewTokenManager.Instance.Token
                : Room.ToString();
            var recDir = string.IsNullOrEmpty(RecordingPath)
                ? null
                : RecordingPath.TrimEnd('/') + "/" + tokenLabel + "/";

            var joined = await _publisherHandle.JoinOrCreateAsync(
                Room,
                DisplayName,
                new CreateRoomRequest
                {
                    Description  = "VaSiLi room " + Room,
                    Publishers   = 12,
                    Bitrate      = 0,
                    FirFreq      = 10,
                    AudioCodec   = "opus",
                    VideoCodec   = "vp8,h264,vp9",
                    NotifyJoining = false,
                    RecDir       = recDir
                });
            Debug.Log($"[VideoRoom] joined room {Room} as id={joined.Id}, private_id={joined.PrivateId}");

            await StartPublishing();

            if (joined.Publishers != null && joined.Publishers.Count > 0)
                await SubscribeToAvailable(joined.Publishers);

            if (enableChat)
                await SetupChatAsync();

            if (!_buttonsWired)
            {
                WireRecordingButton();
                WireAnswerOptionsSource();
                WireGazeButton();
                WireFollowupButton();
                _buttonsWired = true;
            }
        }

        // ---------- Chat ----------

        async Task SetupChatAsync()
        {
            _textHandle = await _session.AttachAsync<TextRoomHandle>("vasili-textroom");
            _chat = new JanusTextRoomClient(this, _textHandle, BuildRtcConfiguration());
            _chat.ConnectionStateChanged += s => Debug.Log("[TextRoom] PC: " + s);
            _chat.MessageReceived        += OnChatMessage;
            _chat.Announcement           += (_, text) => AppendChatLine($"<i>{text}</i>");
            _chat.ParticipantJoined      += (_, p) => AppendChatLine($"<i>{p.Display ?? p.Username} joined</i>");
            _chat.ParticipantLeft        += (_, p) => AppendChatLine($"<i>{p.Display ?? p.Username} left</i>");
            _chat.Ready += async (_, __) =>
            {
                try
                {
                    await _chat.EnsureRoomAsync(Room, "VaSiLi room " + Room);
                    var username = string.IsNullOrEmpty(chatUsername)
                        ? DisplayName + UnityEngine.Random.Range(1000, 9999)
                        : chatUsername;
                    await _chat.JoinAsync(Room, username, DisplayName);
                    _chatReady = true;
                    AppendChatLine("<i>Chat ready</i>");

                    if (_pendingAnswerOptions != null)
                    {
                        SendAnswerOptions(_pendingAnswerOptions);
                        _pendingAnswerOptions = null;
                    }
                    if (enableRemoteLogs)
                        StartRemoteLogs();
                }
                catch (Exception ex) { Debug.LogError($"[TextRoom] Join failed: {ex.Message}"); }
            };

            await _chat.SetupAsync();
            WireChatInput();
        }

        void WireChatInput()
        {
            if (chatSendButton != null) chatSendButton.onClick.AddListener(SendChatFromInput);
            if (chatInput != null)     chatInput.onSubmit.AddListener(_ => SendChatFromInput());
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
            // Suppress our own echo.
            if (_chat != null && e.From == _chat.Username) return;

            // Structured JSON payloads.
            if (!string.IsNullOrEmpty(e.Text) && e.Text.StartsWith("{"))
            {
                try
                {
                    var probe = JsonConvert.DeserializeObject<KindProbe>(e.Text);
                    switch (probe?.kind)
                    {
                        case "answer_options":
                            var ao = JsonConvert.DeserializeObject<AnswerOptionsPayload>(e.Text);
                            if (answerOptionsSource != null && ao?.data != null)
                                answerOptionsSource.OnAnswerOptionsReceived?.Invoke(ao.data);
                            return;

                        case "gaze_overlay":
                            var go = JsonConvert.DeserializeObject<GazeOverlayPayload>(e.Text);
                            if (go != null)
                            {
                                _gazeOverlayOn = go.show;
                                OnGazeOverlayReceived?.Invoke(go.show, go.textTopLeft, go.textBottomRight);
                            }
                            return;

                        case "followup_survey":
                            var fs = JsonConvert.DeserializeObject<FollowupPayload>(e.Text);
                            if (fs != null) OnFollowupSurveyReceived?.Invoke(fs.url);
                            return;
                    }
                }
                catch { /* fall through to plain-text handling */ }
            }

            // Remote log batches: lines start with "[HH:MM:SS] LEVEL:" format.
            if (!string.IsNullOrEmpty(e.Text) && e.Text.StartsWith("[") && e.Text.Contains("]"))
            {
                OnRemoteLogReceived?.Invoke(e.Text);
                return;
            }

            // Regular chat message.
            if (!_chatOpen)
            {
                _unreadCount++;
                OnUnreadCountChanged?.Invoke(_unreadCount);
            }
            AppendChatLine($"<b>{e.Display ?? e.From}:</b> {e.Text}");
        }

        void AppendChatLine(string line)
        {
            if (chatMessages == null) return;
            chatMessages.text += line + "\n";
            var lines = chatMessages.text.Split('\n');
            if (lines.Length > chatMaxLines)
                chatMessages.text = string.Join("\n", lines, lines.Length - chatMaxLines, chatMaxLines);
        }

        /// <summary>Call when the chat panel is opened or closed to manage the unread badge.</summary>
        public void SetChatOpen(bool open)
        {
            _chatOpen = open;
            if (open) MarkChatRead();
        }

        public void MarkChatRead()
        {
            if (_unreadCount == 0) return;
            _unreadCount = 0;
            OnUnreadCountChanged?.Invoke(0);
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
            if (_publisherHandle == null) { Debug.LogWarning("[WebCamClient] ToggleRecording: no publisher handle"); return; }
            var next = !(_publisherHandle.IsRecording ?? false);
            Debug.Log($"[WebCamClient] ToggleRecording → record={next} room={Room}");
            try
            {
                await _publisherHandle.EnableRecordingAsync(Room, next);
                Debug.Log($"[WebCamClient] enable_recording sent (record={next})");
                // Janus sends the 'record' event to other participants, not back to the sender.
                // Update our own state and UI immediately after the request succeeds.
                _publisherHandle.SetIsRecording(next);
                UpdateRecordingUI(next);
            }
            catch (Exception ex) { Debug.LogError($"[WebCamClient] enable_recording failed: {ex.Message}"); }
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

        // ---------- Gaze overlay ----------

        void WireGazeButton()
        {
            if (gazeOverlayButton != null)
                gazeOverlayButton.onClick.AddListener(() => _ = SendGazeOverlayAsync(!_gazeOverlayOn));
        }

        /// <summary>Send a gaze calibration overlay command to all room participants.</summary>
        public async Task SendGazeOverlayAsync(bool show, string textTopLeft = null, string textBottomRight = null)
        {
            if (_chat == null || !_chatReady) { Debug.LogWarning($"[WebCamClient] SendGazeOverlay: chat not ready (chatReady={_chatReady})"); return; }
            Debug.Log($"[WebCamClient] SendGazeOverlay show={show}");
            _gazeOverlayOn = show;
            var payload = JsonConvert.SerializeObject(new GazeOverlayPayload
            {
                show           = show,
                textTopLeft    = textTopLeft    ?? gazeTextTopLeft,
                textBottomRight = textBottomRight ?? gazeTextBottomRight
            }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            try { await _chat.SendChatAsync(payload); }
            catch (Exception ex) { Debug.LogError($"[WebCam] gaze_overlay send failed: {ex.Message}"); }
        }

        // ---------- Follow-up survey ----------

        void WireFollowupButton()
        {
            if (followupButton != null)
                followupButton.onClick.AddListener(() => _ = SendFollowupSurveyAsync(followupUrl));
        }

        /// <summary>Send a follow-up survey URL to all room participants.</summary>
        public async Task SendFollowupSurveyAsync(string url)
        {
            if (_chat == null || !_chatReady || string.IsNullOrEmpty(url)) { Debug.LogWarning($"[WebCamClient] SendFollowup: chat not ready or url empty (chatReady={_chatReady}, url='{url}')"); return; }
            Debug.Log($"[WebCamClient] SendFollowup url={url}");
            var payload = JsonConvert.SerializeObject(new FollowupPayload { url = url },
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            try { await _chat.SendChatAsync(payload); }
            catch (Exception ex) { Debug.LogError($"[WebCam] followup_survey send failed: {ex.Message}"); }
        }

        // ---------- Remote logs ----------

        void StartRemoteLogs()
        {
            Application.logMessageReceived += BufferLogMessage;
            InvokeRepeating(nameof(FlushRemoteLogs), remoteLogIntervalSeconds, remoteLogIntervalSeconds);
        }

        void BufferLogMessage(string condition, string stackTrace, LogType type)
        {
            var level = type switch
            {
                LogType.Error     => "ERROR",
                LogType.Assert    => "ASSERT",
                LogType.Warning   => "WARN",
                LogType.Exception => "EXCEPTION",
                _                 => "LOG"
            };
            _pendingLogLines.Add($"[{DateTime.Now:HH:mm:ss}] {level}: {condition}");
        }

        async void FlushRemoteLogs()
        {
            if (!_chatReady || _pendingLogLines.Count == 0) return;
            var text = string.Join("\n", _pendingLogLines);
            _pendingLogLines.Clear();
            try { await _chat.SendChatAsync(text); }
            catch (Exception ex) { Debug.LogWarning($"[WebCam] remote log flush failed: {ex.Message}"); }
        }

        // ---------- Answer options ----------

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
            if (_chat == null || !_chatReady) { _pendingAnswerOptions = options; return; }
            var payload = JsonConvert.SerializeObject(new AnswerOptionsPayload { data = options });
            _ = SendStructuredAsync(payload);
        }

        async Task SendStructuredAsync(string payload)
        {
            try { await _chat.SendChatAsync(payload); }
            catch (Exception ex) { Debug.LogError($"[TextRoom] send failed: {ex.Message}"); }
        }

        // ---------- Payload types ----------

        [Serializable] class KindProbe        { public string kind; }
        [Serializable] class AnswerOptionsPayload { public string kind = "answer_options"; public string[] data; }
        [Serializable] class GazeOverlayPayload   { public string kind = "gaze_overlay";   public bool show; public string textTopLeft; public string textBottomRight; }
        [Serializable] class FollowupPayload       { public string kind = "followup_survey"; public string url; }

        // ---------- RTC configuration ----------

        RTCConfiguration BuildRtcConfiguration() => new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer
                {
                    urls       = new[] { TurnUrl },
                    credential = TurnCredential,
                    username   = TurnUsername
                }
            }
        };

        // ---------- Publishing ----------

        async Task StartPublishing()
        {
            var rtcConfig = BuildRtcConfiguration();
            _publisher = new JanusVideoRoomPublisher(this, _publisherHandle, rtcConfig);
            _publisher.IceStateChanged        += s => Debug.Log("[Publisher] ICE: " + s);
            _publisher.ConnectionStateChanged += s => Debug.Log("[Publisher] PC: " + s);

            var sendStream = new MediaStream();
            // AudioStreamTrack(AudioSource) hooks OnAudioFilterRead internally at the native
            // 48 kHz audio-thread cadence — no resampling, no main-thread timing jitter.
            if (microphoneSource != null)
                _audioTrack = new AudioStreamTrack(microphoneSource.AudioSource);
            else
                _audioTrack = new AudioStreamTrack();
            _publisher.AddTrack(_audioTrack, sendStream);

            var videoTrack  = captureCamera.CaptureStreamTrack(captureWidth, captureHeight);
            var videoSender = _publisher.AddTrack(videoTrack, sendStream);

            var parameters = videoSender.GetParameters();
            foreach (var enc in parameters.encodings)
            {
                enc.maxFramerate = (uint)maxFramerate;
                enc.maxBitrate   = (ulong)maxBitrate;
                enc.minBitrate   = (ulong)minBitrate;
            }
            videoSender.SetParameters(parameters);

            await _publisher.PublishAsync(new ConfigureRequest { Audio = true, Video = true });
            Debug.Log("[Publisher] published");
        }

        // ---------- Subscribing ----------

        async void OnPublishersAdded(object sender, PublishersAddedEventArgs e)
        {
            try { await SubscribeToAvailable(e.Publishers); }
            catch (Exception ex) { Debug.LogError($"[Subscriber] subscribe failed: {ex.Message}"); }
        }

        async void OnPublisherLeft(object sender, PublisherLeftEventArgs e)
        {
            Debug.Log($"[VideoRoom] publisher {e.Id} left/unpublished — tearing down subscriber so rejoin gets a clean slate");
            _knownPublishers.Remove(e.Id);
            _publisherStreams.Remove(e.Id);

            if (frame != null)
            {
                frame.material.mainTexture = null;
                frame.material.SetTexture("_BaseMap", null);
            }

            // Dispose the subscriber completely. When the remote peer rejoins, SubscribeToAvailable
            // will see _subscriber == null and do a fresh attach + join, which is the only reliable
            // path. Trying to update/unsubscribe on the existing handle leaves it in an ambiguous
            // SDP state that prevents the renegotiation from succeeding on rejoin.
            if (_subscriber != null)
            {
                _subscriber.Dispose();
                _subscriber = null;
                try { await _subscriberHandle.DetachAsync(); } catch { }
                _subscriberHandle = null;
            }
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
                var subs = new List<StreamSubscription>();
                foreach (var s in p.Streams)
                {
                    var sub = new StreamSubscription { Feed = p.Id, Mid = s.Mid };
                    subs.Add(sub);
                    newStreams.Add(sub);
                }
                _publisherStreams[p.Id] = subs;
            }
            if (newStreams.Count == 0) return;

            if (_subscriber == null)
            {
                _subscriberHandle = await _session.AttachAsync<VideoRoomHandle>("vasili-subscriber");
                _subscriber = new JanusVideoRoomSubscriber(this, _subscriberHandle, BuildRtcConfiguration());
                _subscriber.RemoteTrackAdded      += OnRemoteTrack;
                _subscriber.IceStateChanged       += s => Debug.Log("[Subscriber] ICE: " + s);
                _subscriber.ConnectionStateChanged += s => Debug.Log("[Subscriber] PC: " + s);
                await _subscriber.SubscribeAsync(Room, newStreams);
            }
            else if (_subscriber.IsJoined)
            {
                await _subscriber.UpdateAsync(newStreams, null);
            }
            else
            {
                // Subscriber exists but is mid-restore (SDP exchange not complete); call SubscribeAsync
                // so it does a fresh join rather than an update on an un-joined handle.
                await _subscriber.SubscribeAsync(Room, newStreams);
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
                        frame.material.SetTexture("_BaseMap", texture); // URP Lit
                    }
                };
            }
        }
    }
}
