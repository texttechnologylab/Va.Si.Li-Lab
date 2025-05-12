using System.Collections.Generic;
using UnityEngine;
using System.IO;
using VaSiLi.Misc;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Ubiq.Samples.Social;

namespace VaSiLi.SceneManagement
{
    [RequireComponent(typeof(PagePanel))]
    public class ShapeNetFilesMenuManager : MonoBehaviour
    {
        public GameObject controlTemplate;
        public Transform controlsRoot;
        public Text searchBar;
        public int entriesPerPage = 5;

        private List<JToken> shapenetObjects;
        private List<ShapeNetFilesMenuControl> controls = new List<ShapeNetFilesMenuControl>();
        private PagePanel pagePanel;

        private HashSet<string> availableShapeNetObjects = new HashSet<string>();
        //private FileStruct currentFile = new FileStruct() { filename = @"/", full_path = @"/", extension = "folder" };

        private void Awake()
        {
            pagePanel = GetComponent<PagePanel>();
        }

        private void Start()
        {
            //InitAvailableShapeNetObjects();
        }

        private async void InitAvailableShapeNetObjects()
        {
            string request = "http://shapenet.texttechnologylab.org/loadedobjects";
            var response = await Networking.JsonRequest.GetRequest(request);
            var content = await response.Content.ReadAsStringAsync();

            JToken result = JToken.Parse(content);
            foreach (JToken obj in result["ShapeNetObj"])
            {
                availableShapeNetObjects.Add(obj["id"].ToString());
            }
        }

        private void OnEnable()
        {
            searchBar.text = "";
            Search();
            pagePanel.onPageChanged.AddListener(PagePanel_OnPageChanged);
        }

        private void OnDisable()
        {
            if (pagePanel)
            {
                pagePanel.onPageChanged.RemoveListener(PagePanel_OnPageChanged);
            }
        }

        private void PagePanel_OnPageChanged(int page, int pageCount)
        {
            UpdateOptions();
        }

        private void UpdateOptions()
        {
            var optionCount = Mathf.Min(shapenetObjects.Count, 100);
            pagePanel.SetPageCount(optionCount / entriesPerPage);
            int controlI = 0;
            for (int optionI = 0; optionI < optionCount; controlI++, optionI++)
            {
                if (controls.Count <= controlI)
                {
                    controls.Add(InstantiateControl());
                }
                JToken shapeNetObj = shapenetObjects[optionI];
                controls[controlI].Bind(shapeNetObj);
            }

            while (controls.Count > controlI)
            {
                Destroy(controls[controlI].gameObject);
                controls.RemoveAt(controlI);
            }
            
            var startOptionI = pagePanel.page * entriesPerPage;
            var endOptionI = pagePanel.page * entriesPerPage + entriesPerPage - 1;

            for (int i = 0; i < controls.Count; i++)
            {
                controls[i].gameObject.SetActive(i >= startOptionI && i <= endOptionI);
            }
        }

        public void Search()
        {
            string search_term = searchBar.text;
            FetchTTlabShapeNet(search_term);
        }

        private string GetTTlabShapeNetRequest(string search)
        {
            //dataset: 3dw = shapenet code, wss = shapenet sem
            //format: json, csv
            return $"http://shapenet.texttechnologylab.org/search?search={search}";
        }

        private string GetShapeNetRequest(string search, string dataset="wss", string format="json", int rows=100, bool indent=true)
        {
            //dataset: 3dw = shapenet code, wss = shapenet sem
            //format: json, csv

            if (string.IsNullOrEmpty(search))
                return $"https://shapenet.org/solr/models3d/select?q=source%3A{dataset}&rows={rows}&wt={format}&indent={indent}";

            return $"https://shapenet.org/solr/models3d/select?q={search}+AND+source%3A{dataset}&rows={rows}&wt={format}&indent={indent}"; 

        }

        private async void FetchTTlabShapeNet(string search)
        {
            string request = GetTTlabShapeNetRequest(search);
            var response = await Networking.JsonRequest.GetRequest(request);
            var content = await response.Content.ReadAsStringAsync();
            JToken result = JToken.Parse(content);
            JArray resultlist = (JArray)result["results"];
            List<JToken> filtered_resultlist = resultlist.ToObject<List<JToken>>();

            if (filtered_resultlist.Count == 0)
            {
                Debug.Log("No results found");
                searchBar.text = "No results found";
                return;
            }
            shapenetObjects = filtered_resultlist;

            var optionCount = shapenetObjects.Count;
            pagePanel.SetPageCount(optionCount / entriesPerPage);
            pagePanel.SetPage(0);
            UpdateOptions();

        }

        private async void FetchShapeNet(string search)
        {
            string request = GetShapeNetRequest(search);
            var response = await Networking.JsonRequest.GetRequest(request);
            var content = await response.Content.ReadAsStringAsync();

            JToken result = JToken.Parse(content);
            JArray resultlist = (JArray)result["response"]["docs"];
            List<JToken> filtered_resultlist = resultlist.ToObject<List<JToken>>();
            //filtered_resultlist = filtered_resultlist.FindAll(x => availableShapeNetObjects.Contains(x["id"].ToString()));
            filtered_resultlist = filtered_resultlist.FindAll(x => x["hypercategory"] != null);

            if (filtered_resultlist.Count == 0)
            {
                Debug.Log("No results found");
                searchBar.text = "No results found";
                return;
            }
            shapenetObjects = filtered_resultlist;

            var optionCount = shapenetObjects.Count;
            pagePanel.SetPageCount(optionCount / entriesPerPage);
            pagePanel.SetPage(0);
            UpdateOptions();

        }

        private ShapeNetFilesMenuControl InstantiateControl()
        {
            var go = Instantiate(controlTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<ShapeNetFilesMenuControl>();
        }

    }
}

