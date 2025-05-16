using System.Collections;
using UnityEngine;
using static Oculus.Avatar2.OvrAvatarEntity;

public class ScreenshotScript : MonoBehaviour
{
    public SampleAvatarEntity avatar;
    public Camera screenshotCamera;
    public string outputPath;

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(ScreenshotMaker());
    }

    private IEnumerator ScreenshotMaker()
    {
        yield return new WaitForSeconds(4.0f); //Wait until init the default char. We could already use that one, but ...
        for (int i = 0; i < 32; i++) {
            yield return new WaitForSeconds(1.0f); //So the screenshot tool has aanought time to shoot the photo.
            
            // Reset Avatar or it gets overlayerd. I don't know, why that is not default ...
            avatar.Teardown();
            //CreateEntity() is normally protected. Changed it to public. 
            //TODO!!!
            //avatar.CreateEntity();

            // Load the actual avatar preset.
            avatar.LoadPreset(i);

            //Wait till loaded ...
            while(avatar.CurrentState < AvatarState.DefaultAvatar)
            {
                // After that time, they start to smile :)
                yield return new WaitForSeconds(3.0f);
            }

            // Make the actual screenshot.
            MakeScreenshot(i);
        }

        yield return new WaitForSeconds(5.0f);

        Application.Quit();
    }


    private void MakeScreenshot(int index)
    {
        string path = $"{Application.dataPath}/{outputPath}/PresetAvatar_{index}_Thumbnail.png";
        Debug.Log("Make Screenshot: " + path);
        ScreenCapture.CaptureScreenshot(path);
    }
}
