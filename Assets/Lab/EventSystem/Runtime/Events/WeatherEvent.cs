using System;
using TMPro;
using UnityEngine;
/// <summary>
/// This event changes the weather of the scene
/// </summary>
public class WeatherEvent : MonoBehaviour, IEvent
{
	private float brightness;
	private float rainIntensity;
	private float snowIntensity;
	private float cloudIntensity;
	private float fogDensity;
	private Boolean thunderstromIsOn;
	private float thunderstromDelay;
	private WeatherController weatherController;


	public void trigger()
	{
		weatherController.StartRain(rainIntensity);
		weatherController.StartSnow(snowIntensity);
		weatherController.StartCloud(cloudIntensity);
		weatherController.SetFogDensity(fogDensity);
		weatherController.SetBrightness(brightness);
		weatherController.SetThunderstromBooleanValue(thunderstromIsOn, thunderstromDelay);
	}
	public void setWeatherController(WeatherController weatherController)
	{
		this.weatherController = weatherController;
	}

	public void setRainIntensity(float rainIntensity)
	{
		this.rainIntensity = rainIntensity;
	}
	public void setSnowIntensity(float snowIntensity)
	{
		this.snowIntensity = snowIntensity;
	}
	public void setCloudIntensity(float cloudIntensity)
	{
		this.cloudIntensity = cloudIntensity;
	}
	public void setBrightness(float dayBrightness)
	{
		this.brightness = dayBrightness;
	}

	public void setFogDensity(float fogDensity)
	{
		this.fogDensity = fogDensity;
	}
	public void SetThunderstromBooleanValue(Boolean wert, float thunderstromDelay)
	{
		this.thunderstromIsOn = wert;
		this.thunderstromDelay = thunderstromDelay;
	}

	public void previewOn()
	{
		// nothing implemented
	}

	public void previewOff()
	{
		// nothing implemented
	}

	public void delete()
	{
		// nothing implemented
	}

	public string createJson(int groupID)
	{
		JsonWeatherEvent jsonObject = new JsonWeatherEvent { group = groupID, type = "WeatherEvent", brightness = this.brightness, rainIntensity = this.rainIntensity, snowIntensity = this.snowIntensity, cloudIntensity = this.cloudIntensity, fogDensity = this.fogDensity, thunderstromIsOn = this.thunderstromIsOn };
		return JsonUtility.ToJson(jsonObject);
	}
}
/// <summary>
/// Class that defines the JSON structure of this event
/// </summary>
public class JsonWeatherEvent
{
	public int group = 0;
	public string type;
	public float brightness;
	public float rainIntensity;
	public float snowIntensity;
	public float cloudIntensity;
	public float fogDensity;
	public bool thunderstromIsOn;
}