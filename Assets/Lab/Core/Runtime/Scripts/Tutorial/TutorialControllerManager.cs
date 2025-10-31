using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace VaSiLi.Tutorial
{
    public class TutorialControllerManager : MonoBehaviour
    {
        private Transform MarkerTemplate;
        private Transform MarkerContainer;

        private TutorialController Controller;

        void Awake()
        {
            MarkerTemplate = transform.Find("MarkerTemplate");
            MarkerContainer = new GameObject("MarkerContainer").transform;
            MarkerContainer.SetParent(transform, false);
            InitController();
        }

        private void InitController()
        {
            TutorialController[] controllers = GetComponentsInChildren<TutorialController>();
            DetectController(controllers);

            foreach (TutorialController c in controllers)
            {
                if (c != Controller)
                {
                    Destroy(c.GameObject);
                }
            }
        }

        private void DetectController(TutorialController[] controllers)
        {
            var devices = new List<InputDevice>();
            InputDevices.GetDevices(devices);
            foreach (var device in devices)
            {
                foreach (TutorialController c in controllers)
                {
                    if (device.name == c.ControllerID)
                    {
                        Controller = c;
                        return;
                    }
                }
            }

            if (controllers.Length > 0)
                Controller = controllers[0];
        }

        private void HighlightInput(Vector2 position)
        {
            Transform marker = Instantiate(MarkerTemplate);
            marker.SetParent(MarkerContainer, false);
            marker.localPosition = new Vector3(position.x, 0, position.y);
            marker.GetComponent<MeshRenderer>().enabled = true;
        }

        private void RemoveHighlights()
        {
            foreach (Transform marker in MarkerContainer)
                Destroy(marker.gameObject);
        }

        public void HighlightStep(TutorialStep step)
        {
            Vector2[] highlights;

            switch (step)
            {
                case WelcomeStep s:
                    highlights = Controller.WelcomePositions;
                    break;
                case TeleportStep s:
                    highlights = Controller.TeleportPositions;
                    break;
                case MovementStep s:
                    highlights = Controller.MovementPositions;
                    break;
                case MenuStep s:
                    highlights = Controller.MenuPositions;
                    break;
                case TrafficLightStep s:
                    highlights = Controller.MenuPositions;
                    break;
                case ItemStep s:
                    highlights = Controller.ItemPositions;
                    break;
                case ShapeNetStep s:
                    highlights = Controller.ItemPositions;
                    break;    
                case DoorStep s:
                    highlights = Controller.DoorPositions;
                    break;
                case EndStep s:
                    highlights = Controller.EndPositions;
                    break;
                default:
                    highlights = new Vector2[0];
                    break;
            }

            RemoveHighlights();

            foreach (Vector2 highlight in highlights)
                HighlightInput(highlight);
        }
    }
}