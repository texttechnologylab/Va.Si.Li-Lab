using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Voip;
using Ubiq.Voip.Implementations.Dotnet;
using UnityEngine;

public class VoipNetworkSender : MonoBehaviour
{
#if !UNITY_WEBGL
    private IVoipSource input;
#endif
    private NetworkContext context;
    private int counter = 0;
    
    public RoomClient client;
    public AvatarManager avatarManager;
    
    private VoipPeerConnectionManager peerConnectionManager;
    private List<string> mutedUsers = new List<string>();
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
    
    // Jitter buffers per user
    private Dictionary<string, VoipJitterBuffer> jitterBuffers = new Dictionary<string, VoipJitterBuffer>();
    
    // Track ICE connection state per user for detecting state changes
    private Dictionary<string, VoipPeerConnection.IceConnectionState> peerConnectionStates = 
        new Dictionary<string, VoipPeerConnection.IceConnectionState>();

    void Start()
    {
        context = NetworkScene.Register(this);
        peerConnectionManager = GameObject.Find("Voip Manager").GetComponent<VoipPeerConnectionManager>();
        peerConnectionManager.OnPeerConnection.AddListener(
            PeerConnectionManager_OnPeerConnection, runExisting: true);
    }

    private void PeerConnectionManager_OnPeerConnection(VoipPeerConnection pc)
    {
        if (peerConnections.Contains(pc))
            return;
            
        var peerConnection = peerConnections.Find(x => x == pc);
        if (peerConnection != null)
        {
            peerConnection.OnIceConnectionStateChanged.RemoveListener(state => PeerConnection_OnIceConnectionStateChanged(state, pc.PeerUuid));
            peerConnections.Remove(pc);
        }
        peerConnections.Add(pc);
        
        // Initialize peer state tracking - start with fallback mode (unmuted)
        string peerId = pc.PeerUuid;
        peerConnectionStates[peerId] = VoipPeerConnection.IceConnectionState.@new;
        
        // Ensure peer is unmuted initially (using fallback until ICE succeeds)
        if (!mutedUsers.Contains(peerId))
        {
            mutedUsers.Add(peerId);
        }
        
        pc.OnIceConnectionStateChanged.AddListener(state => PeerConnection_OnIceConnectionStateChanged(state, pc.PeerUuid));
        Debug.Log($"VoipNetworkSender: Peer {peerId} registered, starting in fallback mode (muted fallback until ICE connects)");
    }

    private void PeerConnection_OnIceConnectionStateChanged(VoipPeerConnection.IceConnectionState state, string uuid)
    {
        // Track previous state for logging
        peerConnectionStates.TryGetValue(uuid, out var previousState);
        peerConnectionStates[uuid] = state;
        
        bool shouldMuteFallback = false;
        string reason = "";

        switch (state)
        {
            case VoipPeerConnection.IceConnectionState.connected:
                // ICE connection established: mute fallback jitter buffer
                // Use the proper WebRTC connection instead
                shouldMuteFallback = true;
                reason = "ICE connected successfully";
                break;
                
            case VoipPeerConnection.IceConnectionState.closed:
            case VoipPeerConnection.IceConnectionState.failed:
            case VoipPeerConnection.IceConnectionState.disconnected:
                // ICE connection lost: unmute fallback jitter buffer
                shouldMuteFallback = false;
                reason = $"ICE {state}, falling back to jitter buffer";
                
                // Reset jitter buffer state for clean restart
                if (jitterBuffers.TryGetValue(uuid, out var buffer))
                {
                    buffer.Reset();
                }
                break;
                
            case VoipPeerConnection.IceConnectionState.checking:
            case VoipPeerConnection.IceConnectionState.@new:
                // Still trying to establish connection: keep fallback unmuted
                // (don't mute until we're sure ICE worked)
                shouldMuteFallback = false;
                reason = $"ICE {state}, using fallback";
                break;
                
            default:
                // Unknown state: use fallback (unmuted)
                shouldMuteFallback = false;
                reason = $"ICE unknown state '{state}', using fallback";
                break;
        }

        // Update mute state
        bool isMuted = mutedUsers.Contains(uuid);
        if (shouldMuteFallback && !isMuted)
        {
            mutedUsers.Add(uuid);
            Debug.Log($"VoipNetworkSender: Muting fallback for {uuid} ({reason})");
        }
        else if (!shouldMuteFallback && isMuted)
        {
            mutedUsers.Remove(uuid);
            Debug.Log($"VoipNetworkSender: Unmuting fallback for {uuid} ({reason})");
        }
        
        // Update jitter buffer mute state (only if jitter buffer exists)
        // Don't create it here - it's created on first audio packet
        if (jitterBuffers.TryGetValue(uuid, out var jb))
        {
            jb.IsMuted = shouldMuteFallback;
        }
    }

