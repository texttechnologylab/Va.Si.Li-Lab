using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataBrowserInitializer : MonoBehaviour
{
    public GameObject DataBrowserPrefab;

    private GameObject DataBrowser;
    private Transform spawnRelativeTransform;

    public void LoadDataBrowser()
    {
        if (DataBrowser == null)
        {
            DataBrowser = Instantiate(DataBrowserPrefab);
            spawnRelativeTransform = DataBrowser.transform.Find("Relative UI Spawn");
            //DataBrowser.transform.SetParent(transform);
        }
        Request();
    }

    public void Request()
    {
        var cam = Camera.main.transform;

        DataBrowser.transform.position = cam.TransformPoint(spawnRelativeTransform.localPosition);
        DataBrowser.transform.rotation = cam.rotation * spawnRelativeTransform.localRotation;
        DataBrowser.SetActive(true);
    }

}
