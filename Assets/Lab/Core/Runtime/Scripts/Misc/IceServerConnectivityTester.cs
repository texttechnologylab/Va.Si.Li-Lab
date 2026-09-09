using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Tests connectivity to STUN and TURN servers to detect firewall issues that would block WebRTC.
/// </summary>
public static class IceServerConnectivityTester
{
    /// <summary>
    /// Tests connectivity to an ICE server (STUN or TURN).
    /// </summary>
    /// <param name="uri">The ICE server URI (e.g., "stun:host:port" or "turn:host:port")</param>
    /// <param name="username">Username for TURN authentication (optional)</param>
    /// <param name="password">Password for TURN authentication (optional)</param>
    /// <returns>True if the server is reachable</returns>
    public static async Task<bool> TestIceServerAsync(string uri, string username = null, string password = null)
    {
        if (!ParseIceServerUri(uri, out string host, out int port, out bool isTurn))
            return false;

        try
        {
            if (isTurn && !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                return await TestTurnServerAsync(host, port, username, password);
            }
            else
            {
                return await TestStunServerAsync(host, port);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[IceServerConnectivityTester] Failed to test {uri}: {e.Message}");
            return false;
        }
    }

    /// <summary>
    /// Tests connectivity to a STUN server by sending a binding request.
    /// </summary>
    private static async Task<bool> TestStunServerAsync(string host, int port)
    {
        using (var udpClient = new UdpClient())
        {
            udpClient.Connect(host, port);
            
            // Send a minimal STUN binding request
            byte[] stunRequest = BuildStunBindingRequest();
            await udpClient.SendAsync(stunRequest, stunRequest.Length);
            
            // Wait for response with timeout
            var receiveTask = udpClient.ReceiveAsync();
            var timeoutTask = Task.Delay(2000);
            
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            return completedTask == receiveTask && receiveTask.IsCompletedSuccessfully;
        }
    }

    /// <summary>
    /// Tests connectivity to a TURN server with authentication using the Allocate request.
    /// </summary>
    private static async Task<bool> TestTurnServerAsync(string host, int port, string username, string password)
    {
        using (var udpClient = new UdpClient())
        {
            udpClient.Connect(host, port);
            
            // Generate transaction ID for this session
            byte[] transactionId = new byte[12];
            new System.Random().NextBytes(transactionId);
            
            // Step 1: Send initial Allocate request (will be rejected with 401 + nonce + realm)
            byte[] allocateRequest = BuildTurnAllocateRequest(transactionId);
            await udpClient.SendAsync(allocateRequest, allocateRequest.Length);
            
            // Wait for 401 response with nonce and realm
            var receiveTask = udpClient.ReceiveAsync();
            var timeoutTask = Task.Delay(2000);
            
            var completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            if (completedTask != receiveTask || !receiveTask.IsCompletedSuccessfully)
                return false;
            
            var response = receiveTask.Result.Buffer;
            
            // Parse nonce and realm from 401 response
            if (!TryParseAuthChallenge(response, out string nonce, out string realm))
            {
                // If we got any valid STUN/TURN response, the server is reachable
                // Some servers might respond differently
                return response.Length >= 20 && response[0] == 0x01 && response[1] == 0x11;
            }
            
            // Step 2: Send authenticated Allocate request
            byte[] authRequest = BuildTurnAuthenticatedAllocateRequest(transactionId, username, password, nonce, realm);
            await udpClient.SendAsync(authRequest, authRequest.Length);
            
            // Wait for success or other response
            receiveTask = udpClient.ReceiveAsync();
            timeoutTask = Task.Delay(2000);
            
            completedTask = await Task.WhenAny(receiveTask, timeoutTask);
            if (completedTask != receiveTask || !receiveTask.IsCompletedSuccessfully)
                return false;
            
            response = receiveTask.Result.Buffer;
            
            // Check for success (0x0103 = Allocate Success Response)
            // Or at least a valid STUN response indicating the server is working
            return response.Length >= 20 && 
                   (response[0] == 0x01 && (response[1] == 0x03 || response[1] == 0x01 || response[1] == 0x11));
        }
    }

    /// <summary>
    /// Builds a STUN Binding Request message.
    /// </summary>
    private static byte[] BuildStunBindingRequest()
    {
        byte[] request = new byte[20];
        request[0] = 0x00; // Binding Request
        request[1] = 0x01;
        // Length = 0 (bytes 2-3)
        // Magic cookie
        request[4] = 0x21;
        request[5] = 0x12;
        request[6] = 0xA4;
        request[7] = 0x42;
        // Transaction ID (random)
        new System.Random().NextBytes(new Span<byte>(request, 8, 12));
        return request;
    }

    /// <summary>
    /// Builds a TURN Allocate Request message (initial, without auth).
    /// </summary>
    private static byte[] BuildTurnAllocateRequest(byte[] transactionId)
    {
        // TURN Allocate Request: type 0x0003
        // With REQUESTED-TRANSPORT attribute for UDP (17)
        byte[] request = new byte[28]; // 20 header + 8 attribute
        
        // Message Type: Allocate Request (0x0003)
        request[0] = 0x00;
        request[1] = 0x03;
        // Message Length: 8 (one attribute)
        request[2] = 0x00;
        request[3] = 0x08;
        // Magic Cookie
        request[4] = 0x21;
        request[5] = 0x12;
        request[6] = 0xA4;
        request[7] = 0x42;
        // Transaction ID
        Array.Copy(transactionId, 0, request, 8, 12);
        
        // REQUESTED-TRANSPORT attribute (0x0019)
        request[20] = 0x00;
        request[21] = 0x19;
        // Length: 4
        request[22] = 0x00;
        request[23] = 0x04;
        // Protocol: UDP (17)
        request[24] = 0x11;
        // Reserved (3 bytes padding)
        
        return request;
    }

    /// <summary>
    /// Builds an authenticated TURN Allocate Request using MESSAGE-INTEGRITY.
    /// </summary>
    private static byte[] BuildTurnAuthenticatedAllocateRequest(byte[] transactionId, string username, string password, string nonce, string realm)
    {
        var usernameBytes = Encoding.UTF8.GetBytes(username);
        var realmBytes = Encoding.UTF8.GetBytes(realm);
        var nonceBytes = Encoding.UTF8.GetBytes(nonce);
        
        // Pad to 4-byte boundary
        int usernamePadded = (usernameBytes.Length + 3) & ~3;
        int realmPadded = (realmBytes.Length + 3) & ~3;
        int noncePadded = (nonceBytes.Length + 3) & ~3;
        
        int attrLength = 8 + // REQUESTED-TRANSPORT
                         4 + usernamePadded + // USERNAME
                         4 + realmPadded + // REALM
                         4 + noncePadded + // NONCE
                         24; // MESSAGE-INTEGRITY (4 + 20)
        
        byte[] request = new byte[20 + attrLength];
        
        // Message Type: Allocate Request (0x0003)
        request[0] = 0x00;
        request[1] = 0x03;
        // Message Length
        request[2] = (byte)((attrLength >> 8) & 0xFF);
        request[3] = (byte)(attrLength & 0xFF);
        // Magic Cookie
        request[4] = 0x21;
        request[5] = 0x12;
        request[6] = 0xA4;
        request[7] = 0x42;
        // Transaction ID
        Array.Copy(transactionId, 0, request, 8, 12);
        
        int offset = 20;
        
        // REQUESTED-TRANSPORT (0x0019)
        request[offset++] = 0x00;
        request[offset++] = 0x19;
        request[offset++] = 0x00;
        request[offset++] = 0x04;
        request[offset++] = 0x11; // UDP
        offset += 3; // padding
        
        // USERNAME (0x0006)
        request[offset++] = 0x00;
        request[offset++] = 0x06;
        request[offset++] = (byte)((usernameBytes.Length >> 8) & 0xFF);
        request[offset++] = (byte)(usernameBytes.Length & 0xFF);
        Array.Copy(usernameBytes, 0, request, offset, usernameBytes.Length);
        offset += usernamePadded;
        
        // REALM (0x0014)
        request[offset++] = 0x00;
        request[offset++] = 0x14;
        request[offset++] = (byte)((realmBytes.Length >> 8) & 0xFF);
        request[offset++] = (byte)(realmBytes.Length & 0xFF);
        Array.Copy(realmBytes, 0, request, offset, realmBytes.Length);
        offset += realmPadded;
        
        // NONCE (0x0015)
        request[offset++] = 0x00;
        request[offset++] = 0x15;
        request[offset++] = (byte)((nonceBytes.Length >> 8) & 0xFF);
        request[offset++] = (byte)(nonceBytes.Length & 0xFF);
        Array.Copy(nonceBytes, 0, request, offset, nonceBytes.Length);
        offset += noncePadded;
        
        // MESSAGE-INTEGRITY (0x0008) - HMAC-SHA1
        // Key = MD5(username:realm:password)
        var keyString = $"{username}:{realm}:{password}";
        byte[] key;
        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            key = md5.ComputeHash(Encoding.UTF8.GetBytes(keyString));
        }
        
        // Update message length to exclude MESSAGE-INTEGRITY for HMAC calculation
        int lengthForHmac = offset - 20;
        request[2] = (byte)((lengthForHmac >> 8) & 0xFF);
        request[3] = (byte)(lengthForHmac & 0xFF);
        
        // Calculate HMAC-SHA1
        byte[] hmac;
        using (var hmacSha1 = new System.Security.Cryptography.HMACSHA1(key))
        {
            hmac = hmacSha1.ComputeHash(request, 0, offset);
        }
        
        // Now set final message length including MESSAGE-INTEGRITY
        request[2] = (byte)((attrLength >> 8) & 0xFF);
        request[3] = (byte)(attrLength & 0xFF);
        
        // MESSAGE-INTEGRITY attribute
        request[offset++] = 0x00;
        request[offset++] = 0x08;
        request[offset++] = 0x00;
        request[offset++] = 0x14; // 20 bytes
        Array.Copy(hmac, 0, request, offset, 20);
        
        return request;
    }

