using System.Diagnostics;
using UnityEngine;

/*
Modifier_Container

@date 16.03.2026

@author Nick Korjakin
@version 1.0

Extern modifier, which changes the gravity in scene
*/
public class Modifier_Weather : MonoBehaviour, IModifier
{
    public bool active;

    public enum Mode
    {
        Rain,
        Thunder,
        Snow
    }

    [SerializeField] private Mode weatherState;
    public float weatherIntensity = 50f;

    public WeatherController weatherController;

    void Awake()
    {
        if(weatherController == null)
        {
            weatherController = FindFirstObjectByType<WeatherController>();
        }
    }

    public void Activate()
    {
        if(weatherController == null) return;
        switch (weatherState)
        {
            case Mode.Rain:
                weatherController.StartRain(weatherIntensity);
                break;
            case Mode.Thunder:
                weatherController.SetThunderstromBooleanValue(true, weatherIntensity);
                break;
            case Mode.Snow:
                weatherController.StartSnow(weatherIntensity);
                break;
        }
        active = true;
    }

    public void Deactivate()
    {
        if(weatherController == null) return;
        switch (weatherState)
        {
            case Mode.Rain:
                weatherController.StopRain();
                break;
            case Mode.Thunder:
                weatherController.StopThunderstrom();
                break;
            case Mode.Snow:
                weatherController.StopSnow();
                break;
        }
        active = false;
    }

    public void Toggle(GameObject target, float value)
    {
        if(active) Deactivate(); 
        else Activate();
    }

    public string getName()
    {
        return "Modifier_Weather";
    }
    public bool IsActive()
    {
        return active;
    }

    public string getDiscription()
    {
        return "Modifierziert das Wetter zu: " + weatherState;
    }
}
