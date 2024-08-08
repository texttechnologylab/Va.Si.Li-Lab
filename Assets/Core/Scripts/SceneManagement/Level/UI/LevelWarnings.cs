using System;
using System.Linq;
using Ubiq.Samples;
using UnityEngine;
using VaSiLi.SceneManagement;

/// <summary>
/// Helper class to display the time warnings for the users
/// </summary>
public class LevelWarnings : MonoBehaviour
{
    public SocialMenu socialMenu;
    public PanelSwitcher rootPanelSwitcher;
    public PanelSwitcher rolePanelSwitcher;
    public GameObject rolePanel;
    public StartPanel startPanel;

    void OnEnable()
    {
        TimeManager.timerStopped += TimerStopped;
        TimeManager.timerUpdated += OnTimerUpdated;
    }

    void OnDisable()
    {
        TimeManager.timerStopped -= TimerStopped;
        TimeManager.timerUpdated -= OnTimerUpdated;
    }

    private void OnTimerUpdated(int timer)
    {
        var time = SceneManager.infos.FirstOrDefault((info) => info.mode == "timeWarning").description;
        try
        {
            if (Int32.Parse(time) == timer)
                ShowTimeWarning();
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
        }
    }

    private void ShowMessage()
    {
        socialMenu.Request();
        rootPanelSwitcher.SwitchPanel(rolePanel);
        rolePanelSwitcher.SwitchPanel(startPanel.gameObject);
    }

    private void ShowTimeWarning()
    {
        ShowMessage();
        Debug.Log("TimeWarning");
        var text = SceneManager.infos.FirstOrDefault((info) => info.mode == "endWarning").description;
        startPanel.startText.text = text;
    }

    private void TimerStopped()
    {
        ShowMessage();
        Debug.Log("TimeEnd");
        var text = SceneManager.infos.FirstOrDefault((info) => info.mode == "end").description;
        startPanel.startText.text = text;
    }
}