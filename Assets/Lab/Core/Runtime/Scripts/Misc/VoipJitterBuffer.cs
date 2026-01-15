using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// WebRTC-style jitter buffer that handles audio packet reordering, 
/// adaptive playback rate, and demand-driven dequeuing via OnAudioFilterRead.
/// </summary>
public class VoipJitterBuffer : MonoBehaviour
{
    // Jitter buffer configuration
    private const int SAMPLE_RATE = 16000;
    private const int TARGET_BUFFER_MS = 80;        // Target buffering latency
    private const int MIN_BUFFER_MS = 40;           // Minimum buffer before playback starts
    private const int MAX_BUFFER_MS = 200;          // Maximum buffer before speedup
    private const float MAX_SPEEDUP = 1.05f;        // Maximum playback speedup (5%)
    private const float MAX_SLOWDOWN = 0.95f;       // Maximum playback slowdown (5%)
    private const float RATE_SMOOTHING = 0.02f;     // Rate adjustment smoothing factor

    // Audio processing
    private const float VOLUME_BOOST = 4f;
    private const float POST_FILTER_BOOST = 1.3f;
    private const float LOW_PASS_CUTOFF = 3400f;   // More aggressive cutoff to remove high-freq hiss (voice is ~300-3400 Hz)
    private const float HIGH_PASS_CUTOFF = 200f;   // More aggressive to remove rumble and wind noise
    private const float NOISE_GATE_THRESHOLD = 0.02f;  // Suppress audio below this amplitude (-34 dB)
    private const float NOISE_GATE_RELEASE_TIME = 0.2f; // Smooth gate release in seconds

    // Ring buffer for audio samples (thread-safe circular buffer)
    private float[] ringBuffer;
    private int ringBufferSize;
    private volatile int writeIndex = 0;
    private volatile int readIndex = 0;
    private readonly object bufferLock = new object();

    // Packet reordering
    private SortedDictionary<int, float[]> packetBuffer = new SortedDictionary<int, float[]>();
    private int expectedSequenceNumber = -1;
    private int lastProcessedSequence = -1;
    private const int MAX_OUT_OF_ORDER_PACKETS = 10;
    private const int SEQUENCE_WRAP_THRESHOLD = 9000; // Handle counter wrap at 10000

    // Playback state
    private float currentPlaybackRate = 1.0f;
    private float targetPlaybackRate = 1.0f;
    private double resamplePosition = 0.0;
    private bool isPlaying = false;
    private float lastSample = 0f; // For interpolation

    // Low-pass filter state
    private float filterState = 0f;
    private float filterAlpha;
    
    // Biquad low-pass filter coefficients (for better frequency response)
    private float biquadB0, biquadB1, biquadB2;
    private float biquadA1, biquadA2;
    private float biquadZ1 = 0f, biquadZ2 = 0f;
    
    // Biquad high-pass filter coefficients
    private float highPassB0, highPassB1, highPassB2;
    private float highPassA1, highPassA2;
    private float highPassZ1 = 0f, highPassZ2 = 0f;
    
    // Noise gate state
    private float noiseGateLevel = 0f;

    // Statistics
    public int BufferedSamples => GetBufferedSampleCount();
    public float PlaybackRate => currentPlaybackRate;
    public bool IsPlaying => isPlaying;

    // User info
    public string UserId { get; private set; }
    private bool isMuted = false;
    public bool IsMuted
    {
        get => isMuted;
        set => isMuted = value;
    }

    private AudioSource audioSource;
    private bool isInitialized = false;
    private int outputSampleRate;
    private double resampleRatio;

    void Awake()
    {
        // Ensure we have an AudioSource on this GameObject for OnAudioFilterRead to work
        audioSource = GetComponent<AudioSource>();
        
        // Cache output sample rate - must be called from main thread
        outputSampleRate = AudioSettings.outputSampleRate;
        
        // Calculate resample ratio: convert from input sample rate (16kHz) to output sample rate
        resampleRatio = (double)SAMPLE_RATE / outputSampleRate;
    }

