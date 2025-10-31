using System.Threading.Tasks;
using Ubiq.Messaging;
using UnityEngine;
using UnityEngine.UI;

public class CanvasSelect : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private NetworkContext context;
    public GameObject canvas0;
    public GameObject canvas1;
    public GameObject canvas2;
    public GameObject canvas3;
    public Button button;
    public float timeout = 180f; // Default timeout in seconds
    private struct Message
    {
        public int panel;
    }
    void Start()
    {
        context = NetworkScene.Register(this);
        canvas0.SetActive(true);
        button.onClick.AddListener(() => NextPanel());
    }
    // Timeout in seconds
    Task buttonTimeout(float timeout)
    {
        button.interactable = false;
        button.enabled = false;
        return Task.Delay((int)(timeout * 1000)).ContinueWith(_ => { button.interactable = true;  button.enabled = true; }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    public void NextPanel(bool sendUpdate = true)
    {
        if (sendUpdate)
        {
            // Send a message to update the panel
            context.SendJson(new Message { panel = 0 });
        }
        if (canvas3.activeSelf)
        {
            return;
        }
        if (canvas0.activeSelf)
        {
            canvas0.SetActive(false);
            canvas1.SetActive(true);
        }
        else if (canvas1.activeSelf)
        {
            canvas1.SetActive(false);
            canvas2.SetActive(true);
        }
        else if (canvas2.activeSelf)
        {
            canvas2.SetActive(false);
            canvas3.SetActive(true);
        }

        buttonTimeout(timeout);
    }

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        NextPanel(false);
    }
}
