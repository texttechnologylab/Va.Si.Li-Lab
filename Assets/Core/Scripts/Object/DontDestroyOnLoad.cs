using UnityEngine;

namespace VaSiLi.Object
{
    public class DontDestroyOnLoad : MonoBehaviour
    {
        // Start is called before the first frame update
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

    }
}