    public void Initialize(string userId, AudioSource source)
    {
        if (isInitialized)
            return;

        UserId = userId;
        audioSource = source;

        // Calculate ring buffer size (enough for MAX_BUFFER_MS * 2 for safety)
        ringBufferSize = (SAMPLE_RATE * MAX_BUFFER_MS * 2) / 1000;
        ringBuffer = new float[ringBufferSize];

        // Pre-calculate low-pass filter coefficient
        float RC = 1.0f / (2 * Mathf.PI * LOW_PASS_CUTOFF);
        float dt = 1.0f / SAMPLE_RATE;
        filterAlpha = dt / (RC + dt);
        
        // Initialize biquad low-pass filter coefficients
        // Second-order Butterworth low-pass filter for better attenuation
        CalculateBiquadCoefficients(LOW_PASS_CUTOFF, SAMPLE_RATE, 0.707f); // Q = 0.707 for Butterworth response
        
        // Initialize high-pass filter coefficients
        // High-pass at 200 Hz to remove rumble, wind noise, and subsonic frequencies
        CalculateHighPassCoefficients(HIGH_PASS_CUTOFF, SAMPLE_RATE, 0.707f);

        // Configure audio source for OnAudioFilterRead streaming
        // OnAudioFilterRead requires: AudioSource on same GameObject, AudioSource playing
        if (audioSource != null)
        {
            // Create a short looping clip - OnAudioFilterRead will replace the audio data
            // Using output sample rate for proper playback
            int outputSampleRate = AudioSettings.outputSampleRate;
            var streamClip = AudioClip.Create("VoipStream_" + userId, outputSampleRate, 1, outputSampleRate, false);
            float[] silence = new float[outputSampleRate];
            streamClip.SetData(silence, 0);
            
            audioSource.clip = streamClip;
            audioSource.loop = true;
            audioSource.volume = 1.0f;
            audioSource.priority = 0; // Highest priority for voice
            audioSource.Play();
            
            isInitialized = true;
            Debug.Log($"VoipJitterBuffer initialized for user {userId}, output sample rate: {outputSampleRate}");
        }
        else
        {
            Debug.LogError($"VoipJitterBuffer: No AudioSource found for user {userId}. OnAudioFilterRead will not work!");
        }
    }

    /// <summary>
    /// Check if seqA is older than seqB, accounting for wrap-around at 10000.
    /// </summary>
    private bool IsSequenceOlder(int seqA, int seqB)
    {
        // Handle wrap-around: if B > threshold and A is small, A is actually newer
        if (seqB > SEQUENCE_WRAP_THRESHOLD && seqA < 1000)
            return false; // A is from new cycle, newer than B
        
        // Otherwise simple comparison
        return seqA < seqB;
    }

    /// <summary>
    /// Calculate the signed distance between two sequence numbers.
    /// Positive means seqA is ahead, negative means seqA is behind.
    /// Handles wrap-around at 10000.
    /// </summary>
    private int GetSequenceDistance(int seqA, int seqB)
    {
        // If B is near wrap point and A is small, A wrapped around
        if (seqB > SEQUENCE_WRAP_THRESHOLD && seqA < 1000)
            return seqA - seqB + 10000;
        
        return seqA - seqB;
    }

    /// <summary>
    /// Enqueue a received audio packet with sequence number for reordering.
    /// Handles wrap-around at 10000 using proper modular arithmetic.
    /// </summary>
    public void EnqueuePacket(int sequenceNumber, float[] audioData)
    {
        if (audioData == null || audioData.Length == 0)
            return;

        lock (bufferLock)
        {
            // Initialize expected sequence on first packet
            if (expectedSequenceNumber == -1)
            {
                expectedSequenceNumber = sequenceNumber;
            }

            // Discard packets that are too old (already processed)
            if (lastProcessedSequence != -1 && IsSequenceOlder(sequenceNumber, lastProcessedSequence))
            {
                return; // Packet is too old, discard
            }

            // Add to packet buffer for reordering (use raw sequence number as key)
            if (!packetBuffer.ContainsKey(sequenceNumber))
            {
                packetBuffer[sequenceNumber] = audioData;
            }

            // Limit packet buffer size to prevent memory bloat
            while (packetBuffer.Count > MAX_OUT_OF_ORDER_PACKETS * 2)
            {
                var firstKey = packetBuffer.Keys.First();
                packetBuffer.Remove(firstKey);
            }

            // Process packets in order
            ProcessOrderedPackets();
        }
    }

