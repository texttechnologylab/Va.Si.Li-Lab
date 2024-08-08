using Ubiq.XR;

namespace VaSiLi.MetaAvatar
{
    public class OVRHandController : HandController
    {

        private OVRHand ovrhand;
        private XRUIRaycaster ui_ray;
        private TeleportRay teleport_ray;

        public enum Hand
        {
            None = OVRPlugin.Hand.None,
            HandLeft = OVRPlugin.Hand.HandLeft,
            HandRight = OVRPlugin.Hand.HandRight,
        }
        public Hand HandType;



        private void Awake()
        {
            // Overwrite 
            ovrhand = GetComponentInChildren<OVRHand>();
            ui_ray = GetComponentInChildren<XRUIRaycaster>();
            teleport_ray = GetComponentInChildren<TeleportRay>();
        }


        // Update is called once per frame
        void Update()
        {

            if (OVRInput.GetActiveController() != OVRInput.Controller.Hands)
            {
                ui_ray.transform.rotation = transform.rotation;

                //ui_ray.transform.SetPositionAndRotation(transform.position, transform.rotation);
                //teleport_ray.transform.SetPositionAndRotation(transform.position, transform.rotation);
                if (Left)
                {
                    TriggerState = OVRInput.Get(OVRInput.RawButton.LIndexTrigger);
                    TriggerValue = OVRInput.Get(OVRInput.RawAxis1D.LIndexTrigger);

                    GripState = OVRInput.Get(OVRInput.RawButton.LHandTrigger);
                    GripValue = OVRInput.Get(OVRInput.RawAxis1D.LHandTrigger);

                    PrimaryButtonState = OVRInput.Get(OVRInput.RawButton.X);

                    MenuButtonState = OVRInput.Get(OVRInput.RawButton.Y);

                    Joystick = OVRInput.Get(OVRInput.RawAxis2D.LThumbstick);

                }
                else if (Right)
                {
                    TriggerState = OVRInput.Get(OVRInput.RawButton.RIndexTrigger);
                    TriggerValue = OVRInput.Get(OVRInput.RawAxis1D.RIndexTrigger);

                    GripState = OVRInput.Get(OVRInput.RawButton.RHandTrigger);
                    GripValue = OVRInput.Get(OVRInput.RawAxis1D.RHandTrigger);

                    PrimaryButtonState = OVRInput.Get(OVRInput.RawButton.A);

                    MenuButtonState = OVRInput.Get(OVRInput.RawButton.B);

                    Joystick = OVRInput.Get(OVRInput.RawAxis2D.RThumbstick);

                }
                else
                {

                }

            }else if (OVRInput.GetActiveController() == OVRInput.Controller.Hands)
            {
                ui_ray.transform.rotation = ovrhand.PointerPose.rotation;

                if (ovrhand.IsTracked)
                {
                    //GripState = ovrhand.GetFingerIsPinching(OVRHand.HandFinger.Thumb);
                    //GripValue = ovrhand.GetFingerPinchStrength(OVRHand.HandFinger.Thumb);

                    TriggerState = ovrhand.GetFingerIsPinching(OVRHand.HandFinger.Index);
                    TriggerValue = ovrhand.GetFingerPinchStrength(OVRHand.HandFinger.Index);

                    //PrimaryButtonState = ovrhand.GetFingerIsPinching(OVRHand.HandFinger.Ring);
                }
            }


            TriggerPress.Update(TriggerState);
            GripPress.Update(GripState);
            PrimaryButtonPress.Update(PrimaryButtonState);
            MenuButtonPress.Update(MenuButtonState);
            JoystickSwipe.Update(Joystick.x);
        }

        public override bool Left
        {
            get
            {
                return HandType == Hand.HandLeft;
            }
        }

        public override bool Right
        {
            get
            {
                return HandType == Hand.HandRight;
            }
        }

        private void FixedUpdate()
        {

        }


    }
}