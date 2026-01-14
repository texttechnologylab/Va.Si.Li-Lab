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
    public AudioClip Clip;
    private Dictionary<string, Queue<float[]>> audioQueue = new Dictionary<string, Queue<float[]>>();
    public RoomClient client;
    public AvatarManager avatarManager;
    private VoipPeerConnectionManager peerConnectionManager;
    private List<string> mutedUsers = new List<string>(); // List of muted user IDs
    private List<VoipPeerConnection> peerConnections = new List<VoipPeerConnection>();
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
        pc.OnIceConnectionStateChanged.AddListener(state => PeerConnection_OnIceConnectionStateChanged(state, pc.PeerUuid));

    }

    private void PeerConnection_OnIceConnectionStateChanged(VoipPeerConnection.IceConnectionState state, string uuid)
    {
        switch (state)
        {
            case VoipPeerConnection.IceConnectionState.closed:
            case VoipPeerConnection.IceConnectionState.failed:
            case VoipPeerConnection.IceConnectionState.disconnected:
                if (mutedUsers.Contains(uuid))
                {
                    mutedUsers.Remove(uuid);
                }
                break;
            case VoipPeerConnection.IceConnectionState.connected:
                Debug.Log($"Muting user {uuid} due to connection state {state}");
                if (!mutedUsers.Contains(uuid))
                    mutedUsers.Add(uuid);
                break;
            case VoipPeerConnection.IceConnectionState.@new:
            case VoipPeerConnection.IceConnectionState.checking:
            default:
                if (mutedUsers.Contains(uuid))
                {
                    mutedUsers.Remove(uuid);
                }
                break;
        }
    }
    void LateUpdate()
    {
        foreach (var userId in audioQueue.Keys)
        {
            var queue = audioQueue[userId];
            if (queue.Count > 8)
            {
                var audioData = new List<float>();
                while (queue.Count > 0)
                {
                    audioData.AddRange(queue.Dequeue());
                }
                if (audioData != null)
                {
                    PlayAudioClip(userId, audioData.ToArray());
                }
            }
        }

    }

    float[] LowPassFilter(float[] data, float sampleRate, float cutoffFrequency)
    {
        float RC = 1.0f / (2 * Mathf.PI * cutoffFrequency);
        float dt = 1.0f / sampleRate;
        float alpha = dt / (RC + dt);

        float[] filtered = new float[data.Length];
        filtered[0] = data[0]; // initialize

        for (int i = 1; i < data.Length; i++)
        {
            filtered[i] = filtered[i - 1] + alpha * (data[i] - filtered[i - 1]);
        }

        return filtered;
    }

    float[] BoostVolume(float[] data, float boostFactor)
    {
        float[] boosted = new float[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            boosted[i] = Mathf.Clamp(data[i] * boostFactor, -1f, 1f);
        }
        return boosted;
    }

    void PlayAudioClip(string userId, float[] audioData)
    {
        if (!ShouldPlayerUserAudio(userId))
            return;
        var user = client.Peers.FirstOrDefault(p => p.uuid == userId);
        AudioSource audioSource = null;
        if (user != null)
        {
            var avatar = avatarManager.FindAvatar(user);
            audioSource = avatar.gameObject.GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogWarning($"No AudioSource found on Avatar for user {userId}. Creating a new one.");
                audioSource = avatar.gameObject.AddComponent<AudioSource>();
                audioSource.spatialize = true;
                audioSource.maxDistance = 50;
            }
        }

        if (Clip == null || Clip.samples != audioData.Length)
        {
            Clip = AudioClip.Create("VoipAudio", audioData.Length, 1, 16000, false);
        }
        for (int i = 0; i < audioData.Length; i++)
        {
            audioData[i] = Mathf.Clamp(audioData[i] * 4f, -.9999f, .9999f); // Ensure values are within valid range
        }

        audioData = LowPassFilter(audioData, 16000, 2800); // Apply low pass filter
        audioData = BoostVolume(audioData, 1.3f); // Boost volume

        Clip.SetData(audioData, 0);

        if (audioSource != null)
        {
            audioSource.clip = Clip;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning("No AudioSource component found to play the audio clip.");
        }
    }

    bool ShouldPlayerUserAudio(string userId)
    {
        if (mutedUsers.Contains(userId))
        {
            return false;
        }
        return true;
    }

    // Update is called once per frame
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
        // TODO: Sorting and discarding of old entries
        var msg = message.FromJson<VoipAudioMessage>();
        if (!audioQueue.ContainsKey(msg.userId))
        {
            audioQueue[msg.userId] = new Queue<float[]>();
        }

        audioQueue[msg.userId].Enqueue(msg.data);

    }

}