    private void ProcessOrderedPackets()
    {
        // Process packets in order
        while (packetBuffer.Count > 0)
        {
            var firstKey = packetBuffer.Keys.First();
            
            // Check if this is the expected packet
            if (firstKey == expectedSequenceNumber)
            {
                // Packet is in order, process it
                var audioData = packetBuffer[firstKey];
                packetBuffer.Remove(firstKey);
                
                WriteToRingBuffer(audioData);
                
                lastProcessedSequence = firstKey;
                expectedSequenceNumber = (firstKey + 1) % 10000;
                if (expectedSequenceNumber == 0) expectedSequenceNumber = 10000;
            }
            else
            {
                // Packet is out of order
                int distance = GetSequenceDistance(firstKey, expectedSequenceNumber);
                
                if (distance <= 0)
                {
                    // This should have been discarded already, but just in case
                    packetBuffer.Remove(firstKey);
                    continue;
                }
                
                // Check if we should wait for missing packets or skip ahead
                if (distance > MAX_OUT_OF_ORDER_PACKETS)
                {
                    // Large gap detected (packet loss), skip the missing packets
                    Debug.LogWarning($"VoipJitterBuffer ({UserId}): Skipping {distance - 1} lost packets. " +
                        $"Expected {expectedSequenceNumber}, got {firstKey}");
                    expectedSequenceNumber = firstKey;
                    
                    // Process this packet
                    var audioData = packetBuffer[firstKey];
                    packetBuffer.Remove(firstKey);
                    WriteToRingBuffer(audioData);
                    
                    lastProcessedSequence = firstKey;
                    expectedSequenceNumber = (firstKey + 1) % 10000;
                    if (expectedSequenceNumber == 0) expectedSequenceNumber = 10000;
                }
                else if (packetBuffer.Count >= MAX_OUT_OF_ORDER_PACKETS)
                {
                    // Too many buffered out-of-order packets, skip ahead to prevent stalling
                    Debug.LogWarning($"VoipJitterBuffer ({UserId}): Buffer overflow with {packetBuffer.Count} packets. " +
                        $"Skipping to sequence {firstKey}");
                    expectedSequenceNumber = firstKey;
                    
                    var audioData = packetBuffer[firstKey];
                    packetBuffer.Remove(firstKey);
                    WriteToRingBuffer(audioData);
                    
                    lastProcessedSequence = firstKey;
                    expectedSequenceNumber = (firstKey + 1) % 10000;
                    if (expectedSequenceNumber == 0) expectedSequenceNumber = 10000;
                }
                else
                {
                    // Wait for missing packets
                    break;
                }
            }
        }
    }

    private void WriteToRingBuffer(float[] audioData)
    {
        // Process audio: boost, filter
        for (int i = 0; i < audioData.Length; i++)
        {
            // Volume boost and clamp
            float sample = Mathf.Clamp(audioData[i] * VOLUME_BOOST, -0.9999f, 0.9999f);
            
            // Apply high-pass filter first (remove DC and low rumble)
            sample = ApplyHighPassFilter(sample);
            
            // Apply low-pass filter (reduce high-frequency noise)
            sample = ApplyBiquadFilter(sample);
            
            // Post-filter boost
            sample = Mathf.Clamp(sample * POST_FILTER_BOOST, -1f, 1f);
            
            // Apply noise gate to suppress background noise
            // This uses a simple envelope follower that suppresses audio below the noise floor
            float sampleAbs = Mathf.Abs(sample);
            float releaseCoeff = Mathf.Exp(-1.0f / (SAMPLE_RATE * NOISE_GATE_RELEASE_TIME));
            
            if (sampleAbs > noiseGateLevel)
            {
                // Fast attack to loud signals
                noiseGateLevel = sampleAbs;
            }
            else
            {
                // Slow release from peak
                noiseGateLevel = noiseGateLevel * releaseCoeff + sampleAbs * (1.0f - releaseCoeff);
            }
            
            // Gate: suppress sample if below threshold
            if (noiseGateLevel < NOISE_GATE_THRESHOLD)
            {
                sample = 0f;
            }
            
            // Write to ring buffer
            ringBuffer[writeIndex] = sample;
            writeIndex = (writeIndex + 1) % ringBufferSize;
        }

        // Start playback if we have enough buffered
        if (!isPlaying && GetBufferedSampleCount() >= (SAMPLE_RATE * MIN_BUFFER_MS) / 1000)
        {
            isPlaying = true;
        }
    }

