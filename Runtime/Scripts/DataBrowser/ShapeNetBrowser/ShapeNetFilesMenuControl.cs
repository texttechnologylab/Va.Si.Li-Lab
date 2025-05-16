using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using Ubiq.Spawning;
using VaSiLi.VAnnotator;

public class ShapeNetFilesMenuControl : MonoBehaviour
{

    public Text fileNameText;
    public Text fileDescriptionText;
    public RawImage fileLogoImage;
    public Button button;

    public GameObject shapeNetPrefab;

    JToken shapeNetObj;


    public void Bind(JToken shapeNetObj)
    {
        this.shapeNetObj = shapeNetObj;
        if(shapeNetObj["name"] == null)
        {
            fileNameText.text = "Nameless";
        }
        else
        {
            fileNameText.text = shapeNetObj["name"].ToString();
        }
        if (shapeNetObj["hypercategory"] == null)
        {
            fileDescriptionText.text = shapeNetObj["categories"].ToString().Replace("\n", "");
        }
        else
        {
            fileDescriptionText.text = shapeNetObj["hypercategory"].ToString().Replace("\n", "");
        }
        button.name = $"SpawnButton_{shapeNetObj["id"]}";
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            SpawnShapeNetObj();
        });
    }


    private void SpawnShapeNetObj()
    {
        GameObject imgobj = NetworkSpawnManager.Find(this).SpawnWithPeerScope(shapeNetPrefab);
        VAInteractable3DObject img_obj = imgobj.GetComponent<VAInteractable3DObject>();
        
        StartCoroutine(img_obj.Init(shapeNetObj));
        if (img_obj is VAInteractable3DObjectMulti)
        {
            img_obj.disableGrab = true;
        }
    }


}
