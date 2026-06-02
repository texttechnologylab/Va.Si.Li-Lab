using UnityEngine;

namespace VaSiLi.Misc
{
    public class ToggleObject : MonoBehaviour
    {
        public void ToggleActive()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }

}