    /// <summary>
    /// Gets or creates a jitter buffer for a specific user.
    /// </summary>
    private VoipJitterBuffer GetOrCreateJitterBuffer(string userId)
    {
        if (jitterBuffers.TryGetValue(userId, out var existingBuffer))
        {
            return existingBuffer;
        }

        // Find the user's avatar and audio source
        var user = client.Peers.FirstOrDefault(p => p.uuid == userId);
        AudioSource audioSource = null;
        
        if (user != null)
        {
            var avatar = avatarManager.FindAvatar(user);
            if (avatar != null)
            {
                audioSource = avatar.gameObject.GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    Debug.Log($"Creating AudioSource for user {userId}");
                    audioSource = avatar.gameObject.AddComponent<AudioSource>();
                    audioSource.spatialize = true;
                    audioSource.spatialBlend = 1f; // Full 3D
                    audioSource.maxDistance = 50;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                }

                // Add spatialization cache BEFORE jitter buffer.
                // Unity calls OnAudioFilterRead in component order, so the cache
                // must exist first to capture spatialization coefficients before
                // VoipJitterBuffer overwrites the audio data.
                var spatCache = avatar.gameObject.GetComponent<VoipSpatialisationCache>();
                if (spatCache == null)
                {
                    spatCache = avatar.gameObject.AddComponent<VoipSpatialisationCache>();
                }

                // Add or get jitter buffer component on the avatar
                var jitterBuffer = avatar.gameObject.GetComponent<VoipJitterBuffer>();
                if (jitterBuffer == null)
                {
                    jitterBuffer = avatar.gameObject.AddComponent<VoipJitterBuffer>();
                }
                
                jitterBuffer.Initialize(userId, audioSource);
                jitterBuffer.IsMuted = mutedUsers.Contains(userId);
                jitterBuffers[userId] = jitterBuffer;
                
                return jitterBuffer;
            }
        }

        return null;
    }

    void Update()
    {
#if !UNITY_WEBGL
        if (input == null)
        {
            var go = GameObject.Find("Microphone Dotnet Voip Source");
            if (go != null)
            {
                Debug.Log("Input resolved");
                input = go.GetComponent<IVoipSource>();
                if (input is MicrophoneVoipSource)
                {
                    Debug.Log("Valid Input");
                    ((MicrophoneVoipSource)input).OnAudioPcmSample += OnAudioEncoded;
                }
            }
        }
#endif
    }

    private void OnAudioEncoded(SIPSorceryMedia.Abstractions.AudioSamplingRatesEnum samplingRate, float[] sample)
    {
        context.SendJson(new VoipAudioMessage(counter++, client.Me.uuid, sample));
        if (counter > 10000)
            counter = 0;
    }

    private struct VoipAudioMessage
    {
        public float[] data;
        public int counter;
        public string userId;

        public VoipAudioMessage(int counter, string userId, float[] data)
        {
            this.data = data;
            this.userId = userId;
            this.counter = counter;
        }
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var msg = message.FromJson<VoipAudioMessage>();
        
        // Get or create jitter buffer for this user
        var jitterBuffer = GetOrCreateJitterBuffer(msg.userId);
        if (jitterBuffer != null)
        {
            // Enqueue with sequence number for proper ordering
            jitterBuffer.EnqueuePacket(msg.counter, msg.data);
        }
    }

    void OnDestroy()
    {
        // Clean up all jitter buffers and state
        foreach (var kvp in jitterBuffers)
        {
            if (kvp.Value != null)
            {
                kvp.Value.Reset();
            }
        }
        jitterBuffers.Clear();
        mutedUsers.Clear();
        peerConnectionStates.Clear();
    }
}
