using System.Collections;
using UnityEngine;


namespace VaSiLi.Tutorial
{
    public class MovementStep : TutorialStep
    {

        GameObject movementTarget;

        public MovementStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>Bewegung</b></align></size>\n" +
            "Um sich zu Bewegen existieren drei Möglichkeiten: \n - Reale Bewegungen \n - klassische Joystick Eingabe \n - Teleportation.\n\n" +
            "<b>Bewegen Sie sich mithilfe der Joystick Eingabe in den grünen Bereich.</b>";

            movementTarget = manager.transform.Find("TutorialObjects/MovementTarget").gameObject;
        }

        public override IEnumerator LoadStep()
        {
            movementTarget.SetActive(true);
            BoxCollider coll = movementTarget.GetComponent<BoxCollider>();

            bool finished = false;
            while (!finished)
            {
                foreach (Collider c in Physics.OverlapBox(movementTarget.transform.position, coll.bounds.extents))
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
            movementTarget.SetActive(false);
            manager.ResetPlayer();
        }
    }
}