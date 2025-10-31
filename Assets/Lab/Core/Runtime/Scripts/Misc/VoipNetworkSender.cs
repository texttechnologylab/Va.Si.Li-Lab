using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Ubiq.Avatars;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Voip.Implementations.Dotnet;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
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
    private AudioSource audioSource;
    public AvatarManager avatarManager;
    void Start()
    {
        context = NetworkScene.Register(this);
        audioSource = GetComponent<AudioSource>();
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

    void PlayAudioClip(string userId, float[] audioData)
    {

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
        Clip.SetData(audioData, 0);

        if (audioSource != null)
        {
            audioSource.clip = Clip;
            audioSource.Play();
        }
        else if (this.audioSource != null)
        {
            // Fallback to the AudioSource on this GameObject if no AudioSource is found on the Avatar
            this.audioSource.clip = Clip;
            this.audioSource.Play();
        }
        else
        {
            Debug.LogWarning("No AudioSource component found to play the audio clip.");
        }
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
