using System.Collections;
using UnityEngine;
using Ubiq.Spawning;
using VaSiLi.Object;

namespace VaSiLi.Tutorial
{
    public class DoorStep : TutorialStep
    {

        GameObject doorObject;
        GenericGrabbable door;

        public DoorStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>Gegenstände</b></align></size>\n" +
            "In den Szenarios befinden sich unter anderem auch Türen.\n\n" +
            "<b>Treten sie zur Tür und greifen Sie den Türgriff per Halten der Greifen Taste. Bewegen Sie nun ihre Hand oder sich selbst um die Tür zu öffnen.</b>";
            doorObject = manager.transform.Find("TutorialObjects/HingedDoor").gameObject;
            door = manager.transform.Find("TutorialObjects/HingedDoor/Door/Handle").gameObject.GetComponent<GenericGrabbable>();
        }

        public override IEnumerator LoadStep()
        {
            doorObject.gameObject.SetActive(true);
            NetworkSpawnManager spawnManager = GameObject.FindObjectOfType<NetworkSpawnManager>();
            while (true)
            {
                if (door.Owner && door.targetTransform.rotation.eulerAngles.y <= 95)
                    break;
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(2);
            this.manager.NextStep();
        }

        public override void UnloadStep()
        {
            doorObject.gameObject.SetActive(false);
            NetworkSpawnManager spawnManager = GameObject.FindObjectOfType<NetworkSpawnManager>();

        }
    }
}