    /// <summary>
    /// Tries to parse nonce and realm from a TURN 401 error response.
    /// </summary>
    private static bool TryParseAuthChallenge(byte[] response, out string nonce, out string realm)
    {
        nonce = null;
        realm = null;
        
        if (response.Length < 20)
            return false;
        
        // Check if it's an error response (0x0113 for Allocate Error)
        if (response[0] != 0x01 || response[1] != 0x13)
            return false;
        
        int messageLength = (response[2] << 8) | response[3];
        int offset = 20;
        
        while (offset + 4 <= response.Length && offset < 20 + messageLength)
        {
            int attrType = (response[offset] << 8) | response[offset + 1];
            int attrLength = (response[offset + 2] << 8) | response[offset + 3];
            offset += 4;
            
            if (offset + attrLength > response.Length)
                break;
            
            if (attrType == 0x0015) // NONCE
            {
                nonce = Encoding.UTF8.GetString(response, offset, attrLength);
            }
            else if (attrType == 0x0014) // REALM
            {
                realm = Encoding.UTF8.GetString(response, offset, attrLength);
            }
            
            // Move to next attribute (padded to 4-byte boundary)
            offset += (attrLength + 3) & ~3;
        }
        
        return !string.IsNullOrEmpty(nonce) && !string.IsNullOrEmpty(realm);
    }

