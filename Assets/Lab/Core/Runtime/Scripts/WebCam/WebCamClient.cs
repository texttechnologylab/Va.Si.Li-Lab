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
        [Tooltip("How long (seconds) to wait for an ack before marking a batch failed. Must exceed " +
                 "the retransmission window (last retry at 10s) or batches get reported failed while " +
                 "a retry is still outstanding; values below that floor are raised automatically.")]
        public float ackTimeoutSeconds = 20f;
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
        readonly AckTracker _ackTracker = new AckTracker();

        // ---------- Answer-option delivery (see AnswerOptionsPayload for the wire format) ----------

        /// <summary>Identifies this process lifetime; lets the receiver reset its de-duplication
        /// state when we restart instead of discarding everything we send as "stale".</summary>
        readonly string _answerEpoch = Guid.NewGuid().ToString("N").Substring(0, 8);

        /// <summary>Content revision. Only advances for a genuinely new batch — retries, the
        /// heartbeat and push-on-join all re-deliver the current revision.</summary>
        int _answerSeq;

        /// <summary>The batch the participant should currently be seeing. Retries, the heartbeat
        /// and push-on-join re-send THIS, reusing its revision and msgId rather than minting new
        /// ones, so the ack pill counts questions asked rather than packets sent.</summary>
        AnswerBatch _currentBatch;

        /// <summary>Receiver side: highest revision rendered, and the sender epoch it belongs to.</summary>
        int _lastAnswerSeq = -1;
        string _lastAnswerEpoch;

        // Retransmission schedule for an unacknowledged batch, in seconds from first send, plus
        // a slow re-announce so state converges even if every retry was lost. Mirrors
        // RETRY_DELAYS_MS / HEARTBEAT_MS in the web frontend.
        static readonly float[] AnswerRetryDelaysSeconds = { 2f, 5f, 10f };
        const float AnswerHeartbeatSeconds = 15f;
        /// <summary>Ack budget floor: the last retry fires at 10 s, so anything shorter would
        /// report failure while a retransmission is still in flight. See Update().</summary>
        const float AnswerAckMinBudgetSeconds = 20f;
        float _answerHeartbeatDue;

        sealed class AnswerBatch
        {
            public int Seq;
            public string[] Data;
            public string MsgId;
            public bool Tracked;
            public bool Acked;
            public string SuppressedBy;   // null when the batch shows concrete options
            public string[] Lines;
            public float FirstSentAt;     // Time.realtimeSinceStartup, -1 until first transmission
            public int RetriesDone;
        }
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
            //
            // Floored to the retransmission window rather than trusting the serialized value:
            // ackTimeoutSeconds is a public field, so scenes and prefabs saved before the retry
            // schedule existed still carry the old 5 s and would report a batch failed while its
            // 10 s retry was still outstanding. Raising the default alone would not reach them.
            _ackTracker.Tick((long)(Mathf.Max(ackTimeoutSeconds, AnswerAckMinBudgetSeconds) * 1000f));
            // Drives answer-option retransmission and the slow re-announce, same pattern.
            TickAnswerOptionDelivery();
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
                CancelInvoke(nameof(BroadcastRecordingState));
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
            // A restore tears down the PeerConnection and renegotiates, so we are NOT joined to
            // the room during that window even though the old value said we were. Clearing it
            // here (rather than only at the start of the join sequence) closes the gap between
            // the handle being restored and Ready firing again, during which sends would
            // otherwise be attempted against a dead channel and silently dropped.
            _textHandle.Restored += (_, __) =>
            {
                Debug.Log("[TextRoom] Handle restored — chat marked not-ready until rejoined");
                _chatReady = false;
            };
            _chat = new JanusTextRoomClient(this, _textHandle, BuildRtcConfiguration());
            _chat.ConnectionStateChanged += s => Debug.Log("[TextRoom] PC: " + s);
            _chat.MessageReceived        += OnChatMessage;
            _chat.Announcement           += (_, text) => AppendChatLine($"<i>{text}</i>");
            _chat.ParticipantJoined      += (_, p) =>
            {
                AppendChatLine($"<i>{p.Display ?? p.Username} joined</i>");
                // Push-on-join: re-deliver the current batch so a participant who joined or
                // reloaded sees the current options. This re-sends the SAME revision and msgId
                // rather than composing a new batch — the participant's revision guard makes it
                // a no-op if they already have it, and the ack pill doesn't gain a duplicate
                // entry for what is the same question.
                DeliverCurrentBatch("participant-joined");
                // Catch a (re)joining participant up on the current recording state so their REC
                // indicator is correct even if they reloaded mid-recording.
                BroadcastRecordingState();
            };
            _chat.ParticipantLeft        += (_, p) => AppendChatLine($"<i>{p.Display ?? p.Username} left</i>");
            _chat.Ready += (_, __) => _ = RunChatJoinSequenceAsync();

            Debug.Log("[TextRoom] Starting SDP setup (SetupAsync)");
            await _chat.SetupAsync();
            Debug.Log("[TextRoom] SetupAsync complete — waiting for data channel to open");
            WireChatInput();
        }

        /// <summary>
        /// Join the textroom once the data channel is open, retrying the REQUESTS rather than
        /// the connection.
        ///
        /// This runs after Ready, so the PeerConnection, DTLS and SCTP association are already
        /// established — a lost create/join response therefore only costs one round trip to
        /// recover, not a full renegotiation. The previous version bounded the whole sequence
        /// with a single 15 s CancellationTokenSource and, on expiry, logged an error and gave
        /// up: chat stayed dead for the rest of the session with no path back except a full
        /// Janus session reconnect.
        ///
        /// Each attempt uses a FRESH username, because a previous attempt may have succeeded
        /// server-side with only its response lost — rejoining under the same name would then be
        /// rejected as already taken.
        /// </summary>
        async Task RunChatJoinSequenceAsync()
        {
            const int maxAttempts = 5;
            const int perAttemptSeconds = 8;

            // A (re)join means the previous session state is gone: stop reporting ready until we
            // actually are, so nothing writes to a channel that isn't joined yet.
            _chatReady = false;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (_chat == null) return;
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(perAttemptSeconds));
                var username = string.IsNullOrEmpty(chatUsername)
                    ? DisplayName + UnityEngine.Random.Range(1000, 9999)
                    : chatUsername + "-" + UnityEngine.Random.Range(1000, 9999);
                try
                {
                    // Join first. The room usually already exists (the web interviewer creates
                    // it), so this is one round trip in the common case; creating up front would
                    // always cost two. Fall back to create-then-join only if the join fails.
                    try
                    {
                        await _chat.JoinAsync(Room, username, DisplayName, cts.Token);
                    }
                    catch (JanusException ex)
                    {
                        Debug.LogWarning($"[TextRoom] Join failed ({ex.Message}) — creating room and retrying");
                        await _chat.EnsureRoomAsync(Room, "VaSiLi room " + Room, cts.Token);
                        await _chat.JoinAsync(Room, username, DisplayName, cts.Token);
                    }

                    Debug.Log($"[TextRoom] Joined as {username} — chat is ready (attempt {attempt})");
                    _chatReady = true;
                    AppendChatLine("<i>Chat ready</i>");

                    // Re-deliver whatever the participant should currently be seeing. Covers both
                    // a batch composed while the channel was down and a peer who has been in the
                    // room the whole time while we were the ones reconnecting.
                    DeliverCurrentBatch("chat-ready");

                    // Re-announce recording state whenever our textroom channel (re)connects, and
                    // (re)start the heartbeat if we're already recording, so participants converge
                    // on the correct REC indicator after a reconnect.
                    BroadcastRecordingState();
                    UpdateRecordingHeartbeat(_publisherHandle != null && (_publisherHandle.IsRecording ?? false));
                    if (enableRemoteLogs)
                        StartRemoteLogs();
                    return;
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"[TextRoom] Join attempt {attempt}/{maxAttempts} timed out after {perAttemptSeconds}s");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[TextRoom] Join attempt {attempt}/{maxAttempts} failed: {ex.Message}");
                }

                if (attempt < maxAttempts)
                {
                    var backoffMs = Math.Min(500 * (int)Math.Pow(2, attempt - 1), 5000);
                    try { await Task.Delay(backoffMs); } catch { }
                }
            }

            Debug.LogError($"[TextRoom] Join sequence failed after {maxAttempts} attempts — chat unavailable. " +
                           "The data channel is open but the textroom plugin is not responding.");
            AppendChatLine("<i>Chat konnte nicht verbunden werden.</i>");
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
                            HandleAnswerOptionsPayload(JsonConvert.DeserializeObject<AnswerOptionsPayload>(e.Text));
                            return;

                        case "ack":
                            // Interviewer side: correlate the interviewee's ack to a tracked batch.
                            var ack = JsonConvert.DeserializeObject<AckPayload>(e.Text);
                            if (ack != null) HandleAnswerAck(ack.ackOf);
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

        /// <summary>
        /// One rendered block in the chat. Most are plain text (<see cref="AckId"/> null).
        /// An answer-option notice carries the batch's msgId so its status (and therefore
        /// the text it renders) can be flipped in place when the interviewee's ack arrives
        /// or the batch times out — the Unity equivalent of the website's per-msgId DOM node.
        /// </summary>
        sealed class ChatBlock
        {
            public string AckId;             // null for plain chat lines
            public string[] OptionLines;     // the echoed option lines (answer-option notices only)
            public AckStatus Status;         // current ack state (answer-option notices only)
        }

        readonly List<ChatBlock> _chatBlocks = new List<ChatBlock>();

        void AppendChatLine(string line)
        {
            _chatBlocks.Add(new ChatBlock { OptionLines = new[] { line } });
            RenderChat();
        }

        /// <summary>
        /// Echo the answer options the interviewer just sent into their OWN chat as a notice
        /// block (local-only — never transmitted). Starts in the Pending state and is flipped
        /// to Acked/Failed by <see cref="UpdateAnswerOptionsNotice"/>, keyed by msgId. Mirrors
        /// the website's appendLocalAnswerOptions so the interviewer keeps a running record of
        /// what was shown and whether the participant confirmed it.
        /// </summary>
        void AppendAnswerOptionsNotice(string msgId, string[] optionLines)
        {
            if (string.IsNullOrEmpty(msgId) || optionLines == null || optionLines.Length == 0) return;
            _chatBlocks.Add(new ChatBlock { AckId = msgId, OptionLines = optionLines, Status = AckStatus.Pending });
            RenderChat();
        }

        /// <summary>Flip a previously-appended answer-option notice to reflect the ack status.
        /// No-op if there is no local notice for this msgId (e.g. a silent push-on-join re-send).</summary>
        void UpdateAnswerOptionsNotice(string msgId, AckStatus status)
        {
            if (string.IsNullOrEmpty(msgId)) return;
            bool changed = false;
            foreach (var b in _chatBlocks)
            {
                if (b.AckId == msgId && b.Status != status) { b.Status = status; changed = true; }
            }
            if (changed) RenderChat();
        }

        void RenderChat()
        {
            if (chatScrollRect == null || chatScrollRect.content == null) return;

            // Handle both cases: TMP_Text on the Content object itself or as a child.
            if (_chatText == null)
                _chatText = chatScrollRect.content.GetComponentInChildren<TMP_Text>();
            if (_chatText == null) return;

            // Trim the oldest blocks so we never render more than chatMaxLines lines total.
            TrimChatBlocks();

            var sb = new System.Text.StringBuilder();
            foreach (var b in _chatBlocks)
            {
                if (b.AckId != null) sb.Append(RenderAnswerOptionsNotice(b));
                else foreach (var l in b.OptionLines) sb.Append(l).Append('\n');
            }
            _chatText.text = sb.ToString();

            // Force layout rebuild so the ContentSizeFitter updates the height immediately.
            LayoutRebuilder.ForceRebuildLayoutImmediate(chatScrollRect.content);

            // Stay grounded: move scroll to the bottom (0 is bottom in a standard vertical ScrollRect).
            chatScrollRect.verticalNormalizedPosition = 0f;
        }

        // Status → header text/colour for the answer-option notice, mirroring the website's
        // ANSWER_OPT_STATES (pending → "werden gesendet", acked → "angezeigt", failed → "nicht
        // bestätigt"). TMP rich-text replaces the website's CSS classes.
        static string RenderAnswerOptionsNotice(ChatBlock b)
        {
            // Plain-ASCII status markers (no FontAwesome/emoji) so they render in any TMP font
            // atlas — TMP shows a tofu box for glyphs the font lacks, unlike the website's icon font.
            string header, color;
            switch (b.Status)
            {
                case AckStatus.Acked:  header = "[OK] Antwortoptionen angezeigt";    color = "#2E7D32"; break;
                case AckStatus.Failed: header = "[!] Anzeige nicht bestätigt";       color = "#E65100"; break;
                default:               header = "... Antwortoptionen werden gesendet"; color = "#9E9E9E"; break;
            }
            var sb = new System.Text.StringBuilder();
            sb.Append("<color=").Append(color).Append("><b>").Append(header).Append("</b></color>\n");
            foreach (var line in b.OptionLines)
                sb.Append("<color=#777777>  - ").Append(line).Append("</color>\n");
            return sb.ToString();
        }

        void TrimChatBlocks()
        {
            static int LineCount(ChatBlock b) => b.AckId != null ? b.OptionLines.Length + 1 : b.OptionLines.Length;
            int total = 0;
            foreach (var b in _chatBlocks) total += LineCount(b);
            while (total > chatMaxLines && _chatBlocks.Count > 0)
            {
                total -= LineCount(_chatBlocks[0]);
                _chatBlocks.RemoveAt(0);
            }
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

            // Relay the recording state to the participant over the textroom data channel so
            // their REC indicator lights up — the videoroom plugin doesn't notify other peers.
            // Driven from here (rather than only ToggleRecording) so a state change reported by
            // Janus's own 'record' event broadcasts too. Also (re)starts/stops the heartbeat.
            BroadcastRecordingState();
            UpdateRecordingHeartbeat(on);
        }

        // ---------- Recording-state broadcast (Interviewer → participant) ----------

        const float RecordingHeartbeatSeconds = 10f;

        /// <summary>
        /// Best-effort, idempotent broadcast of the current recording state to the room. The
        /// participant matches on kind=="recording" and just sets its indicator each time, so
        /// re-sending the same value is expected. No-op until the chat data channel is ready.
        /// Mirrors the website's broadcastRecordingState.
        /// </summary>
        void BroadcastRecordingState()
        {
            if (_chat == null || !_chatReady) return;
            bool active = _publisherHandle != null && (_publisherHandle.IsRecording ?? false);
            var payload = JsonConvert.SerializeObject(new RecordingPayload { active = active });
            _ = SendStructuredAsync(payload);
        }

        // The data channel is lossy, so a single send can be missed. While recording is on we
        // re-announce every RecordingHeartbeatSeconds so the participant's indicator converges;
        // the heartbeat is stopped as soon as recording stops.
        void UpdateRecordingHeartbeat(bool on)
        {
            CancelInvoke(nameof(BroadcastRecordingState));
            if (on)
                InvokeRepeating(nameof(BroadcastRecordingState), RecordingHeartbeatSeconds, RecordingHeartbeatSeconds);
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

        /// <summary>
        /// Compose and start delivering a new answer-option batch. Advances the content
        /// revision, so this is for genuinely NEW options only — re-delivering the current ones
        /// (push-on-join, retry, heartbeat) goes through <see cref="DeliverCurrentBatch"/>.
        ///
        /// There is no "silent" variant any more: the local chat notice is appended on a batch's
        /// FIRST transmission only, so re-delivery can never duplicate it. That used to need an
        /// explicit silentLocal flag at every re-send site.
        /// </summary>
        public void SendAnswerOptions(string[] options)
        {
            if (options == null || options.Length == 0) return;

            // Apply the SAME filter the interviewee uses (AnswerOptionFilter), so the echo shows
            // exactly what will appear on their screen. When the filter suppresses everything we
            // still send — their panel has to switch to the free-answer state — but we label the
            // echo as suppressed and name the rule, so "deliberately hidden" can never be
            // mistaken for "never arrived". That ambiguity is what made the real transport
            // problem so hard to diagnose on the web side.
            var displayable = AnswerOptionFilter.Filter(options);
            bool hasConcrete = displayable != null && displayable.Count > 0;
            string suppressedBy = hasConcrete ? null : AnswerOptionFilter.DescribeSuppression(options);

            _currentBatch = new AnswerBatch
            {
                Seq          = ++_answerSeq,
                Data         = options,
                SuppressedBy = suppressedBy,
                Lines        = hasConcrete
                                 ? AnswerOptionLines(displayable)
                                 : new[] { "Freie Antwort — " + suppressedBy },
                FirstSentAt  = -1f,
            };

            DeliverCurrentBatch("initial");
        }

        /// <summary>
        /// Transmit the current batch. Safe to call repeatedly: the receiver drops any revision
        /// it has already rendered, so retries, the heartbeat and push-on-join are all the same
        /// operation. Returns false when the channel isn't up yet — the batch stays current and
        /// the heartbeat (or the next join) will deliver it.
        /// </summary>
        bool DeliverCurrentBatch(string why)
        {
            var batch = _currentBatch;
            if (batch == null) return false;
            if (_chat == null || !_chatReady)
            {
                Debug.Log($"[AnswerOptions] channel not ready — deferring seq={batch.Seq} ({why})");
                return false;
            }

            // Register with the ack tracker on the FIRST real transmission rather than when the
            // batch was composed. Otherwise the ack timeout would burn down while the data
            // channel was still connecting and report a failure for something never sent.
            if (!batch.Tracked)
            {
                batch.Tracked = true;
                batch.FirstSentAt = Time.realtimeSinceStartup;
                var meta = new Dictionary<string, object> { { "count", batch.Lines?.Length ?? 0 } };
                batch.MsgId = _ackTracker.Track(meta);
                AppendAnswerOptionsNotice(batch.MsgId, batch.Lines);
            }

            var payload = JsonConvert.SerializeObject(new AnswerOptionsPayload
            {
                msgId = batch.MsgId,
                epoch = _answerEpoch,
                seq   = batch.Seq,
                data  = batch.Data,
            });
            Debug.Log($"[AnswerOptions] sending seq={batch.Seq} msgId={batch.MsgId} reason={why} " +
                      $"suppressedBy={batch.SuppressedBy ?? "(none)"}");
            _ = SendStructuredAsync(payload);
            return true;
        }

        /// <summary>
        /// Drives retransmission and the slow re-announce. Called once per frame — the
        /// Unity-idiomatic replacement for setTimeout, same pattern as AckTracker.Tick.
        ///
        /// Answer options are a STATE ("which options apply right now"), not an event, so an
        /// unacknowledged batch is retried on a short schedule and the current one is
        /// re-announced slowly forever. Duplicates cost one small message and are dropped by
        /// the receiver's revision guard.
        /// </summary>
        void TickAnswerOptionDelivery()
        {
            var batch = _currentBatch;
            if (batch == null) return;
            float now = Time.realtimeSinceStartup;

            if (batch.Tracked && !batch.Acked && batch.RetriesDone < AnswerRetryDelaysSeconds.Length)
            {
                float due = batch.FirstSentAt + AnswerRetryDelaysSeconds[batch.RetriesDone];
                if (now >= due)
                {
                    batch.RetriesDone++;
                    Debug.LogWarning($"[AnswerOptions] no ack for seq={batch.Seq} — retransmitting " +
                                     $"(attempt {batch.RetriesDone}/{AnswerRetryDelaysSeconds.Length})");
                    DeliverCurrentBatch("retry");
                }
            }

            if (now >= _answerHeartbeatDue)
            {
                _answerHeartbeatDue = now + AnswerHeartbeatSeconds;
                DeliverCurrentBatch("heartbeat");
            }
        }

        /// <summary>Build the lines echoed into the interviewer's chat for a displayed batch.
        /// Collapses an 11-point 0–10 scale into one readable line; otherwise one line per option
        /// (descriptive second line stripped, like the rendered rows). Mirrors answerOptionLines on the web.</summary>
        static string[] AnswerOptionLines(List<string> displayable)
        {
            if (AnswerOptionFilter.IsNumericScale(displayable) && displayable.Count == 11)
            {
                // Show the two endpoint anchor labels (e.g. "ganz unzufrieden … ganz zufrieden"),
                // falling back to the bare numbers, like the website's answerOptionLines.
                var (lowNum, lowLabel) = ParseScaleEntry(displayable[0]);
                var (highNum, highLabel) = ParseScaleEntry(displayable[10]);
                var ends = (lowLabel.Length > 0 || highLabel.Length > 0)
                    ? $" ({(lowLabel.Length > 0 ? lowLabel : lowNum)} … {(highLabel.Length > 0 ? highLabel : highNum)})"
                    : "";
                return new[] { "Skala 0–10" + ends };
            }
            return displayable
                .Select(e => AnswerOptionFilter.FilterText(e).Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();
        }

        // Split a scale entry like "0 - ganz und gar unzufrieden" into its leading number and the
        // descriptive anchor text. Falls back gracefully if no separator is present. Ports the
        // website's parseScaleEntry.
        static (string num, string label) ParseScaleEntry(string text)
        {
            var firstLine = AnswerOptionFilter.FilterText(text).Trim();
            var m = System.Text.RegularExpressions.Regex.Match(firstLine, @"^(\d+)\s*[-–—:.]?\s*(.*)$");
            return m.Success ? (m.Groups[1].Value, m.Groups[2].Value.Trim()) : (firstLine, "");
        }

        /// <summary>
        /// Render an incoming answer-option batch, dropping duplicates.
        ///
        /// Duplicates are expected and cheap by design: the sender retransmits an
        /// unacknowledged batch and re-announces the current one on a slow heartbeat, so a
        /// delivery lost while our channel was down still converges. Re-rendering each of those
        /// would rebuild the TextMeshPro meshes for nothing.
        ///
        /// We ack duplicates too — otherwise a lost ACK (rather than a lost batch) would leave
        /// the sender's pill stuck on "not confirmed" for options plainly on screen, and would
        /// keep its retry schedule running.
        /// </summary>
        void HandleAnswerOptionsPayload(AnswerOptionsPayload ao)
        {
            if (ao?.data == null) return;

            // A changed epoch means the sender restarted and its revision counter went back to
            // zero. Without resetting here, every batch it sends from now on would look stale.
            var epoch = string.IsNullOrEmpty(ao.epoch) ? null : ao.epoch;
            if (epoch != _lastAnswerEpoch)
            {
                Debug.Log($"[AnswerOptions] sender epoch changed {_lastAnswerEpoch ?? "(none)"} -> " +
                          $"{epoch ?? "(none)"} — resetting de-duplication");
                _lastAnswerEpoch = epoch;
                _lastAnswerSeq = -1;
            }

            // seq < 0 means the sender doesn't stamp revisions (older build): render every time,
            // which is the pre-existing behaviour and still correct, just not de-duplicated.
            if (ao.seq >= 0 && ao.seq <= _lastAnswerSeq)
            {
                Debug.Log($"[AnswerOptions] duplicate seq={ao.seq} — already rendered, acking only");
                if (!string.IsNullOrEmpty(ao.msgId)) _ = SendAckAsync(ao.msgId);
                return;
            }
            if (ao.seq >= 0) _lastAnswerSeq = ao.seq;

            Debug.Log($"[AnswerOptions] rendering seq={ao.seq} epoch={epoch ?? "(none)"} msgId={ao.msgId}");
            if (answerOptionsSource != null)
                answerOptionsSource.DisplayOptions(ao.data);

            // Acknowledge after rendering so the sender's pill confirms actual display.
            if (!string.IsNullOrEmpty(ao.msgId)) _ = SendAckAsync(ao.msgId);
        }

        /// <summary>Interviewer side: an interviewee confirmed receipt, so stop retransmitting.</summary>
        void HandleAnswerAck(string ackOf)
        {
            if (string.IsNullOrEmpty(ackOf)) return;
            if (_currentBatch != null && _currentBatch.MsgId == ackOf)
                _currentBatch.Acked = true;
            _ackTracker.Ack(ackOf);
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
            // Keep the per-batch chat notice in sync with the real ack status, so it flips
            // from "wird gesendet" to "angezeigt" only once the participant confirms (or to
            // "nicht bestätigt" on timeout). Batches without a notice are silently skipped.
            foreach (var e in entries) UpdateAnswerOptionsNotice(e.Id, e.Status);

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

        /// <summary>
        /// Answer-option batch on the wire. Field-for-field compatible with the web
        /// frontend's payload (js/answer-options.js) — change both or neither.
        ///
        /// <c>epoch</c> identifies the SENDER'S process/page lifetime and <c>seq</c> is a
        /// CONTENT revision, not a packet counter. Retries, the heartbeat and push-on-join
        /// re-sends all carry the revision of the batch they are re-delivering, so the
        /// receiver can drop a duplicate without re-rendering while a first delivery still
        /// gets through. Without the epoch, a sender restart would reset seq to zero and the
        /// receiver would discard every later batch as "stale".
        /// </summary>
        [Serializable] class AnswerOptionsPayload
        {
            public string kind = "answer_options";
            public string msgId;
            public string epoch;
            public int seq = -1;   // -1 = absent (older sender); receiver then skips de-duplication
            public string[] data;
        }
        [Serializable] class AckPayload           { public string kind = "ack"; public string ackOf; public string status = "received"; }
        [Serializable] class GazeOverlayPayload   { public string kind = "gaze_overlay";   public bool show; public string textTopLeft; public string textBottomRight; }
        [Serializable] class FollowupPayload       { public string kind = "followup_survey"; public string url; }
        [Serializable] class RecordingPayload      { public string kind = "recording"; public bool active; }

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
