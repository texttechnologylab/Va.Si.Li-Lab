using UnityEngine;

namespace VaSiLi.Modules
{
    public class ActivateConditionally : MonoBehaviour
    {
        public Module module;

        public GameObject condtioned;

        void Awake()
        {
            condtioned.SetActive(module.enabled); 
        }
    }
}