    /// <summary>
    /// Parses a STUN/TURN URI and extracts the host, port, and whether it's a TURN server.
    /// Supports formats like: stun:host:port, stun:host, turn:host:port, etc.
    /// </summary>
    public static bool ParseIceServerUri(string uri, out string host, out int port, out bool isTurn)
    {
        host = null;
        port = 3478; // Default STUN/TURN port
        isTurn = false;

        if (string.IsNullOrEmpty(uri))
            return false;

        // Detect if it's a TURN server
        isTurn = uri.StartsWith("turn", StringComparison.OrdinalIgnoreCase);

        // Remove scheme (stun:, stuns:, turn:, turns:)
        string hostPort = uri;
        if (uri.Contains("://"))
        {
            hostPort = uri.Substring(uri.IndexOf("://") + 3);
        }
        else if (uri.Contains(":"))
        {
            int schemeEnd = uri.IndexOf(':');
            // Check if this is scheme:host or host:port
            if (uri.StartsWith("stun") || uri.StartsWith("turn"))
            {
                hostPort = uri.Substring(schemeEnd + 1);
            }
        }

        // Remove any query parameters or path
        int queryIndex = hostPort.IndexOf('?');
        if (queryIndex >= 0)
            hostPort = hostPort.Substring(0, queryIndex);

        // Parse host:port
        int lastColon = hostPort.LastIndexOf(':');
        if (lastColon > 0 && lastColon < hostPort.Length - 1)
        {
            // Check if it's IPv6 (contains multiple colons)
            if (hostPort.Count(c => c == ':') > 1 && !hostPort.StartsWith("["))
            {
                // IPv6 without brackets - use whole string as host
                host = hostPort;
            }
            else
            {
                host = hostPort.Substring(0, lastColon);
                if (host.StartsWith("[") && host.EndsWith("]"))
                    host = host.Substring(1, host.Length - 2); // Remove IPv6 brackets
                    
                if (int.TryParse(hostPort.Substring(lastColon + 1), out int parsedPort))
                    port = parsedPort;
            }
        }
        else
        {
            host = hostPort;
        }

        return !string.IsNullOrEmpty(host);
    }
}
