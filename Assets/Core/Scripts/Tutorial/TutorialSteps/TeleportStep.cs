using System.Collections;
using UnityEngine;
using Ubiq.XR;

namespace VaSiLi.Tutorial
{
    public class TeleportStep : TutorialStep
    {

        GameObject movementTarget;
        XRPlayerController playerController;

        public TeleportStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>Teleportieren</b></align></size>\n" +
            "Die Teleportation wird über das Halten des Taste A aktiviert. Es erscheint ein Indikator zu welcher Position dies geschieht. " +
            "Nach erneutem loslassen wird die Teleportation durchgeführt. \n\n<b>Bewegen Sie sich erneut in den grünen Bereich.</b>";

            movementTarget = manager.transform.Find("TutorialObjects/MovementTarget").gameObject;
            playerController = GameObject.FindObjectOfType<XRPlayerController>();

        }

        float flySpeed;
        float movementSpeed;

        public override IEnumerator LoadStep()
        {
            movementTarget.SetActive(true);
            BoxCollider coll = movementTarget.GetComponent<BoxCollider>();
            Vector3 extends = coll.bounds.extents;
            coll.enabled = false;

            flySpeed = playerController.joystickFlySpeed;
            movementSpeed = playerController.GetComponent<DesktopPlayerController>().movementSpeed;
            playerController.joystickFlySpeed = 0;
            playerController.GetComponent<DesktopPlayerController>().movementSpeed = 0;

            yield return new WaitForSeconds(1);


            bool finished = false;
            while (!finished)
            {
                foreach (Collider c in Physics.OverlapBox(movementTarget.transform.position, extends))
                {
                    if (c.tag == "Player")
                    {
                        finished = true;
                        break;
                    }
                }
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(1);
            this.manager.NextStep();
        }

        public override void UnloadStep()
        {
            playerController.joystickFlySpeed = flySpeed;
            playerController.GetComponent<DesktopPlayerController>().movementSpeed = movementSpeed;

            manager.ResetPlayer();
            movementTarget.SetActive(false);
        }
    }
}