    /// <summary>
    /// Calculate biquad filter coefficients for a low-pass filter.
    /// Implements a second-order Butterworth low-pass filter.
    /// </summary>
    private void CalculateBiquadCoefficients(float cutoffFrequency, int sampleRate, float Q)
    {
        float w0 = 2.0f * Mathf.PI * cutoffFrequency / sampleRate;
        float sinW0 = Mathf.Sin(w0);
        float cosW0 = Mathf.Cos(w0);
        float alpha = sinW0 / (2.0f * Q);
        
        // For low-pass filter
        biquadB0 = (1.0f - cosW0) / 2.0f;
        biquadB1 = 1.0f - cosW0;
        biquadB2 = (1.0f - cosW0) / 2.0f;
        
        float a0 = 1.0f + alpha;
        biquadA1 = -2.0f * cosW0 / a0;
        biquadA2 = (1.0f - alpha) / a0;
        
        // Normalize b coefficients
        biquadB0 /= a0;
        biquadB1 /= a0;
        biquadB2 /= a0;
    }

    /// <summary>
    /// Apply biquad low-pass filter to a single sample.
    /// Uses direct form II implementation for numerical stability.
    /// </summary>
    private float ApplyBiquadFilter(float input)
    {
        // Direct form II
        float w = input - biquadA1 * biquadZ1 - biquadA2 * biquadZ2;
        float output = biquadB0 * w + biquadB1 * biquadZ1 + biquadB2 * biquadZ2;
        
        biquadZ2 = biquadZ1;
        biquadZ1 = w;
        
        return output;
    }

    /// <summary>
    /// Calculate biquad filter coefficients for a high-pass filter.
    /// Implements a second-order Butterworth high-pass filter to remove DC offset and low rumble.
    /// </summary>
    private void CalculateHighPassCoefficients(float cutoffFrequency, int sampleRate, float Q)
    {
        float w0 = 2.0f * Mathf.PI * cutoffFrequency / sampleRate;
        float sinW0 = Mathf.Sin(w0);
        float cosW0 = Mathf.Cos(w0);
        float alpha = sinW0 / (2.0f * Q);
        
        // For high-pass filter
        highPassB0 = (1.0f + cosW0) / 2.0f;
        highPassB1 = -(1.0f + cosW0);
        highPassB2 = (1.0f + cosW0) / 2.0f;
        
        float a0 = 1.0f + alpha;
        highPassA1 = -2.0f * cosW0 / a0;
        highPassA2 = (1.0f - alpha) / a0;
        
        // Normalize b coefficients
        highPassB0 /= a0;
        highPassB1 /= a0;
        highPassB2 /= a0;
    }

    /// <summary>
    /// Apply biquad high-pass filter to a single sample.
    /// Uses direct form II implementation for numerical stability.
    /// </summary>
    private float ApplyHighPassFilter(float input)
    {
        // Direct form II
        float w = input - highPassA1 * highPassZ1 - highPassA2 * highPassZ2;
        float output = highPassB0 * w + highPassB1 * highPassZ1 + highPassB2 * highPassZ2;
        
        highPassZ2 = highPassZ1;
        highPassZ1 = w;
        
        return output;
    }

    private int GetBufferedSampleCount()
    {
        int write = writeIndex;
        int read = readIndex;
        
        if (write >= read)
            return write - read;
        else
            return ringBufferSize - read + write;
    }

