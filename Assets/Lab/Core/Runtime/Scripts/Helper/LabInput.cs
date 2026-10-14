using UnityEngine.InputSystem;

namespace VaSiLi.Helper
{
    /// <summary>
    /// Shared input shortcuts that work with controllers, hand tracking and a desktop keyboard.
    /// With hand tracking, OVRInput maps the right/left index pinch to Button.One/Button.Three
    /// and the left-hand menu gesture to Button.Start, which is what the hand fallbacks rely on.
    /// </summary>
    public static class LabInput
    {
        /// <summary>True when tracked hands (not controllers) are the active input.</summary>
        public static bool HandsActive => (OVRInput.GetActiveController() & OVRInput.Controller.Hands) != 0;

        /// <summary>A button, or right index pinch with hands, or the given key.</summary>
        public static bool PrimaryDown(Key key = Key.None) => OVRInput.GetDown(OVRInput.Button.One) || KeyDown(key);

        /// <summary>Release of <see cref="PrimaryDown"/>.</summary>
        public static bool PrimaryUp(Key key = Key.None) => OVRInput.GetUp(OVRInput.Button.One) || KeyUp(key);

        /// <summary>B button, or the left-hand menu gesture with hands, or Escape.</summary>
        public static bool BackDown()
        {
            if (HandsActive)
                return OVRInput.GetDown(OVRInput.Button.Start) || KeyDown(Key.Escape);
            return OVRInput.GetDown(OVRInput.Button.Two) || KeyDown(Key.Escape);
        }

        /// <summary>Left grip held, or left index pinch held with hands, or the given key.</summary>
        public static bool SecondaryHeld(Key key = Key.None)
        {
            // Button.Three is the X button on controllers, so only use it as the pinch when hands are tracked
            bool xr = HandsActive
                ? OVRInput.Get(OVRInput.Button.Three)
                : OVRInput.Get(OVRInput.Button.PrimaryHandTrigger);
            return xr || KeyHeld(key);
        }

        // Keyboard helpers, safe when no keyboard is connected (e.g. on the headset)
        public static bool KeyDown(Key key) => key != Key.None && Keyboard.current != null && Keyboard.current[key].wasPressedThisFrame;
        public static bool KeyUp(Key key) => key != Key.None && Keyboard.current != null && Keyboard.current[key].wasReleasedThisFrame;
        public static bool KeyHeld(Key key) => key != Key.None && Keyboard.current != null && Keyboard.current[key].isPressed;
    }
}
