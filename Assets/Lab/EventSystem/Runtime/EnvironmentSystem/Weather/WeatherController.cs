using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/**
* Implementation of Weather Simulation
* @author Gurpreet Singh
*/
public class WeatherController : MonoBehaviour, IWeatherSimulator
{

    public ParticleSystem snowParticles;
    public ParticleSystem rainParticles;
    public ParticleSystem cloudParticles;
    public ParticleSystem thunderParticleSystem;
    private Coroutine thunderStormCoroutine = null;

    //Start Methode
    void Start()
    {
        StopRain();
        StopSnow();
        StopCloud();
        StopThunderstrom();
        StopWeather();
    }
    
    [AgentTool(
        "start_rain",
        "Call when the user wants rain."
    )]
    public void StartRain(float intensity)
    {
        if (!rainParticles.isPlaying)
        {
            rainParticles.Play();
        }

        float rainIntensity = intensity * 100;
        var emissionModule = rainParticles.emission;
        emissionModule.rateOverTime = rainIntensity;
    }
    
    [AgentTool(
        "start_snow",
        "Call when the user wants snow."
    )]
    public void StartSnow(float intensity)
    {
        if (!snowParticles.isPlaying)
        {
            snowParticles.Play();
        }

        float snowIntensity = intensity * 1000;
        var emissionModule = snowParticles.emission;
        emissionModule.rateOverTime = snowIntensity;


    }
    
    [AgentTool(
        "start_cloud",
        "Call when the user wants clouds."
    )]
    public void StartCloud(float intensity)
    {
        if (!cloudParticles.isPlaying)
        {
            cloudParticles.Play();
        }
        float cloudIntensity = intensity * 1000;
        var emissionModule = cloudParticles.emission;
        emissionModule.rateOverTime = cloudIntensity;

    }

    public void SetThunderstromBooleanValue(Boolean value, float delay)
    {
        StopThunderstrom();
        if (value)
        {   
            Debug.Log("Thunderstrom getriggered!");
            StartThunderstormEffects(delay);
        }

    }
    public void StopRain()
    {
        if (rainParticles.isPlaying)
        {
            rainParticles.Stop();
        }
    }
    public void StopSnow()
    {
        if (snowParticles.isPlaying)
        {
            snowParticles.Stop();
        }


    }
    public void StopCloud()
    {
        cloudParticles.Stop();
    }

    public void StopThunderstrom()
    {
        if (thunderStormCoroutine != null)
        {
            StopCoroutine(thunderStormCoroutine);
            thunderStormCoroutine = null;
        }

        thunderParticleSystem.Stop();
    }

    public void StopWeather()
    {
        snowParticles.Stop();
        rainParticles.Stop();
        cloudParticles.Stop();
        thunderParticleSystem.Stop();
    }

    public void SetBrightness(float temperature)
    {
        float brightness = Mathf.Lerp(1f, 0.2f, Mathf.InverseLerp(0f, -15f, temperature));
        RenderSettings.skybox.SetFloat("_Exposure", brightness);
    }

    public void SetFogDensity(float fogDensity)
    {
        RenderSettings.fog = true;
        RenderSettings.fogDensity = fogDensity;
        RenderSettings.fogMode = FogMode.Exponential;
    }
     // Start a coroutine to simulate lightning duration
    private void StartThunderstormEffects(float delay)
    {
        this.thunderStormCoroutine = StartCoroutine(EmitParticlesWithDelay(delay));
    }

    private IEnumerator EmitParticlesWithDelay(float delay)
    {
        while (true)
        {
                thunderParticleSystem.Emit(1); 
                yield return new WaitForSeconds(delay);


            yield return null;
        }
    }

}
