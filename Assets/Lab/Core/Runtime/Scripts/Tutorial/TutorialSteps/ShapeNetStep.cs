using System.Collections;
using UnityEngine;
using Ubiq.Spawning;
using VaSiLi.Object;

namespace VaSiLi.Tutorial
{
    public class ShapeNetStep : TutorialStep
    {

        GameObject menu;
        GameObject target;
        NetworkSpawnManager spawnManager;
        bool flag = false;

        public ShapeNetStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>Gegenstände II</b></align></size>\n" +
            "In dem Szenario werden sie ein Menü finden aus welchem sie Objekte ihrer Wahl auswählen können\n" +
            "Wählen Sie ein Objekt aus und platzieren sie es in den grünen bereich.";

            menu = manager.transform.Find("TutorialObjects/DataBrowser").gameObject;
            spawnManager = NetworkSpawnManager.Find(manager);
            ObjectCollision.collided += OnCollision;
            target = manager.transform.Find("TutorialObjects/MovementTarget").gameObject;
        }

        public void OnCollision(GameObject collider)
        {
            flag = true;
        }

        public override IEnumerator LoadStep()
        {
            menu.SetActive(true);
            target.SetActive(true);
            target.GetComponent<BoxCollider>().enabled = true;
            while (!flag)
            {
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(2);
            this.manager.NextStep();
        }

        public override void UnloadStep()
        {
            menu.SetActive(false);
            target.SetActive(false);
            ObjectCollision.collided -= OnCollision;
            foreach (MultiGrabbable grabbable in spawnManager.GetComponentsInChildren<MultiGrabbable>())
            {
                GameObject.Destroy(grabbable.transform.parent.gameObject);
            }
            manager.ResetPlayer();
        }
    }
}