using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomBackroundSound : MonoBehaviour
{
    public AudioSource randomSound;

    public AudioClip[] audioSources;
    // Start is called before the first frame update
    void Start()
    {
        CallAudio();
    }

    void CallAudio()
    {
        Invoke("RandomSoundness", 5);
    }

    void RandomSoundness()
    {
        randomSound.clip = audioSources[Random.Range(0, audioSources.Length)];
        randomSound.Play();
        CallAudio();
    }
}
