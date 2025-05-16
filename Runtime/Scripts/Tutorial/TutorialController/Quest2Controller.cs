using UnityEngine;

namespace VaSiLi.Tutorial
{
    public class Quest2Controller : MonoBehaviour, TutorialController
    {

        public string ControllerID
        {
            get
            {
                return "Oculus Touch Controller - Left";
            }
        }
        public GameObject GameObject
        {
            get
            {
                return this.gameObject;
            }
        }

        public Vector2[] WelcomePositions
        {
            get
            {
                return new Vector2[0];
            }
        }
        public Vector2[] MovementPositions
        {
            get
            {
                return new Vector2[] { new Vector2(1.9f, -1.45f) };
            }
        }
        public Vector2[] TeleportPositions
        {
            get
            {
                return new Vector2[] { new Vector2(-2.26f, -0.76f) };
            }
        }
        public Vector2[] MenuPositions
        {
            get
            {
                return new Vector2[] { new Vector2(-1.621f, 0.51f) };
            }
        }
        public Vector2[] ItemPositions
        {
            get
            {
                return new Vector2[] { new Vector2(-1.621f, 0.51f), new Vector2(-2.58f, 0.79f) };
            }
        }
        public Vector2[] DoorPositions
        {
            get
            {
                return new Vector2[] { new Vector2(-2.58f, 0.79f) };
            }
        }
        public Vector2[] EndPositions
        {
            get
            {
                return new Vector2[0];
            }
        }
    }
}