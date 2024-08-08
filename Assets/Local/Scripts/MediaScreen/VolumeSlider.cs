using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ubiq.XR;

public class VolumeSlider : MonoBehaviour, IGraspable
{

    AudioSource audioSource;
    float maxY, minY;

    Hand graspingHand = null;

    void Start() 
    {
        audioSource = transform.parent.parent.GetComponentInChildren<AudioSource>();
        Transform slider = transform.parent.Find("Slider");

        maxY = slider.transform.position.y + slider.transform.localScale.y / 2;
        minY = slider.transform.position.y - slider.transform.localScale.y / 2;
    }

    void Update()
    {
        if(graspingHand != null){
            Vector3 newPos = transform.position;
            float handY = graspingHand.transform.position.y;
            if(handY > minY && handY < maxY){
                newPos.y = handY;
                setVolume(handY);
            }
                
            transform.position = newPos;
        }
    }

    void setVolume(float sliderPos){
        if(audioSource == null) return;

        float diffY = maxY - minY;
        float localSliderPos = sliderPos - minY;

        float volume = localSliderPos/ diffY;

        audioSource.volume = volume;
    }

    void IGraspable.Grasp(Hand controller)
    {
        graspingHand = controller;
    }

    void IGraspable.Release(Hand controller)
    {
        graspingHand = null;
    }
}
