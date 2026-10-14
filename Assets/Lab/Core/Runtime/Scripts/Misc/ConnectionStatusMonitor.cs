using System.Collections;
using System.Reflection;
using TMPro;
using Ubiq.Rooms;
using Ubiq.Voip;
using Ubiq.XR.Notifications;
using UnityEngine;
using VaSiLi.Networking;
using static Ubiq.Rooms.RoomClient;

/// <summary>
/// Monitors and displays connection status for RoomClient, API, and VoIP in a compact single-line format.
/// </summary>
public class ConnectionStatusMonitor : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField]
    [Tooltip("The TextMeshPro text component to display status. Falls back to legacy Text if not set.")]
    private TMP_Text statusText;

    [SerializeField]
    [Tooltip("Legacy UI Text component (used if TMP_Text is not assigned).")]
    private UnityEngine.UI.Text legacyText;

    [Header("Settings")]
    [SerializeField]
    [Tooltip("How often to check the status (in seconds).")]
    private float updateInterval = 5f;

    private static string apiUrl => VasiliEndpoints.Current.api + "/logging/status";

    [Header("Status Symbols")]
    [SerializeField] private string symbolOk = "✔️";
    [SerializeField] private string symbolError = "❌";
    [SerializeField] private string symbolPending = "…";
    [SerializeField] private string symbolWarning = "⚠️";

    public enum ConnectionStatus { Ok, Error, Pending, Warning }

    [Header("VoIP Test Settings")]
    [SerializeField]
    [Tooltip("Fallback STUN server if room doesn't provide ice-servers.")]
    private string fallbackStunServer = "stun.l.google.com";
    [SerializeField]
    private int fallbackStunPort = 19302;

    [SerializeField]
    private RoomClient roomClient;
    private VoipPeerConnectionManager voipManager;

    private bool apiOnline = false;
    private bool isCheckingApi = false;

    // VoIP connectivity test
    private bool voipReachable = false;
    private bool isCheckingVoip = false;

    // RoomClient heartbeat via reflection
    private FieldInfo iceServersField;
    private TimeoutNotification timeoutNotification = null;

    private void Start()
    {
        // Find references
        voipManager = FindObjectOfType<VoipPeerConnectionManager>();

        if (statusText == null && legacyText == null)
        {
            statusText = GetComponent<TMP_Text>();
            if (statusText == null)
            {
                legacyText = GetComponent<UnityEngine.UI.Text>();
            }
        }

        // Get reflection access to VoipPeerConnectionManager's iceServers field
        if (voipManager != null)
        {
            iceServersField = typeof(VoipPeerConnectionManager).GetField("iceServers", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // Subscribe to timeout notifications
        PlayerNotifications.OnNotification += OnNotification;

        // Start periodic updates
        StartCoroutine(UpdateStatusPeriodically());
    }

    private void OnDestroy()
    {
        PlayerNotifications.OnNotification -= OnNotification;
    }

    private void OnNotification(Notification notification)
    {
        // Check if it's a timeout notification from RoomClient
        if (notification is RoomClient.TimeoutNotification)
        {
            timeoutNotification = notification as RoomClient.TimeoutNotification;
        }
    }

    private IEnumerator UpdateStatusPeriodically()
    {
        while (true)
        {
            // Check API status
            if (!isCheckingApi)
            {
                CheckApiStatus();
            }

            // Check VoIP connectivity
            if (!isCheckingVoip)
            {
                CheckVoipConnectivity();
            }

            // Update display
            UpdateDisplay();

            yield return new WaitForSeconds(updateInterval);
        }
    }

    private async void CheckApiStatus()
    {
        isCheckingApi = true;

        try
        {
            var response = await JsonRequest.GetRequest(apiUrl);
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                apiOnline = content.Contains("online");
            }
            else
            {
                apiOnline = false;
            }
        }
        catch
        {
            apiOnline = false;
        }

        isCheckingApi = false;
        UpdateDisplay();
    }

    /// <summary>
    /// Tests VoIP connectivity by attempting a connection to a STUN/TURN server.
    /// Uses ICE servers from the room if available, otherwise falls back to Google STUN.
    /// Supports TURN servers with username/password authentication.
    /// This helps detect firewall issues that would block WebRTC.
    /// </summary>
    private async void CheckVoipConnectivity()
    {
        isCheckingVoip = true;

        // Get ICE server from room, or use fallback
        string serverUri = $"stun:{fallbackStunServer}:{fallbackStunPort}";
        string username = null;
        string password = null;

        if (iceServersField != null && voipManager != null)
        {
            try
            {
                var iceServersCollection = iceServersField.GetValue(voipManager);
                if (iceServersCollection != null)
                {
                    // Get the servers list via reflection (it's a private nested class)
                    var serversField = iceServersCollection.GetType().GetField("servers");
                    if (serversField != null)
                    {
                        var servers = serversField.GetValue(iceServersCollection) as System.Collections.IList;
                        if (servers != null && servers.Count > 0)
                        {
                            // Get first server (could be STUN or TURN)
                            var server = servers[0];
                            var uriField = server.GetType().GetField("uri");
                            var usernameField = server.GetType().GetField("username");
                            var passwordField = server.GetType().GetField("password");
                            
                            if (uriField != null)
                            {
                                string uri = uriField.GetValue(server) as string;
                                if (!string.IsNullOrEmpty(uri))
                                {
                                    serverUri = uri;
                                    
                                    // Get credentials for TURN
                                    if (usernameField != null)
                                        username = usernameField.GetValue(server) as string;
                                    if (passwordField != null)
                                        password = passwordField.GetValue(server) as string;
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ConnectionStatusMonitor] Failed to get ICE servers: {e.Message}");
            }
        }

        try
        {
            voipReachable = await IceServerConnectivityTester.TestIceServerAsync(serverUri, username, password);
        }
        catch
        {
            voipReachable = false;
        }

        isCheckingVoip = false;
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        string status = BuildStatusString();
        SetText(status);
    }

    private string BuildStatusString()
    {
        // Room status
        string roomStatus = GetConnectionStatusSymbol(GetRoomStatus());

        // API status
        string apiStatus = isCheckingApi ? symbolPending : (apiOnline ? symbolOk : symbolError);

        // VoIP status
        string voipStatus = GetVoipStatus();

        // Compact format: "R:✓ A:✓ V:✓" or "Room:✓|API:✓|VoIP:✓"
        return $"Room:{roomStatus} API:{apiStatus} VoIP:{voipStatus}";
    }

    public ConnectionStatus GetRoomStatus()
    {
        if (roomClient == null)
            return ConnectionStatus.Error;

        // Check if there's an active timeout notification (connection lost)
        if (timeoutNotification != null && !timeoutNotification.Deleted)
            return ConnectionStatus.Warning;
        if (roomClient.JoinedRoom)
            return ConnectionStatus.Ok;

        return ConnectionStatus.Ok;
    }

    private string GetConnectionStatusSymbol(ConnectionStatus status)
    {
        switch (status)
        {
            case ConnectionStatus.Ok:
                return symbolOk;
            case ConnectionStatus.Error:
                return symbolError;
            case ConnectionStatus.Pending:
                return symbolPending;
            case ConnectionStatus.Warning:
                return symbolWarning;
            default:
                return symbolError;
        }
    }

    private string GetVoipStatus()
    {
        if (voipManager == null)
            return symbolError;

        // First check: can we reach STUN servers? (firewall test)
        if (!voipReachable)
        {
            if (isCheckingVoip)
                return symbolPending;
            return symbolError;
        }

        // Check if we have any active peer connections and their state
        var peerConnections = voipManager.GetComponentsInChildren<VoipPeerConnection>();
        bool hasConnections = false;
        bool allConnected = true;
        bool anyFailed = false;

        foreach (var pc in peerConnections)
        {
            hasConnections = true;
            
            // Check ICE connection state
            switch (pc.iceConnectionState)
            {
                case VoipPeerConnection.IceConnectionState.connected:
                case VoipPeerConnection.IceConnectionState.completed:
                    // Good state
                    break;
                case VoipPeerConnection.IceConnectionState.failed:
                case VoipPeerConnection.IceConnectionState.closed:
                    anyFailed = true;
                    allConnected = false;
                    break;
                default:
                    // checking, new, disconnected - still connecting
                    allConnected = false;
                    break;
            }
        }

        if (anyFailed)
            return symbolError;

        // If in a room with peers and all connections are good
        if (hasConnections && allConnected)
            return symbolOk;

        // If in a room alone or still connecting, VoIP is ready but no peers
        if (roomClient != null && roomClient.JoinedRoom)
        {
            return hasConnections ? symbolPending : symbolOk;
        }

        // Not in room yet but STUN is reachable - ready to connect
        return voipReachable ? symbolPending : symbolError;
    }

    private void SetText(string text)
    {
        if (statusText != null)
        {
            statusText.text = text;
        }
        else if (legacyText != null)
        {
            legacyText.text = text;
        }
    }

    /// <summary>
    /// Force an immediate status update.
    /// </summary>
    public void RefreshNow()
    {
        if (!isCheckingApi)
        {
            CheckApiStatus();
        }
        if (!isCheckingVoip)
        {
            CheckVoipConnectivity();
        }
        UpdateDisplay();
    }
}
