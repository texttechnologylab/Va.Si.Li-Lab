using Oculus.Platform;
using Oculus.Platform.Models;
using UnityEngine;

public class MetaRegister : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //Oculus.Platform.Core.LogMessages = true;

        // Core.AsyncInitialize() is asynchronous: it returns immediately and the
        // platform is NOT ready yet. Every platform call (entitlement check,
        // GetLoggedInUser, access token) must happen AFTER init completes, otherwise
        // you get "This function requires an initialized Oculus Platform" followed by
        // a NullReferenceException. So gate all of it behind the init callback.
        try
        {
            Core.AsyncInitialize().OnComplete(OnPlatformInitialized);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to start Oculus Platform initialization: " + e.Message);
        }

        ConfigurePerformance();
    }

    private void OnPlatformInitialized(Message<PlatformInitialize> msg)
    {
        if (msg.IsError || msg.Data.Result != PlatformInitializeResult.Success)
        {
            var reason = msg.IsError ? msg.GetError().Message : msg.Data.Result.ToString();
            Debug.LogError("Oculus Platform failed to initialize: " + reason);
            return;
        }

        Debug.Log("Oculus Platform initialized.");

        Entitlements.IsUserEntitledToApplication().OnComplete(entitlementMsg =>
        {
            if (!entitlementMsg.IsError)
            {
                Debug.Log("User is entitled to the application.");
            }
            else
            {
                Debug.LogError("User is not entitled to the application: " + entitlementMsg.GetError().Message);
            }
        });

        Users.GetLoggedInUser().OnComplete((Message<User> msg) =>
        {
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
    }

    private void ConfigurePerformance()
    {
        if (OVRManager.eyeTrackedFoveatedRenderingSupported)
        {
            Debug.Log("Eye-tracked foveated rendering is supported and enabled.");
            OVRManager.eyeTrackedFoveatedRenderingEnabled = true;

        }
        else
        {
            Debug.Log("Eye-tracked foveated rendering is not supported.");
        }
        OVRPlugin.suggestedCpuPerfLevel = OVRPlugin.ProcessorPerformanceLevel.SustainedHigh;
        Debug.Log("Suggested CPU Performance Level: " + OVRPlugin.suggestedCpuPerfLevel);
        OVRPlugin.systemDisplayFrequency = 72.0f;
        OVRPlugin.foveatedRenderingLevel = OVRPlugin.FoveatedRenderingLevel.High;
        
        //OVRManager.SetSpaceWarp(true);
    }

    // Update is called once per frame
    void Update()
    {

    }
}
