using Oculus.Avatar2;
using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

public class MetaRegister : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Oculus.Platform.Core.LogMessages = true;
        OvrPlatformInit.InitializeOvrPlatform();
        //OvrAvatarLog.logLevel = 0;
        Debug.Log("Status: " + OvrPlatformInit.status);
        Debug.Log("Valid Token: " + OvrAvatarEntitlement.AccessTokenIsValid());
        Entitlements.IsUserEntitledToApplication().OnComplete(callbackMethod);
        
        void callbackMethod(Message msg)
        {
            if (!msg.IsError)
            {
                Debug.Log("User is entitled to the application.");

            }
            else
            {
                Debug.LogError("User is not entitled to the application: " + msg.GetError().Message);
            }
        }
        Users.GetLoggedInUser().OnComplete(callback: (Message<User> msg) =>
        {
            Debug.Log("Valid Token: " + OvrAvatarEntitlement.AccessTokenIsValid());
            if (msg.IsError)
            {
                Debug.LogError("Failed to get logged in user: " + msg.GetError().Message);
            }
            else
            {
                Debug.Log("User ID: " + msg.Data.ID);
                Debug.Log("UserName: " + msg.Data.DisplayName);
                Debug.Log("UserImageURL: " + msg.Data.ImageURL);
                // You can store the user information if needed
            }
        });
        if (OVRManager.eyeTrackedFoveatedRenderingSupported)
        {
            Debug.Log("Eye-tracked foveated rendering is supported and enabled.");
            OVRManager.eyeTrackedFoveatedRenderingEnabled = true;

        }
        else
        {
            Debug.Log("Eye-tracked foveated rendering is not supported.");
        }
        OVRPlugin.systemDisplayFrequency = 90.0f;
        OVRPlugin.foveatedRenderingLevel = OVRPlugin.FoveatedRenderingLevel.High;
        OVRManager.SetSpaceWarp(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