    /// <summary>
    /// Called by Unity's audio thread. This is where we implement demand-driven dequeuing
    /// and adaptive playback rate.
    /// </summary>
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!isPlaying || isMuted)
        {
            // Output silence
            Array.Clear(data, 0, data.Length);
            return;
        }

        lock (bufferLock)
        {
            int bufferedSamples = GetBufferedSampleCount();
            int bufferedMs = (bufferedSamples * 1000) / SAMPLE_RATE;

            // Calculate target playback rate based on buffer fill level
            if (bufferedMs > MAX_BUFFER_MS)
            {
                // Buffer is too full, speed up playback
                float excess = (float)(bufferedMs - TARGET_BUFFER_MS) / (MAX_BUFFER_MS - TARGET_BUFFER_MS);
                targetPlaybackRate = Mathf.Lerp(1.0f, MAX_SPEEDUP, Mathf.Clamp01(excess));
            }
            else if (bufferedMs < MIN_BUFFER_MS)
            {
                // Buffer is getting low, slow down slightly
                float deficit = (float)(TARGET_BUFFER_MS - bufferedMs) / TARGET_BUFFER_MS;
                targetPlaybackRate = Mathf.Lerp(1.0f, MAX_SLOWDOWN, Mathf.Clamp01(deficit));
            }
            else
            {
                targetPlaybackRate = 1.0f;
            }

            // Smooth rate transitions to avoid audio artifacts
            currentPlaybackRate = Mathf.Lerp(currentPlaybackRate, targetPlaybackRate, RATE_SMOOTHING);

            // Fill output buffer with resampled audio
            // resampleRatio is pre-calculated in Awake() to convert from 16kHz to output sample rate
            int samplesPerChannel = data.Length / channels;
            
            for (int i = 0; i < samplesPerChannel; i++)
            {
                float sample = 0f;

                if (bufferedSamples > 0)
                {
                    // Linear interpolation for smooth resampling
                    int index0 = readIndex;
                    int index1 = (readIndex + 1) % ringBufferSize;
                    float frac = (float)(resamplePosition - Math.Floor(resamplePosition));

                    float sample0 = ringBuffer[index0];
                    float sample1 = (index1 != writeIndex) ? ringBuffer[index1] : sample0;
                    
                    sample = sample0 + frac * (sample1 - sample0);
                    lastSample = sample;

                    // Advance read position: resampleRatio * playbackRate
                    // resampleRatio converts sample rates, playbackRate handles jitter compensation
                    resamplePosition += resampleRatio * currentPlaybackRate;
                    
                    while (resamplePosition >= 1.0)
                    {
                        resamplePosition -= 1.0;
                        readIndex = (readIndex + 1) % ringBufferSize;
                        bufferedSamples = GetBufferedSampleCount();
                        
                        if (bufferedSamples <= 0)
                        {
                            resamplePosition = 0;
                            break;
                        }
                    }
                }
                else
                {
                    // Buffer underrun - fade out smoothly
                    sample = lastSample * 0.95f;
                    lastSample = sample;
                    
                    if (Mathf.Abs(lastSample) < 0.001f)
                    {
                        isPlaying = false;
                        lastSample = 0f;
                    }
                }

                // Write to all channels (mono to stereo/surround expansion)
                for (int ch = 0; ch < channels; ch++)
                {
                    data[i * channels + ch] = sample;
                }
            }
        }
    }

    public void Reset()
    {
        lock (bufferLock)
        {
            writeIndex = 0;
            readIndex = 0;
            resamplePosition = 0;
            isPlaying = false;
            expectedSequenceNumber = -1;
            lastProcessedSequence = -1;
            packetBuffer.Clear();
            filterState = 0f;
            biquadZ1 = 0f;
            biquadZ2 = 0f;
            highPassZ1 = 0f;
            highPassZ2 = 0f;
            noiseGateLevel = 0f;
            lastSample = 0f;
            currentPlaybackRate = 1.0f;
            targetPlaybackRate = 1.0f;
            
            if (ringBuffer != null)
                Array.Clear(ringBuffer, 0, ringBuffer.Length);
        }
        isInitialized = false;
    }

    void OnDisable()
    {
        // Stop the AudioSource when disabled to prevent OnAudioFilterRead from being called
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    void OnDestroy()
    {
        Reset();
    }
}
