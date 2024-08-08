using UnityEngine;
using VaSiLi.SceneManagement;

/// <summary>
/// Helper class to play the bell sound when a certain time passes
/// </summary>
public class TimeEvents : MonoBehaviour
{
    public AudioSource bellSoundSource;
    void OnEnable()
    {
        TimeManager.timerUpdated += OnTimeManagerUpdate;
    }

    private void OnDisable()
    {
        TimeManager.timerUpdated -= OnTimeManagerUpdate;
    }

    void OnTimeManagerUpdate(int time)
    {
        // After one minute has passed play the sound
        if (time == 1)
        {
            bellSoundSource.Play();
        }
    }

}
