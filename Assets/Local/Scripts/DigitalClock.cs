using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;
using VaSiLi.SceneManagement;
public class DigitalClock : MonoBehaviour
{
    public TextMeshPro textField;
    public bool realtime = false;
    private DateTime timer = new DateTime(2023, 1, 10, 8, 11, 0);

    void Start()
    {
        DateTime todayDate = DateTime.Today;
        textField = GetComponent<TextMeshPro>();
        TimeManager.timerStarted += StartClock;
        TimeManager.timerReset += OnTimerReset;
    }

    private void OnTimerReset()
    {
        timer = new DateTime(2023, 1, 10, 8, 11, 0);
    }

    private void StartClock()
    {
        StartCoroutine(UpdateClock());
    }

    IEnumerator UpdateClock()
    {
        while (true)
        {
            if (realtime)
            {
                timer = DateTime.Now;
            }
            else
            {
                timer = timer.AddSeconds(1);
            }
            textField.SetText(timer.ToString("hh:mm"));
            yield return new WaitForSeconds(1);
        }
    }

}
