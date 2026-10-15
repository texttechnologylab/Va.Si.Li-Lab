using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeatherUIController : MonoBehaviour
{
    public Slider brightnessSlider;
    public Slider rainIntensitySlider;
    public Slider snowIntensitySlider;
    public GameObject thunderStromDuration;
    public Toggle rainToggle;
    public Toggle snowToggle;
    public Toggle thunderStromToggle;

    void Start()
    {

        rainIntensitySlider.onValueChanged.AddListener(OnRainToggleValueChanged);
        snowIntensitySlider.onValueChanged.AddListener(OnSnowToggleValueChanged);

        // Add listener functions to the toggle events
        rainToggle.onValueChanged.AddListener(delegate { OnRainToggleValueChanged(rainIntensitySlider.value); });
        snowToggle.onValueChanged.AddListener(delegate { OnSnowToggleValueChanged(snowIntensitySlider.value); });
        thunderStromToggle.onValueChanged.AddListener(delegate {OnThunderStromValueChanged(thunderStromDuration.GetComponentInChildren<Text>().text); });
    }

    void OnRainToggleValueChanged(float intensity)
    {
        if (rainToggle.isOn)
        {

            rainIntensitySlider.gameObject.SetActive(true);

        }
        else
        {

            rainIntensitySlider.gameObject.SetActive(false);

        }
    }

    void OnSnowToggleValueChanged(float intensity)
    {
        if (snowToggle.isOn)
        {

            snowIntensitySlider.gameObject.SetActive(true);

        }
        else
        {
            snowIntensitySlider.gameObject.SetActive(false);

        }
    }
    void OnThunderStromValueChanged(string thunderstromduration)
    {
        if (thunderStromToggle.isOn)
        {
            thunderStromDuration.gameObject.SetActive(true);
        }
        else
        {
            thunderStromDuration.gameObject.SetActive(false);
        }
    }

}
