using System.Collections;
using UnityEngine;

namespace VaSiLi.Tutorial
{
    public class MenuStep : TutorialStep
    {

        GameObject menu;

        public MenuStep(TutorialManager manager) : base(manager)
        {
        }

        protected override void init()
        {
            this.DescriptionRTF = "<size=120%><align=\"center\"><b>Menü Bedienung</b></align></size>\n" +
            "Es existieren verschiedene 2D Menüs. Diese können bedient werden, indem mit der rechten Hand auf sie gezeigt wird. " +
            "Anschließend erscheint ein Cursor dessen Eingabe über den Trigger bestätigt werden kann.\n\n <b>Klicken Sie den auf Button \"Name\".</b>";

            menu = manager.transform.Find("TutorialObjects/Menu Manager").gameObject;
        }

        public override IEnumerator LoadStep()
        {
            menu.SetActive(true);

            while (true)
            {
                Transform t = menu.transform.Find("Menu/Canvas/Main Panel/Set Avatar Panel");
                if (t.gameObject != null && t.gameObject.activeInHierarchy)
                    break;

                yield return new WaitForEndOfFrame();
            }

            while (true)
            {
                Transform t = menu.transform.Find("Menu/Canvas/Main Panel/Menu Panel");
                if (t.gameObject != null && t.gameObject.activeInHierarchy)
                    break;

                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(2);
            this.manager.NextStep();
        }

        public override void UnloadStep()
        {
            menu.SetActive(false);
        }
    }
}