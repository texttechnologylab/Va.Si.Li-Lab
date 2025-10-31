using UnityEngine;

/// <summary>
/// Only let's the attached GameObject spawn once
/// </summary>
public class SceneMenuLimiter : MonoBehaviour
{
    private static bool initiated;

    void Awake()
    {
        if (initiated)
        {
            gameObject.SetActive(false);
            Destroy(this);
        }
        initiated = true;
    }
}