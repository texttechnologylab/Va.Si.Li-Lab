using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTest : MonoBehaviour
{
    public void Scenenwechseln(string name)
    {
        SceneManager.LoadScene(name);
    }
}
