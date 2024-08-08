using System.Collections;
using UnityEngine;
using Ubiq.Spawning;
using VaSiLi.Object.Drawing;

namespace VaSiLi.Tutorial
{
    public class ItemStep : TutorialStep
    {

        Pen pen;

        public ItemStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>Gegenstände</b></align></size>\n" +
            "In den Szenarios befinden sich verschiedene interaktive Gegenstände wie beispielsweise dieser Stift.\n\n" +
            "<b>Greifen Sie den Stift per Halten der Greifen Taste. Malen Sie nun per Trigger Taste in den Raum und lassen Sie anschließend den Stift wieder los.</b>";

            pen = manager.transform.Find("TutorialObjects/Pen").gameObject.GetComponent<Pen>();
        }

        public override IEnumerator LoadStep()
        {
            pen.gameObject.SetActive(true);
            NetworkSpawnManager spawnManager = GameObject.FindObjectOfType<NetworkSpawnManager>();
            while (true)
            {
                if (!pen.Owner && spawnManager.GetComponentInChildren<LineTrail>() != null)
                    break;
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(2);
            this.manager.NextStep();
        }

        public override void UnloadStep()
        {
            pen.gameObject.SetActive(false);
            NetworkSpawnManager spawnManager = GameObject.FindObjectOfType<NetworkSpawnManager>();

            foreach (LineTrail lt in spawnManager.GetComponentsInChildren<LineTrail>())
            {
                GameObject.Destroy(lt.gameObject);
            }
        }
    }
}