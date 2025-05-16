using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
using Ubiq.Rooms;
using Ubiq.Samples;
using System.Linq;
using System.Xml.Linq;
using UnityEngine.Events;
using Ubiq.Spawning;
using VaSiLi.VAnnotator;

namespace VaSiLi.SceneManagement
{
    public class LocalFilesMenuManager : MonoBehaviour
    {
        public GameObject controlTemplate;
        public Transform controlsRoot;

        private HashSet<FileStruct> controlNames = new HashSet<FileStruct>();
        private List<LocalFilesMenuControl> controls = new List<LocalFilesMenuControl>();

        private FileStruct currentFile = new FileStruct() { filename = @"/", full_path = @"/", extension = "folder" };


        void Start()
        {
            UpdateControls(currentFile);
        }

        public void UpdateControls(FileStruct file)
        {
 
                controlNames.Clear();

                if (SystemInfo.operatingSystem.Contains("Windows") && file.full_path == @"/")
                {
                // 0 - 26 for a-z. hanged it to 10, so I exclude my mounted servers
                for (int i = 0; i < 10; i++)
                    {
                        string drive = ((char)('A' + i)).ToString();
                        if (Directory.Exists(drive + @":\"))
                        {
                            controlNames.Add(new FileStruct() { filename = drive + @":\", full_path = drive + @":\", extension = "folder" });
                        }
                    }
                }
                else
                {
                    if (file.full_path.Length > 3) {
                        string parentFolder = Directory.GetParent(file.full_path).FullName;
                        controlNames.Add(new FileStruct() { filename = "...", full_path = parentFolder, extension = "prev_folder" });
                    }else{
                        controlNames.Add(new FileStruct() { filename = "...", full_path = @"/", extension = "prev_folder" });

                    }
                    GetFilesInFolder(file.full_path);
                }

                UpdateAvailableData();
                currentFile = file;

        }


        private void GetFilesInFolder(string path)
        {
            DirectoryInfo info = new DirectoryInfo(path);
            DirectoryInfo[] dirInfo = info.GetDirectories();

            foreach (DirectoryInfo file in dirInfo)
            {
                if (!file.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    controlNames.Add(new FileStruct() { filename = file.Name, full_path = file.FullName, extension = "folder" });
                }
            }

            FileInfo[] fileInfo = info.GetFiles();
            foreach (FileInfo file in fileInfo)
            {
                if (!file.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    controlNames.Add(new FileStruct() { filename = file.Name, full_path = file.FullName, extension = file.Extension });
                }
            }
        }


        private void UpdateAvailableData()
        {
            int controlI = 0;

            foreach (FileStruct controleName in controlNames)
            {
                if (controls.Count <= controlI)
                {
                    controls.Add(InstantiateControl());
                }

                controls[controlI].Bind(controleName, this);
                controlI++;
            }

            while (controls.Count > controlI)
            {
                Destroy(controls[controlI].gameObject);
                controls.RemoveAt(controlI);
            }
        }

        private LocalFilesMenuControl InstantiateControl()
        {
            var go = GameObject.Instantiate(controlTemplate, controlsRoot);
            go.SetActive(true);
            return go.GetComponent<LocalFilesMenuControl>();
        }

        public void CreateEmptyVATextDocument()
        {
            GameObject txtPrefab = (GameObject)Resources.Load("VAInteractableObjects/VAInteractableTextObject");
            GameObject txtobj = NetworkSpawnManager.Find(this).SpawnWithPeerScope(txtPrefab);
            VAInteractableTextObject txt_obj = txtobj.GetComponent<VAInteractableTextObject>();
            StartCoroutine(txt_obj.LocalInit("", ""));
        }

    }
}

