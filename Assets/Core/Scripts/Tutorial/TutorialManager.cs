using UnityEngine;
using VaSiLi.Misc;
using VaSiLi.SceneManagement;
using TMPro;
//using Unity.Plastic.Newtonsoft.Json.Linq;
using Newtonsoft.Json.Linq;

namespace VaSiLi.Tutorial
{
    public class TutorialManager : MonoBehaviour
    {
        private TutorialStep[] steps;
        private TutorialControllerManager controllerManager;

        private Transform player;
        private Vector3 playerStartPos;
        private Quaternion playerStartRot;

        private int stepIndex = -1;

        private JToken jsonTexts;
        private LanguageManager languageManager;

        // Start is called before the first frame update
        void Start()
        {
            FetchTexts();

            steps = new TutorialStep[]{
            new WelcomeStep(this),     
            new MovementStep(this),
            new TeleportStep(this),
            new MenuStep(this),
            new TrafficLightStep(this),
            new ItemStep(this),
            new ShapeNetStep(this),
            new DoorStep(this),
            new EndStep(this)
            };

            foreach (Transform child in transform.Find("TutorialObjects"))
            {
                child.gameObject.SetActive(false);
            }

            player = GameObject.FindGameObjectWithTag("Player").transform;
            playerStartPos = player.position;
            playerStartRot = player.rotation;

            LanguageManager.languageChanged += DisplayText;

            controllerManager = GetComponentInChildren<TutorialControllerManager>();
            NextStep();
        }

        public void GoToStartScene()
        {
            SceneManager.SetScene(null);
        }

        public void ResetPlayer()
        {
            player.rotation = playerStartRot;
            player.position = playerStartPos;
        }

        public void NextStep()
        {
            stepIndex++;
            if (stepIndex >= steps.Length)
                return;

            if (stepIndex > 0)
                steps[stepIndex - 1].UnloadStep();


            TutorialStep step = steps[stepIndex];

            DisplayText(LanguageManager.SelectedLanguage);

            controllerManager.HighlightStep(step);

            StartCoroutine(step.LoadStep());
        }

        private void DisplayText(LanguageManager.Language _lang)
        {
            if (jsonTexts == null) return;

            JToken stepData = jsonTexts[LanguageManager.SelectedLanguage.ToString()][steps[stepIndex].GetType().Name];

            string title = stepData["title"].ToString();
            string task = stepData["task"].ToString();
            string description = stepData["description"].ToString();

            string text = "<size=120%><align=\"center\"><b>" + title + "</b></align></size>\n" + description + "\n\n<b>" + task + "</b>";

            GetComponentInChildren<TextMeshPro>().text = text;
        }

        private async void FetchTexts()
        {
            var response = await VaSiLi.Networking.JsonRequest.GetRequest("http://api.vasililab.texttechnologylab.org/infos");
            var content = await response.Content.ReadAsStringAsync();

            JToken result = JToken.Parse(content);
            foreach (JToken child in result["result"])
            {
                if (child["mode"].ToString() == "tutorial")
                {
                    jsonTexts = child["description"];
                    DisplayText(LanguageManager.SelectedLanguage);
                }
            }
        }
    }
}