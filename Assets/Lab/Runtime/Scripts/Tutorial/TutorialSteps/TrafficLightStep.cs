using System.Collections;
using UnityEngine;
using System.Linq;
using VaSiLi.Misc;

namespace VaSiLi.Tutorial
{
    public class TrafficLightStep : TutorialStep
    {
        public TrafficLightStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>Gefühls-Ampel</b></align></size>\n" +
            "Sie können Ihren Gefühlszustand einem Außenstehenden über die Kontrollfläche an Ihrer rechten Hand mitteilen. Grün bedeutet, " +
            "es geht Ihnen gut, gelb, Sie fühlen sich unwohl und rot, dass Sie das Szenario abbrechen möchten.\n\n" +
            "<b>Drücken Sie, ähnlich wie in dem vorherigen Menü, einer der Auswahlmöglichkeiten.</b>";
        }

        public override IEnumerator LoadStep()
        {
            TrafficLight[] lights = GameObject.FindObjectsOfType<TrafficLight>();

            float alphaSum = lights.Aggregate<TrafficLight, float>(0f, (sum, light) => sum + light.buttonclick.image.color.a);
            float currentAlphaSum = alphaSum;

            while (alphaSum == currentAlphaSum)
            {
                currentAlphaSum = lights.Aggregate<TrafficLight, float>(0f, (sum, light) => sum + light.buttonclick.image.color.a);
                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(2);
            this.manager.NextStep();
        }

        public override void UnloadStep()
        {
            foreach (TrafficLight a in GameObject.FindObjectsOfType<TrafficLight>())
            {
                if (a.buttonclick.image.color.a > 0.5f)
                    a.HighlightTrafficLight();
            }

        }
    }
}