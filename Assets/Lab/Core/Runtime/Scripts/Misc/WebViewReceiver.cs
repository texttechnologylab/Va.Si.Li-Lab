using UnityEngine;

public class WebViewReceiver : MonoBehaviour
{
    public void OnJsResult(string message)
    {
        Debug.Log("✅ JS returned via unitySendMessage: " + message);
    }
}
