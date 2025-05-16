using System.Collections;
using UnityEngine;

namespace VaSiLi.Tutorial
{
    public class WelcomeStep : TutorialStep
    {

        public WelcomeStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>VaSiLiLab Tutorial</b></align></size>\n" +
            "Willkommen im Tutorial! In diesem werden wir uns die Grundlagen zur Bedienung des Programmes anschauen.\n" +
            "Rechts befindet sich eine Abbildung der benutzten VR-Controller. In jedem Schritt wird hier markiert welche Tasten benötigt werden.";
        }

        public override IEnumerator LoadStep()
        {
            yield return new WaitForSeconds(10);
            this.manager.NextStep();
        }

        public override void UnloadStep()
        {
        }
    }
}