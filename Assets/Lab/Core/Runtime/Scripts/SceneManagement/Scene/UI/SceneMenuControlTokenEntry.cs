using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VaSiLi.UI;
using VaSiLi.WebCam;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Replaces the direct "Join" click on a scene row with a two-step token-entry flow:
    ///
    ///   1. User clicks the scene row's join button  → the token entry panel opens.
    ///   2. User types an interview token and presses Confirm (or hits Enter).
    ///   3. The token is validated (non-empty), stored in <see cref="InterviewTokenManager"/>,
    ///      and then <see cref="confirmButton"/>'s <see cref="ConditionalButton.OnSuccess"/>
    ///      fires — wire that in the Inspector to whatever the original join button called
    ///      (e.g. SceneMenuControlJoinButton.Join, PanelSwitcher.SwitchPanel, …).
    ///
    /// The <see cref="ConditionalButton"/> on the confirm button is configured at runtime
    /// with a condition that checks the token field is non-empty. All downstream actions
    /// remain in the Inspector as normal UnityEvent listeners on <c>OnSuccess</c>, so
    /// no C# changes are needed when the panel wiring changes.
    /// </summary>
    public class SceneMenuControlTokenEntry : MonoBehaviour
    {
        [Tooltip("Button on the scene row that opens the token panel (was the direct 'Join' button).")]
        public Button openPanelButton;

        [Tooltip("Panel that slides in to collect the interview token.")]
        public GameObject tokenPanel;

        [Tooltip("Input field inside the token panel.")]
        public TMP_InputField tokenInput;

        [Tooltip("Confirm button inside the token panel. Attach a ConditionalButton component to it " +
                 "and wire ConditionalButton.OnSuccess with all the actions the original join button " +
                 "called (SceneMenuControlJoinButton.Join, PanelSwitcher.SwitchPanel, …).")]
        public ConditionalButton confirmButton;

        [Tooltip("Optional cancel/back button inside the token panel.")]
        public Button cancelButton;

        [Tooltip("Optional label shown below the input when validation fails.")]
        public TMPro.TMP_Text validationMessage;

        void Start()
        {
            if (tokenPanel != null) tokenPanel.SetActive(false);

            if (openPanelButton != null)
                openPanelButton.onClick.AddListener(ShowTokenPanel);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(HideTokenPanel);

            if (tokenInput != null)
                tokenInput.onSubmit.AddListener(_ => TryConfirm());

            if (confirmButton != null)
            {
                // Set the validation condition: token field must be non-empty.
                confirmButton.Condition = ValidateToken;

                // After a successful confirm, close the panel.
                confirmButton.OnSuccess.AddListener(HideTokenPanel);

                // Wire the button's own onClick → ConditionalButton.Press so the user
                // can also assign it in the Inspector without needing to know the API.
                var btn = confirmButton.GetComponent<Button>();
                if (btn != null)
                    btn.onClick.AddListener(TryConfirm);

                // Show validation feedback on failure.
                confirmButton.OnFailure.AddListener(ShowValidationError);
            }
        }

        void ShowTokenPanel()
        {
            if (validationMessage != null) validationMessage.gameObject.SetActive(false);
            if (openPanelButton != null) openPanelButton.interactable = false;
            if (tokenPanel != null) tokenPanel.SetActive(true);
            if (tokenInput != null)
            {
                // Pre-fill with any previously entered token.
                tokenInput.text = (InterviewTokenManager.Instance != null && !string.IsNullOrEmpty(InterviewTokenManager.Instance.Token))
                    ? InterviewTokenManager.Instance.Token
                    : "123";
                tokenInput.ActivateInputField();
            }
        }

        void HideTokenPanel()
        {
            if (tokenPanel != null) tokenPanel.SetActive(false);
            if (openPanelButton != null) openPanelButton.interactable = true;
        }

        // Called by the confirm button's onClick (wired in Start) or by Enter in the input field.
        void TryConfirm()
        {
            // Store the token first so ConditionalButton.OnSuccess listeners (e.g. Join)
            // already see the updated value when they fire.
            if (ValidateToken())
            {
                if (InterviewTokenManager.Instance != null)
                    InterviewTokenManager.Instance.SetToken(tokenInput.text.Trim());
                else
                    Debug.LogWarning("[TokenEntry] InterviewTokenManager not in scene — token not stored.");
            }
            confirmButton?.Press();
        }

        bool ValidateToken()
        {
            return tokenInput != null && !string.IsNullOrEmpty(tokenInput.text.Trim());
        }

        void ShowValidationError()
        {
            if (validationMessage != null)
            {
                validationMessage.text = "Please enter a valid interview token.";
                validationMessage.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("[TokenEntry] Token field is empty.");
            }
        }
    }
}
