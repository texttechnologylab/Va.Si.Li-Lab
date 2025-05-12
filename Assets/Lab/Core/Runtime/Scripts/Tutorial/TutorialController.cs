using UnityEngine;

namespace VaSiLi.Tutorial
{
    public interface TutorialController
    {
        public string ControllerID
        {
            get;
        }

        public GameObject GameObject
        {
            get;
        }

        public Vector2[] WelcomePositions
        {
            get;
        }
        public Vector2[] MovementPositions
        {
            get;
        }
        public Vector2[] TeleportPositions
        {
            get;
        }
        public Vector2[] MenuPositions
        {
            get;
        }
        public Vector2[] ItemPositions
        {
            get;
        }
        public Vector2[] DoorPositions
        {
            get;
        }
        public Vector2[] EndPositions
        {
            get;
        }
    }
}