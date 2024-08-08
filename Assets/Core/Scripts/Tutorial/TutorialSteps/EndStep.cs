using System.Collections;
using UnityEngine;

namespace VaSiLi.Tutorial
{
    public class EndStep : TutorialStep
    {

        public EndStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>VaSiLiLab Tutorial</b></align></size>\n" +
            "Vielen Dank für das abschließen des Tutorials. Als nächstes werden wir in die Szenarioauswahl wechseln." +
            "Hier kann ein Szenario über das Menü \"Join Room\" beigetreten und anschließend eine Rolle gewählt werden.";
        }

        public override IEnumerator LoadStep()
        {
            yield return new WaitForSeconds(15);
            this.manager.GoToStartScene();
        }

        public override void UnloadStep()
        {
        }
    }
}