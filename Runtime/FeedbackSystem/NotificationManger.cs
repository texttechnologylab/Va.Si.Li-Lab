using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    [SerializeField] public GameObject notificationPrefab;//template for instantiating new instances.
    public Transform userCamera;
    [SerializeField] private GameObject currentNotification;


    // Function to trigger different types of notifications

    public void TriggerNotification(string notificationText, float duration, string notificationType, string position)
    {
        if (currentNotification != null)
        {
            Destroy(currentNotification);
        }

        currentNotification = Instantiate(notificationPrefab, GetNotificationPosition(position), GetNotificationRotation(position));
        TextMeshProUGUI notificationTextComponent = currentNotification.GetComponentInChildren<TextMeshProUGUI>();

        if (notificationTextComponent != null)
        {
            notificationTextComponent.text = notificationText;
            SetNotificationType(notificationType);
        }
        else
        {
            Debug.LogError("Text component not found on currentNotification!");
        }

        currentNotification.SetActive(true);
        StartCoroutine(StartCountdown(duration));
    }

    private void SetNotificationType(string notificationType)
    {
        if (currentNotification != null)
        {

            Image notificationImage = currentNotification.transform
            .Find("Menu")
            .Find("Canvas")
            .Find("Main Panel")
            .gameObject.GetComponent<Image>();


            if (notificationImage != null)
            {

                switch (notificationType)
                {
                    case "Warning":
                        notificationImage.color = Color.yellow;
                        break;
                    case "Information":
                        notificationImage.color = Color.blue;
                        break;
                    case "Error":
                        notificationImage.color = Color.red;
                        break;
                    // Add more cases for different types of notifications
                    default:
                        notificationImage.color = Color.white;
                        break;
                }
            }
            else
            {
                Debug.LogError("Image component not found on currentNotification");
            }
        }
        else
        {
            Debug.LogError("currentNotification is null in SetNotificationType");
        }
    }





    // Coroutine to countdown and destroy the notification window
    private IEnumerator StartCountdown(float duration)
    {
        float remainingTime = duration;
        Text countdownText = currentNotification.GetComponentInChildren<Text>();

        while (remainingTime > 0)
        {
            countdownText.text = remainingTime.ToString("F0");
            yield return new WaitForSeconds(1f);
            remainingTime--;

        }

        if (currentNotification != null)
        {
            Destroy(currentNotification);
  
        }
    }

    private Quaternion GetNotificationRotation(string position)
    {
        float rotationForce = 20f;
        Quaternion rotation = Quaternion.identity;

        switch (position)
        {
            case "Left":
                rotation = userCamera.rotation * Quaternion.Euler(0f, -rotationForce, 0f);
                break;
            case "Right":
                rotation = userCamera.rotation * Quaternion.Euler(0f, rotationForce, 0f);
                break;
            case "Top":
                rotation = userCamera.rotation * Quaternion.Euler(-rotationForce, 0f, 0f);
                break;
            case "Bottom":
                rotation = userCamera.rotation * Quaternion.Euler(rotationForce, 0f, 0f);
                break;
            case "Center":
            default:
                break;

        }

        return rotation;
    }


    private Vector3 GetNotificationPosition(string position)
    {
        float distanceAhead = 1f;
        float heightAbove = 0f;
        float lateralOffset = 0.4f;

        Vector3 newPosition = userCamera.position + userCamera.forward * distanceAhead - userCamera.up * heightAbove;


        switch (position)
        {
            case "Left":
                newPosition -= userCamera.right * lateralOffset;
                break;
            case "Right":
                newPosition += userCamera.right * lateralOffset;
                break;
            case "Top":
                newPosition += userCamera.up * lateralOffset;
                break;
            case "Bottom":
                newPosition += -userCamera.up * lateralOffset;
                break;
            case "Center":
            default:
                break;

        }

        return newPosition;
    }

}