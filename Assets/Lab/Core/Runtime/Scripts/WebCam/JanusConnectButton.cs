using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace VaSiLi.WebCam
{
    /// <summary>
    /// Toggle button that starts or stops the Janus WebRTC connection on <see cref="WebCamClient"/>.
    /// Wire the serialized fields in the Inspector and call <see cref="Toggle"/> from the Button's onClick.
    /// </summary>
    public class JanusConnectButton : MonoBehaviour
    {
        [Tooltip("The WebCamClient to connect / disconnect.")]
        public WebCamClient client;

        [Tooltip("Optional label on the button.")]
        public TMP_Text label;

        [Tooltip("Optional renderer whose material color changes with connection state.")]
        public Image buttonRenderer;

        public string connectText = "Connect";
        public string disconnectText = "Disconnect";

        public Color connectColor = Color.white;
        public Color connectedColor = Color.green;

        void Start()
        {
            UpdateVisuals();
        }

        /// <summary>Called by the Button onClick event.</summary>
        public void Toggle()
        {
            Debug.Log("[JanusConnectButton] Toggle connection");
            if (client == null) return;
            if (client.IsConnected)
                client.Disconnect();
            else
                client.Connect();

            // Visuals update after a short delay so IsConnected reflects the new state.
            Invoke(nameof(UpdateVisuals), 0.5f);
        }

        void UpdateVisuals()
        {
            bool connected = client != null && client.IsConnected;
            if (label != null) label.text = connected ? disconnectText : connectText;
            if (buttonRenderer != null)
                buttonRenderer.color = connected ? connectedColor : connectColor;
        }
    }
}
