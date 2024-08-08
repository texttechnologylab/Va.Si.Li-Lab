using Ubiq.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Class to control the various UI element interactions
    /// </summary>
    public class LevelMenuControl : MonoBehaviour
    {
        public TMPro.TMP_Text levelDesciption;
        public Button startButton;
        public Toggle readyButton;
        public Button closeButton;
        public Button lastPhaseButton;
        public Button moveToNextLevelButton;
        public Button kickButton;
        public LevelManager levelManager;
        public RoleDescriptionControl roleDescriptionControl;
        public SocialMenu socialMenu;
        public PanelSwitcher panelSwitcher;

        void Awake()
        {
            LevelManager.levelChanged += delegate
            {
                if (RoleManager.CurrentRole?.mode == Mode.Player)
                {
                    UpdatePhaseButtons();
                    socialMenu.Request();
                    panelSwitcher.SwitchPanel(gameObject);
                }
            };
            LevelManager.levelStatus += UpdateLevelStatus;
            LevelManager.allReady += OnAllReady;
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
            if (RoleManager.CurrentRole?.admin == true)
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

            UpdatePhaseButtons();
            HandlePhaseButton(lastPhaseButton);

            if (RoleManager.CurrentRole?.admin == true && levelManager.CurrentStatus == LevelManager.Status.RUNNING)
                readyButton.gameObject.SetActive(false);
            else if (RoleManager.CurrentRole?.admin == true && levelManager.CurrentStatus == LevelManager.Status.WAITING)
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

        public void HandlePhaseButton(Button phaseButton)
        {
            int buttonIndex = int.Parse(phaseButton.GetComponentInChildren<Text>().text);

            if (RoleManager.CurrentRole?.level?.Length > buttonIndex - 1)
                levelDesciption.text = RoleManager.CurrentRole?.level[buttonIndex - 1].description;
            else
                levelDesciption.text = $"Phase {buttonIndex} text";
        }

        public void HandleNextPhaseButton()
        {
            levelManager.TryCompleteLevel();
            UpdatePhaseButtons();
        }

        private void UpdatePhaseButtons()
        {
            if (!RoleManager.CurrentRole.HasValue)
            {
                return;
            }
            int buttonIndex = int.Parse(lastPhaseButton.GetComponentInChildren<Text>().text);

            while (buttonIndex - 1 < levelManager.CurrentLevel && (
                    RoleManager.CurrentRole?.level?.Length > buttonIndex - 1 ||
                    RoleManager.CurrentRole?.mode == Mode.Spectator && SceneManager.CurrentScene?.level?.Length > buttonIndex - 1
                    )
                )
            {
                AddPhaseButton();
                buttonIndex++;
            }

            if ((RoleManager.CurrentRole.Value.admin || RoleManager.CurrentRole?.name == "hidden") && levelManager.CurrentLevel >= 0
            && int.Parse(lastPhaseButton.GetComponentInChildren<Text>().text) < RoleManager.CurrentRole?.level.Length
            && levelManager.CurrentStatus == LevelManager.Status.RUNNING)
                moveToNextLevelButton.gameObject.SetActive(true);
            else
                moveToNextLevelButton.gameObject.SetActive(false);

            if(levelManager.CurrentStatus == LevelManager.Status.WAITING) {
                moveToNextLevelButton.gameObject.SetActive(false);
            }

            if (RoleManager.CurrentRole?.name == "hidden")
            {
                kickButton.gameObject.SetActive(true);
            }
        }

        private void AddPhaseButton()
        {
            GameObject phaseButton = Instantiate(lastPhaseButton.gameObject, lastPhaseButton.transform.parent);
            int buttonIndex = int.Parse(lastPhaseButton.GetComponentInChildren<Text>().text);

            phaseButton.GetComponentInChildren<Text>().text = "" + (buttonIndex + 1);
            phaseButton.transform.localPosition += Vector3.down * 25;

            Button b = phaseButton.GetComponent<Button>();
            b.onClick.RemoveAllListeners();
            b.onClick.AddListener(delegate () { HandlePhaseButton(b); });

            lastPhaseButton = b;
        }
    }
}