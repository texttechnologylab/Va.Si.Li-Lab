using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VaSiLi.Object.Drawing;
using VaSiLi.SceneManagement;
using VaSiLi.VAnnotator;
using Ubiq.Spawning;

public struct FileStruct
{
    public string filename;
    public string full_path;
    public string extension;
}

public class LocalFilesMenuControl : MonoBehaviour
{
    public Text fileNameText;
    public RawImage fileLogoImage;
    public Button button;

    public GameObject txtPrefab;
    public GameObject imgPrefab;

    private LocalFilesMenuManager manager;

    


    private static Dictionary<string, Texture2D> fileLogoTextures = new Dictionary<string, Texture2D>();

    private void Awake()
    {
        //https://fonts.google.com/icons
        //Fill, weight=7000
        if (fileLogoTextures == null || fileLogoTextures.Count == 0)
        {
            fileLogoTextures.Add("folder", Resources.Load<Texture2D>("FileExtensionLogos/folder_FILL1_wght700_GRAD0_opsz48"));
            fileLogoTextures.Add("prev_folder", Resources.Load<Texture2D>("FileExtensionLogos/folder_FILL1_wght700_GRAD0_opsz48"));
            fileLogoTextures.Add(".txt", Resources.Load<Texture2D>("FileExtensionLogos/description_FILL1_wght700_GRAD0_opsz48"));
            fileLogoTextures.Add(".json", Resources.Load<Texture2D>("FileExtensionLogos/description_FILL1_wght700_GRAD0_opsz48"));
            fileLogoTextures.Add(".jpg", Resources.Load<Texture2D>("FileExtensionLogos/image_FILL1_wght700_GRAD0_opsz48"));
            fileLogoTextures.Add(".png", Resources.Load<Texture2D>("FileExtensionLogos/image_FILL1_wght700_GRAD0_opsz48"));
            fileLogoTextures.Add(".pdf", Resources.Load<Texture2D>("FileExtensionLogos/task_FILL1_wght700_GRAD0_opsz48"));
            fileLogoTextures.Add(".obj", Resources.Load<Texture2D>("FileExtensionLogos/deployed_code_FILL0_wght400_GRAD0_opsz48"));
            fileLogoTextures.Add(".stl", Resources.Load<Texture2D>("FileExtensionLogos/deployed_code_FILL0_wght400_GRAD0_opsz48"));

            // Add more file extensions and textures as needed
        }
    }

    public void Bind(FileStruct file, LocalFilesMenuManager manager)
    {
        this.manager = manager;
        fileNameText.text = file.filename;
        if (fileLogoTextures.ContainsKey(file.extension))
        {
            fileLogoImage.texture = fileLogoTextures[file.extension];
        }

        button.onClick.AddListener(() =>
        {
            if (file.extension.Contains("folder"))
            {
                this.manager.UpdateControls(file);
            }
            else
            {
                //GameObject oriented_obj = null;
                switch(file.extension)
                {
                    case ".obj":
                        //GameObject obj = ObjectLoader.LoadObject(file.full_path, "");
                        //oriented_obj = ObjectLoader.Reorientate_Obj(obj, Vector3.up, Vector3.forward, 0.01f);
                        //GameObject vaobj = Instantiate(spawningGameObject, new Vector3(0, 1f, 0), Quaternion.identity);
                        //obj.transform.SetParent(vaobj.transform, false);
                        //vaobj.transform.SetParent(spawningTransform, false);
                        //obj.AddComponent<VAInteractableObject>();
                        break;
                    case ".txt":
                    case ".json":
                        string file_text = File.ReadAllText(file.full_path);
                        GameObject txtobj = NetworkSpawnManager.Find(this).SpawnWithPeerScope(txtPrefab);
                        VAInteractableTextObject txt_obj = txtobj.GetComponent<VAInteractableTextObject>();
                        StartCoroutine(txt_obj.LocalInit(file.filename, file_text));
                        break;

                    case ".jpg":
                    case ".jpeg":
                    case ".png":
                        byte[] img = File.ReadAllBytes(file.full_path);
                        GameObject imgobj = NetworkSpawnManager.Find(this).SpawnWithPeerScope(imgPrefab);
                        VAInteractableImageObject img_obj = imgobj.GetComponent<VAInteractableImageObject>();
                        StartCoroutine(img_obj.Init(file.filename, img));
                        break;
                    default:
                        Debug.Log("File extension not supported ....");
                        break;
                }
                Debug.Log("A file was selected ....");
            }
        });

    }

}
