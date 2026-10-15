using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VotingManager : MonoBehaviour
{
    [SerializeField] public GameObject yesNoVotePrefab;
    [SerializeField] public GameObject singleChoiceVotePrefab;
    public GameObject togglePrefab;

    private GameObject currentVoteWindow;


    public void TriggerVoting(string type, string question, float duration, List<string> options)
    {
        if (currentVoteWindow != null)
        {
            Destroy(currentVoteWindow);
        }

        if (type == "Yes/No")
        {
            InstantiateYesNoVoteWindow(question, duration);
        }
        else if (type == "MCQ")
        {
            InstantiateSingleChoiceVoteWindow(question, duration, options);
        }

    }

    // Coroutine to countdown and destroy the notification window
    private IEnumerator StartCountdown(GameObject votingWindow, float duration)
    {
        float remainingTime = duration;
        Text countdownText = votingWindow
                    .transform
                                    .Find("Menu")
                                    .Find("Canvas")
                                    .Find("Main Panel")
                                    .Find("Menu Panel")
                                    .Find("Notifcation Panel")
                                    .Find("Duration")
                                    .gameObject
                                    .GetComponent<Text>();

        if (countdownText != null)
        {
            while (remainingTime > 0)
            {
                countdownText.text = "Vote (" + remainingTime.ToString("F0") + ")";
                yield return new WaitForSeconds(1f);
                remainingTime--;
            }
        }

        if (votingWindow != null)
        {
            Destroy(votingWindow);
        }
    }


    private void InstantiateYesNoVoteWindow(string question, float duration)
    {
        if (yesNoVotePrefab == null)
        {
            Debug.LogError("Yes/No voting prefab is not assigned.");
            return;
        }

        // Instantiate the Yes/No voting window
        GameObject currentVoteWindow = Instantiate(yesNoVotePrefab);

        // Find the question Text component in the instantiated voting window
        Text questionText = currentVoteWindow.transform.Find("Menu/Canvas/Main Panel/Menu Panel/Notifcation Panel/Question").gameObject.GetComponent<Text>();
        if (questionText == null)
        {
            Debug.LogError("Question Text component not found in the instantiated voting window.");
            Destroy(currentVoteWindow);
            return;
        }

        // Set the text of the question
        questionText.text = question;

        // Start the countdown coroutine for this voting window
        StartCoroutine(StartCountdown(currentVoteWindow, duration));
    }


    private void InstantiateSingleChoiceVoteWindow(string question, float duration, List<string> options)
    {
        Debug.Log("" + options.Count + "" + question);
        // Instantiate the Single Choice Vote window
        currentVoteWindow = Instantiate(singleChoiceVotePrefab);

        // Find the question Text component in the instantiated voting window
        Text questionText = currentVoteWindow.transform
                                   .Find("Menu")
                                   .Find("Canvas")
                                   .Find("Main Panel")
                                   .Find("Menu Panel")
                                   .Find("Notifcation Panel")
                                   .Find("Question")
                                   .gameObject
                                   .GetComponent<Text>();

        // Set the text of the question
        questionText.text = question;

        // Find the Options Panel in the instantiated voting window
        Transform optionsPanel = currentVoteWindow.transform
                                                 .Find("Menu")
                                                 .Find("Canvas")
                                                 .Find("Main Panel")
                                                 .Find("Menu Panel")
                                                 .Find("Notifcation Panel")
                                                 .Find("Options Panel");


        RectTransform optionsRectTransform = optionsPanel.GetComponent<RectTransform>();

        // Calculate the width and height of a single toggle based on the dimensions of the options panel
        float toggleWidth = optionsRectTransform.sizeDelta.x / 2; // Two toggles per row
        float toggleHeight = optionsRectTransform.sizeDelta.y / Mathf.Ceil(options.Count / 2f);
        // Calculate the spacing between toggles
        float spacingX = 10f; // Adjust as needed
        float spacingY = 10f; // Adjust as needed

        // Counter variables for tracking the position of toggles
        int columnCount = 0;
        int rowCount = 0;

        foreach (string option in options)
        {
            // Create a new GameObject for the toggle
            GameObject toggleObject = new GameObject("Toggle");

            // Add necessary components to the GameObject
            Toggle toggle = toggleObject.AddComponent<Toggle>();
            
            // Set the toggle's parent to the options panel
            toggleObject.transform.SetParent(optionsPanel, false);

            // Create a new GameObject for the toggle's label (Text component)
            GameObject labelObject = new GameObject("Label");
            Text label = labelObject.AddComponent<Text>();

            label.text = option;
            label.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
            label.alignment = TextAnchor.MiddleLeft;

            // Set the label's parent to the toggleObject
            labelObject.transform.SetParent(toggleObject.transform, false);

            // Add functionality to the toggle (to work like radio buttons)
            //toggle.group = optionsPanel.GetComponent<ToggleGroup>();

            toggle.onValueChanged.AddListener((value) =>
            {
                if (value)
                {
                    Debug.Log("Selected option: " + option);
                }
            });

            // Calculate the position of the toggle
            float xOffset = (toggleWidth + spacingX) * columnCount;
            float yOffset = (toggleHeight + spacingY) * rowCount;

            // Set the position of the toggle
            toggleObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(xOffset, -yOffset);

            // Update column count and row count
            columnCount++;
            if (columnCount >= 2)
            {
                columnCount = 0;
                rowCount++;
            }
        }


        // Start the countdown coroutine for this voting window
        StartCoroutine(StartCountdown(currentVoteWindow, duration));
    }

}