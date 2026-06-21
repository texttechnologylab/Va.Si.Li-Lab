using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        [Tooltip("List of STUN/TURN servers used for WebRTC. Leave username/credential empty for STUN servers.")]
        public List<IceServerConfig> IceServers = new List<IceServerConfig>
        {
            new IceServerConfig
            {
                urls       = new List<string> { "turn:turn.vasililab.texttechnologylab.org:3478" },
                username   = "testuser",
                credential = ""
            }
        };

        [Serializable]
        public class IceServerConfig
        {
            [Tooltip("One or more URLs for this server, e.g. \"stun:stun.l.google.com:19302\" or \"turn:host:3478\".")]
            public List<string> urls = new List<string>();
            [Tooltip("Username for TURN authentication. Leave empty for STUN.")]
            public string username = "";
            [Tooltip("Credential/password for TURN authentication. Leave empty for STUN.")]
            public string credential = "";
        }

        [Header("Capture")]
        public Camera captureCamera;
        public int captureWidth = 854;
        public int captureHeight = 480;
        public int maxBitrate = 1_000_000;
        public int minBitrate = 250_000;
        public int maxFramerate = 24;

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
        [Tooltip("ScrollRect that wraps the chat content. The TMP_Text must be on the ScrollRect's content child.")]
        public ScrollRect chatScrollRect;
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

        [Header("Answer-Option Ack Pill (Interviewer)")]
        [Tooltip("Root of the header chip; shown only once at least one batch has been sent.")]
        public GameObject ackPill;
        [Tooltip("Background graphic of the pill. Turns orange if any batch ever failed to be acked.")]
        public Graphic ackPillBackground;
        [Tooltip("Status icon. Reflects the MOST RECENT batch (check / warn / spinner).")]
        public Image ackIcon;
        [Tooltip("Label showing acked/total, e.g. \"2/3\".")]
        public TMP_Text ackCountLabel;
        public Sprite ackCheckSprite;
        public Sprite ackWarnSprite;
        public Sprite ackSpinnerSprite;
        [Tooltip("How long (seconds) to wait for an ack before marking a batch failed.")]
        public float ackTimeoutSeconds = 5f;
        public Color ackGreen = new Color(0.30f, 0.78f, 0.31f);
        public Color ackGrey = new Color(0.62f, 0.62f, 0.62f);
        public Color ackNeutralGrey = new Color(0.20f, 0.20f, 0.20f);
        public Color ackOrange = new Color(0.95f, 0.55f, 0.10f);

        [Header("Gaze Overlay (Interviewer)")]
        [Tooltip("Button that toggles the gaze calibration overlay on the remote side.")]
        public Button gazeOverlayButton;
        [Tooltip("Activated while the gaze overlay is currently showing on the remote side.")]
        public GameObject gazeIndicator;
        [Tooltip("Optional renderer whose material color is swapped between idle and active.")]
        public Renderer gazeButtonRenderer;
        public Color gazeIdleColor = Color.white;
        public Color gazeActiveColor = Color.green;
        public TMP_Text gazeButtonLabel;
        public string gazeIdleText = "Kalibrierung";
        public string gazeActiveText = "Kalibrierung aktiv";
        [Tooltip("Fires with the current overlay state whenever it changes. Use to drive any extra UI not covered by the indicator/renderer/label fields above.")]
        public UnityEvent<bool> OnGazeOverlayChanged;
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
        [Tooltip("Comma-separated substrings (case-insensitive). A log line is forwarded if it " +
                 "contains ANY of them. Leave empty to forward everything. Default matches the " +
                 "WebRTC/Janus triage prefixes.")]
        public string remoteLogFilter = "Janus,Publisher,Subscriber,TextRoom,VideoRoom,WebCam";
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
        string[] _lastAnswerOptions;
        readonly AckTracker _ackTracker = new AckTracker();
        bool _gazeOverlayOn;
        readonly List<string> _pendingLogLines = new List<string>();
        bool _buttonsWired;

        AudioStreamTrack _audioTrack;

        readonly Dictionary<long, RemotePublisher> _knownPublishers = new Dictionary<long, RemotePublisher>();
        readonly Dictionary<long, List<StreamSubscription>> _publisherStreams = new Dictionary<long, List<StreamSubscription>>();

        // Serializes (re)subscribe vs. unsubscribe so their multi-await SDP sequences can't
        // interleave on the shared subscriber PeerConnection.
        readonly SemaphoreSlim _subLifecycleLock = new SemaphoreSlim(1, 1);

        // ---------- State ----------

        public bool IsConnected => _connection != null && _connection.IsConnected;

        // ---------- Unity lifecycle ----------

        void Start()
        {
            StartCoroutine(WebRTC.Update());

            _ackTracker.Changed += OnAckEntriesChanged;
            if (ackPill != null) ackPill.SetActive(false);

            // Apply room ID from the token manager if available.
            if (InterviewTokenManager.Instance != null && InterviewTokenManager.Instance.RoomId != 0)
                Room = InterviewTokenManager.Instance.RoomId;
        }

        void Update()
        {
            // Drives the ack timeout — the Unity-idiomatic replacement for setTimeout (no threads).
            _ackTracker.Tick((long)(ackTimeoutSeconds * 1000));
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
                if (microphoneSource != null) microphoneSource.OnAudioRead -= ForwardMicAudio;
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
                    Publishers   = 6,
                    Bitrate      = 0,
                    FirFreq      = 10,
                    AudioCodec   = "opus",
                    // Priority order; Janus (SFU, no transcode) picks the first codec each
                    // publisher's offer supports. Browsers that can't do vp9/h265 fall through
                    // to h264. The Quest can hardware en/decode all of these.
                    VideoCodec   = "h264,vp8",
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
            Debug.Log("[TextRoom] Attaching textroom handle");
            _textHandle = await _session.AttachAsync<TextRoomHandle>("vasili-textroom");
            _chat = new JanusTextRoomClient(this, _textHandle, BuildRtcConfiguration());
            _chat.ConnectionStateChanged += s => Debug.Log("[TextRoom] PC: " + s);
            _chat.MessageReceived        += OnChatMessage;
            _chat.Announcement           += (_, text) => AppendChatLine($"<i>{text}</i>");
            _chat.ParticipantJoined      += (_, p) =>
            {
                AppendChatLine($"<i>{p.Display ?? p.Username} joined</i>");
                // Push-on-join: re-send the current answer categories as a fresh tracked batch
                // (new msgId → fresh ack) so a participant who joined or reloaded gets the
                // current options and the interviewer gets fresh confirmation.
                if (_lastAnswerOptions != null && _lastAnswerOptions.Length > 0)
                    SendAnswerOptions(_lastAnswerOptions);
            };
            _chat.ParticipantLeft        += (_, p) => AppendChatLine($"<i>{p.Display ?? p.Username} left</i>");
            _chat.Ready += async (_, __) =>
            {
                Debug.Log("[TextRoom] Ready fired — starting join sequence");
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                try
                {
                    await _chat.EnsureRoomAsync(Room, "VaSiLi room " + Room, cts.Token);
                    Debug.Log("[TextRoom] EnsureRoom done");
                    var username = string.IsNullOrEmpty(chatUsername)
                        ? DisplayName + UnityEngine.Random.Range(1000, 9999)
                        : chatUsername;
                    await _chat.JoinAsync(Room, username, DisplayName, cts.Token);
                    Debug.Log("[TextRoom] JoinAsync done — chat is ready");
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
                catch (OperationCanceledException)
                {
                    Debug.LogError("[TextRoom] Join sequence timed out (15 s) — data channel may not be routing to Janus textroom plugin correctly");
                }
                catch (Exception ex) { Debug.LogError($"[TextRoom] Join failed: {ex.Message}\n{ex.StackTrace}"); }
            };

            Debug.Log("[TextRoom] Starting SDP setup (SetupAsync)");
            await _chat.SetupAsync();
            Debug.Log("[TextRoom] SetupAsync complete — waiting for data channel to open");
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
                                answerOptionsSource.DisplayOptions(ao.data);
                            // Interviewee acknowledges the batch right after rendering, so the
                            // interviewer's pill can confirm receipt. Skip if no msgId (older sender).
                            if (!string.IsNullOrEmpty(ao?.msgId))
                                _ = SendAckAsync(ao.msgId);
                            return;

                        case "ack":
                            // Interviewer side: correlate the interviewee's ack to a tracked batch.
                            var ack = JsonConvert.DeserializeObject<AckPayload>(e.Text);
                            if (ack != null) _ackTracker.Ack(ack.ackOf);
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

        TMP_Text _chatText; // cached reference to the TMP_Text inside the ScrollRect content

        void AppendChatLine(string line)
        {
            if (chatScrollRect == null || chatScrollRect.content == null) return;
            
            // Handle both cases: TMP_Text on the Content object itself or as a child.
            if (_chatText == null)
                _chatText = chatScrollRect.content.GetComponentInChildren<TMP_Text>();
            if (_chatText == null) return;

            _chatText.text += line + "\n";
            var lines = _chatText.text.Split('\n');
            if (lines.Length > chatMaxLines)
                _chatText.text = string.Join("\n", lines, lines.Length - chatMaxLines, chatMaxLines);

            // Force layout rebuild so the ContentSizeFitter updates the height immediately.
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);
            
            // Stay grounded: move scroll to the bottom (0 is bottom in a standard vertical ScrollRect).
            chatScrollRect.verticalNormalizedPosition = 0f;
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
            UpdateGazeOverlayUI(false);
        }

        /// <summary>Send a gaze calibration overlay command to all room participants.</summary>
        public async Task SendGazeOverlayAsync(bool show, string textTopLeft = null, string textBottomRight = null)
        {
            if (_chat == null || !_chatReady) { Debug.LogWarning($"[WebCamClient] SendGazeOverlay: chat not ready (chatReady={_chatReady})"); return; }
            Debug.Log($"[WebCamClient] SendGazeOverlay show={show}");
            // Update local state + UI BEFORE awaiting the send. SendChatAsync uses textroom
            // `ack: false` (matching the website) so Janus never sends a transaction-matched
            // response and the awaited Task never completes — anything after the await would
            // never run, which previously left the toggle stuck on "active" because !_gazeOverlayOn
            // kept reading the un-updated value.
            _gazeOverlayOn = show;
            UpdateGazeOverlayUI(show);
            var payload = JsonConvert.SerializeObject(new GazeOverlayPayload
            {
                show           = show,
                textTopLeft    = textTopLeft    ?? gazeTextTopLeft,
                textBottomRight = textBottomRight ?? gazeTextBottomRight
            }, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            try { await _chat.SendChatAsync(payload); }
            catch (Exception ex) { Debug.LogError($"[WebCam] gaze_overlay send failed: {ex.Message}"); }
        }

        void UpdateGazeOverlayUI(bool on)
        {
            if (gazeIndicator != null) gazeIndicator.SetActive(on);
            if (gazeButtonRenderer != null && gazeButtonRenderer.material != null)
            {
                var color = on ? gazeActiveColor : gazeIdleColor;
                gazeButtonRenderer.material.color = color;
                gazeButtonRenderer.material.SetColor("_BaseColor", color); // URP Lit
            }
            if (gazeButtonLabel != null)
                gazeButtonLabel.text = on ? gazeActiveText : gazeIdleText;
            OnGazeOverlayChanged?.Invoke(on);
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
            if (InterviewTokenManager.Instance != null && !string.IsNullOrEmpty(InterviewTokenManager.Instance.Token))
            {
                var token = InterviewTokenManager.Instance.Token;
                url += (url.Contains('?') ? "&" : "?") + "token=" + Uri.EscapeDataString(token);
            }
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
            // Only forward lines matching the filter so triage chat isn't flooded.
            // Empty filter = forward everything.
            if (!MatchesRemoteLogFilter(condition))
                return;

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

        string[] _remoteLogFilterTerms;
        string _remoteLogFilterCache;

        bool MatchesRemoteLogFilter(string condition)
        {
            if (string.IsNullOrEmpty(remoteLogFilter)) return true; // no filter = forward all
            if (string.IsNullOrEmpty(condition)) return false;

            // Re-parse the comma-separated list only when the Inspector value changes.
            if (!ReferenceEquals(_remoteLogFilterCache, remoteLogFilter))
            {
                _remoteLogFilterCache = remoteLogFilter;
                _remoteLogFilterTerms = remoteLogFilter
                    .Split(',')
                    .Select(t => t.Trim())
                    .Where(t => t.Length > 0)
                    .ToArray();
            }

            foreach (var term in _remoteLogFilterTerms)
                if (condition.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            return false;
        }

        async void FlushRemoteLogs()
        {
            if (!_chatReady || _pendingLogLines.Count == 0) return;
            var text = string.Join("\n", _pendingLogLines);
            _pendingLogLines.Clear();
            try { await _chat.SendChatAsync(text); }
            // Prefix deliberately avoids the default remote-log filter terms so a failing flush
            // can't buffer its own error and amplify into a feedback loop.
            catch (Exception ex) { Debug.LogWarning($"[RemoteLogFlush] failed: {ex.Message}"); }
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
            // Remember the latest batch so we can re-send (with a fresh msgId) to participants
            // who join or reload after the original send. See push-on-join below.
            _lastAnswerOptions = options;
            if (_chat == null || !_chatReady) { _pendingAnswerOptions = options; return; }

            var meta = new Dictionary<string, object> { { "count", options.Length } };
            var msgId = _ackTracker.Track(meta);
            var payload = JsonConvert.SerializeObject(new AnswerOptionsPayload { msgId = msgId, data = options });
            _ = SendStructuredAsync(payload);
        }

        /// <summary>Interviewee → interviewer confirmation that a batch was received and rendered.</summary>
        Task SendAckAsync(string ackOf)
        {
            if (_chat == null || !_chatReady) return Task.CompletedTask;
            var payload = JsonConvert.SerializeObject(new AckPayload { ackOf = ackOf },
                new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            return SendStructuredAsync(payload);
        }

        async Task SendStructuredAsync(string payload)
        {
            try { await _chat.SendChatAsync(payload); }
            catch (Exception ex) { Debug.LogError($"[TextRoom] send failed: {ex.Message}"); }
        }

        // ---------- Answer-option ack pill (Interviewer) ----------

        void OnAckEntriesChanged(IReadOnlyList<AckEntry> entries)
        {
            if (ackPill != null) ackPill.SetActive(entries.Count > 0);
            if (entries.Count == 0) return;

            var latest = entries[entries.Count - 1];
            bool hadFailure = entries.Any(e => e.Status == AckStatus.Failed);
            int acked = entries.Count(e => e.Status == AckStatus.Acked);

            // ICON = status of the MOST RECENT batch, so the newest send is always readable.
            if (ackIcon != null)
            {
                ackIcon.sprite = latest.Status switch
                {
                    AckStatus.Acked  => ackCheckSprite,
                    AckStatus.Failed => ackWarnSprite,
                    _                => ackSpinnerSprite,
                };

                // On the orange pill keep the icon white for legibility; otherwise colour by
                // the latest status. Decoupled from the pill background on purpose.
                ackIcon.color = hadFailure ? Color.white
                              : latest.Status == AckStatus.Acked ? ackGreen
                              : ackGrey;
            }

            // PILL background = orange if ANY batch EVER failed (persistent error signal),
            // independent of the icon, so one old failure can't be masked by a newer success.
            if (ackPillBackground != null)
                ackPillBackground.color = hadFailure ? ackOrange : ackNeutralGrey;

            if (ackCountLabel != null)
                ackCountLabel.text = $"{acked}/{entries.Count}";
        }

        // ---------- Payload types ----------

        [Serializable] class KindProbe        { public string kind; }
        [Serializable] class AnswerOptionsPayload { public string kind = "answer_options"; public string msgId; public string[] data; }
        [Serializable] class AckPayload           { public string kind = "ack"; public string ackOf; public string status = "received"; }
        [Serializable] class GazeOverlayPayload   { public string kind = "gaze_overlay";   public bool show; public string textTopLeft; public string textBottomRight; }
        [Serializable] class FollowupPayload       { public string kind = "followup_survey"; public string url; }

        // ---------- RTC configuration ----------

        RTCConfiguration BuildRtcConfiguration()
        {
            var servers = new List<RTCIceServer>();
            foreach (var cfg in IceServers)
            {
                if (cfg == null)
                    continue;

                var urls = cfg.urls?.Where(u => !string.IsNullOrWhiteSpace(u)).ToArray();
                if (urls == null || urls.Length == 0)
                    continue;

                var server = new RTCIceServer { urls = urls };

                // Only set credentials for TURN servers; leaving them set for STUN can break some stacks.
                if (!string.IsNullOrEmpty(cfg.username) && !string.IsNullOrEmpty(cfg.credential))
                {
                    server.username   = cfg.username;
                    server.credential = cfg.credential;
                }

                servers.Add(server);
            }

            return new RTCConfiguration { iceServers = servers.ToArray() };
        }

        void ForwardMicAudio(float[] data, int channels, int sampleRate) =>
            _audioTrack?.SetData(data, channels, sampleRate);

        // ---------- Publishing ----------

        async Task StartPublishing()
        {
            var rtcConfig = BuildRtcConfiguration();
            foreach (var srv in rtcConfig.iceServers)
                Debug.Log($"[WebCamClient] iceServer urls=[{string.Join(", ", srv.urls)}] username={srv.username}");
            _publisher = new JanusVideoRoomPublisher(this, _publisherHandle, rtcConfig);
            _publisher.IceStateChanged        += s => Debug.Log("[Publisher] ICE: " + s);
            _publisher.ConnectionStateChanged += s => Debug.Log("[Publisher] PC: " + s);

            var sendStream = new MediaStream();
            _audioTrack = new AudioStreamTrack();
            if (microphoneSource != null)
                microphoneSource.OnAudioRead += ForwardMicAudio;
            else
                Debug.LogWarning("[WebCamClient] microphoneSource not assigned — publishing silent audio. Assign the JanusMicrophoneSource child in the Inspector.");
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

            if (logOutboundStats)
                StartCoroutine(LogOutboundStats());
        }

        [Header("WebRTC Stats")]
        [Tooltip("Periodically log the publisher's outbound RTP stats (and the receiver's loss/jitter " +
                 "report) to triage audio loops/crackling. Logs under [JanusStats] so it relays to chat.")]
        public bool logOutboundStats = false;
        [Tooltip("How often (seconds) to sample WebRTC stats.")]
        public float outboundStatsIntervalSeconds = 3f;

        System.Collections.IEnumerator LogOutboundStats()
        {
            // Track deltas between samples so we report per-interval loss, not cumulative totals.
            int lastAudioLost = 0;
            ulong lastAudioPackets = 0;

            while (_publisher != null && _publisher.PeerConnection != null)
            {
                yield return new WaitForSeconds(outboundStatsIntervalSeconds);

                var pc = _publisher?.PeerConnection;
                if (pc == null) yield break;

                var op = pc.GetStats();
                yield return op;
                if (op.IsError || op.Value == null) continue;

                using (var report = op.Value)
                {
                    foreach (var s in report.Stats.Values)
                    {
                        // Our send side, audio track.
                        if (s is RTCOutboundRTPStreamStats outAudio && outAudio.kind == "audio")
                        {
                            ulong dPackets = outAudio.packetsSent - lastAudioPackets;
                            lastAudioPackets = outAudio.packetsSent;
                            Debug.Log($"[JanusStats] out-audio: +{dPackets} pkts/{outboundStatsIntervalSeconds:0}s, " +
                                      $"retx={outAudio.retransmittedPacketsSent}, targetBitrate={outAudio.targetBitrate:0}");
                        }
                        // The REMOTE end's report back to us — what the receiver actually experienced.
                        else if (s is RTCRemoteInboundRtpStreamStats remote && remote.kind == "audio")
                        {
                            int dLost = remote.packetsLost - lastAudioLost;
                            lastAudioLost = remote.packetsLost;
                            Debug.Log($"[JanusStats] remote-audio: +{dLost} lost/{outboundStatsIntervalSeconds:0}s " +
                                      $"(total={remote.packetsLost}), fractionLost={remote.fractionLost:0.000}, " +
                                      $"jitter={remote.jitter * 1000:0.0}ms, rtt={remote.roundTripTime * 1000:0}ms");
                        }
                    }
                }
            }
        }

        // ---------- Subscribing ----------

        async void OnPublishersAdded(object sender, PublishersAddedEventArgs e)
        {
            try { await SubscribeToAvailable(e.Publishers); }
            catch (Exception ex) { Debug.LogError($"[Subscriber] subscribe failed: {ex.Message}"); }
        }

        async void OnPublisherLeft(object sender, PublisherLeftEventArgs e)
        {
            long leftId = e.Id;

            await _subLifecycleLock.WaitAsync();
            try
            {
                // The handler is wired to both PublisherLeft and PublisherUnpublished, so a single
                // disconnect fires it more than once. The _knownPublishers guard is what stops the
                // duplicate from re-tearing-down a freshly built subscriber: the first call removes
                // the id, any later call for the same id finds it absent and returns.
                if (!_knownPublishers.ContainsKey(leftId)) return;

                Debug.Log($"[VideoRoom] publisher {leftId} left/unpublished — tearing down subscriber so the next subscribe gets a clean slate");
                _knownPublishers.Remove(leftId);
                _publisherStreams.Remove(leftId);

                if (frame != null)
                {
                    frame.material.mainTexture = null;
                    frame.material.SetTexture("_BaseMap", null);
                }

                // Keeping the subscriber handle and renegotiating via 'update' was tried and leaves
                // Unity WebRTC's transceiver/SDP state ambiguous enough that a subsequent
                // resubscribe doesn't deliver tracks (e.g. after the web peer reloads the page).
                // Capture the locals and null the fields synchronously so a resubscribe that runs
                // after the await below builds a fresh handle instead of racing with this detach.
                var sub = _subscriber;
                var handle = _subscriberHandle;
                _subscriber = null;
                _subscriberHandle = null;

                if (sub != null)
                {
                    sub.Dispose();
                    if (handle != null)
                    {
                        try { await handle.DetachAsync(); } catch { }
                    }
                }
            }
            catch (Exception ex) { Debug.LogWarning($"[Subscriber] teardown error: {ex.Message}"); }
            finally { _subLifecycleLock.Release(); }
        }

        async Task SubscribeToAvailable(List<RemotePublisher> publishers)
        {
            await _subLifecycleLock.WaitAsync();
            try
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
                    await _subscriber.SubscribeAsync(Room, newStreams, _publisherHandle.PrivateId);
                }
                else if (_subscriber.IsJoined)
                {
                    await _subscriber.UpdateAsync(newStreams, null);
                }
                else
                {
                    // Subscriber exists but is mid-restore (SDP exchange not complete); call SubscribeAsync
                    // so it does a fresh join rather than an update on an un-joined handle.
                    await _subscriber.SubscribeAsync(Room, newStreams, _publisherHandle.PrivateId);
                }
            }
            finally { _subLifecycleLock.Release(); }
        }

        void OnRemoteTrack(object sender, RemoteTrackEventArgs e)
        {
            Debug.Log($"[WebCam] RemoteTrack: type={(e.Track != null ? e.Track.GetType().Name : "null")} mid={e.Mid} feed={e.StreamInfo?.FeedId}");
            if (e.Track is AudioStreamTrack audio)
            {
                if (output == null)
                {
                    Debug.LogWarning("[WebCam] AudioTrack received but no output AudioSource assigned — assign it in the Inspector.");
                }
                else
                {
                    output.loop = true;
                    output.SetTrack(audio);
                    output.Play();
                    Debug.Log($"[WebCam] AudioTrack playing: volume={output.volume} mute={output.mute} spatialBlend={output.spatialBlend}");
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
