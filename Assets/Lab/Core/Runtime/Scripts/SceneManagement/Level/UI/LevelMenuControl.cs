using Ubiq.Samples;
using UnityEngine;
using UnityEngine.UI;
using System;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Class to control the various UI element interactions
    /// </summary>
    //TODO: Rewrite
    public class LevelMenuControl : MonoBehaviour
    {
        public TMPro.TMP_Text levelDesciption;
        public Button startButton;
        public Toggle readyButton;
        public Button closeButton;
        public LevelButton lastLevelButton;
        public Button moveToNextLevelButton;
        public Button kickButton;
        public LevelManager levelManager;
        public RoleDescriptionControl roleDescriptionControl;
        public SocialMenu socialMenu;
        public PanelSwitcher panelSwitcher;

        void Awake()
        {
            LevelManager.levelChanged += OnLevelChanged;
            LevelManager.levelStatus += UpdateLevelStatus;
            LevelManager.allReady += OnAllReady;
        }

        private void OnLevelChanged(int level)
        {
            UpdatePhaseButtons(level);
            socialMenu.Request();
            panelSwitcher.SwitchPanel(gameObject);
        }

        private void UpdateLevelStatus(int level, LevelManager.Status status)
        {
            switch (status)
            {
                case LevelManager.Status.RUNNING:
                    OnRunning();
                    closeButton.gameObject.SetActive(true);
                    break;
                // TODO: This currently makes the menu be re-requested when toggeling the ready button
                case LevelManager.Status.WAITING:
                    socialMenu.Request();
                    readyButton.gameObject.SetActive(true);
                    readyButton.isOn = false;
                    closeButton.gameObject.SetActive(false);
                    break;

            }
        }

        public void OnAllReady(bool ready)
        {
            readyButton.gameObject.SetActive(!ready);
            startButton.gameObject.SetActive(ready);
        }

        public void OnRunning()
        {
            if (RoleManager.CurrentRole?.IsAdmin == true)
            {
                moveToNextLevelButton.gameObject.SetActive(true);
            }
            readyButton.gameObject.SetActive(false);
            socialMenu.gameObject.SetActive(false);
            closeButton.gameObject.SetActive(true);
        }

        public void OnEnable()
        {
            readyButton.gameObject.SetActive(true);
            if (levelManager.CurrentStatus == LevelManager.Status.RUNNING)
            {
                closeButton.gameObject.SetActive(true);
                readyButton.gameObject.SetActive(false);
            }

            if (levelManager.CurrentStatus == LevelManager.Status.WAITING && readyButton.isOn)
            {
                readyButton.isOn = false;
            }

            UpdatePhaseButtons(LevelManager.CurrentLevelIndex);
            HandlePhaseButton(lastLevelButton);

            if (RoleManager.CurrentRole?.IsAdmin == true && levelManager.CurrentStatus == LevelManager.Status.RUNNING)
                readyButton.gameObject.SetActive(false);
            else if (RoleManager.CurrentRole?.IsAdmin == true && levelManager.CurrentStatus == LevelManager.Status.WAITING)
                readyButton.gameObject.SetActive(!startButton.gameObject.activeSelf);

            // Non-players should never have access to the ready button
            // and should always be able to close the menu
            if (RoleManager.CurrentRole?.mode != Mode.Player)
            {
                readyButton.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(true);
            }
        }

        public void HandleReadyButton(bool state)
        {
            if (state)
                levelManager.OnReady();
            else
                levelManager.OnUnReady();
        }

        public void HandleStartButton()
        {
            levelManager.StartLevel();
        }

        public void HandlePhaseButton(LevelButton phaseButton)
        {
            int buttonIndex = phaseButton.level;
            if (!LevelManager.CurrentLevel.HasValue ||
            !RoleManager.CurrentRole.HasValue ||
            !LevelManager.CurrentLevel.Value.roleDescriptions.ContainsKey(RoleManager.CurrentRole.Value.id) ||
            LevelManager.Levels.Length > buttonIndex - 1)
            {
                levelDesciption.text = $"Level {buttonIndex} placeholder text";
                return;
            }
            string[] data = LevelManager.GetLevelDescription().description;
            levelDesciption.text = String.Join("\n", data);
        }

        private void UpdatePhaseButtons(int level)
        {
            if (!RoleManager.CurrentRole.HasValue)
            {
                return;
            }
            int buttonIndex = lastLevelButton.level;

            while (buttonIndex - 1 < level)
            {
                AddPhaseButton();
                buttonIndex++;
            }

            if ((RoleManager.CurrentRole.Value.IsAdmin || RoleManager.CurrentRole.Value.IsSpectator)
            && LevelManager.CurrentLevelIndex >= 0
            && buttonIndex < LevelManager.Levels.Length
            && levelManager.CurrentStatus == LevelManager.Status.RUNNING)
                moveToNextLevelButton.gameObject.SetActive(true);
            else
                moveToNextLevelButton.gameObject.SetActive(false);


            if (levelManager.CurrentStatus == LevelManager.Status.WAITING)
            {
                moveToNextLevelButton.gameObject.SetActive(false);
            }

            // TODO:
            /*
            if (RoleManager.CurrentRole?.id == "hidden")
            {
                kickButton.gameObject.SetActive(true);
            }*/
        }

        private void UpdatePhaseButtonsOLD()
        {
            /*if (!RoleManager.CurrentRole.HasValue)
            {
                return;
            }
            int buttonIndex = lastLevelButton.level;

            while (buttonIndex - 1 < levelManager.CurrentLevelIndex && (
                    RoleManager.CurrentRole?.level?.Length > buttonIndex - 1 ||
                    RoleManager.CurrentRole?.mode == Mode.Spectator && SceneManager.CurrentScene?.levels?.Length > buttonIndex - 1
                    )
                )
            {
                AddPhaseButton();
                buttonIndex++;
            }

            if ((RoleManager.CurrentRole.Value.admin || RoleManager.CurrentRole?.mode == "spectator") && levelManager.CurrentLevelIndex >= 0
            && int.Parse(lastLevelButton.GetComponentInChildren<Text>().text) < RoleManager.CurrentRole?.level.Length
            && levelManager.CurrentStatus == LevelManager.Status.RUNNING)
                moveToNextLevelButton.gameObject.SetActive(true);
            else
                moveToNextLevelButton.gameObject.SetActive(false);

            if (levelManager.CurrentStatus == LevelManager.Status.WAITING)
            {
                moveToNextLevelButton.gameObject.SetActive(false);
            }

            if (RoleManager.CurrentRole?.name == "hidden")
            {
                kickButton.gameObject.SetActive(true);
            }*/
        }

        private void AddPhaseButton()
        {
            GameObject phaseButton = Instantiate(lastLevelButton.gameObject, lastLevelButton.transform.parent);
            int buttonIndex = lastLevelButton.level + 1;

            phaseButton.GetComponentInChildren<Text>().text = "" + buttonIndex;

            phaseButton.transform.localPosition += Vector3.down * 25;

            LevelButton b = phaseButton.GetComponent<LevelButton>();
            b.level = buttonIndex;
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(delegate () { HandlePhaseButton(b); });

            lastLevelButton = b;
        }
    }
}