using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Dummiesman;

public class ObjectLoader : MonoBehaviour
{

    public static GameObject LoadObject(string objpath, string mtlpath)
    {

        if (!File.Exists(objpath))
        {
            Debug.Log(objpath + " obj doesn't exist.");
        }
        else
        {

            if (!File.Exists(mtlpath))
            {
                Debug.Log(mtlpath + " mtl doesn't exist.");
            }

            OBJLoader loader = new OBJLoader();
            GameObject obj = loader.Load(objpath, mtlpath);
            return obj;
        }
        return null;
    }

    public static GameObject Reorientate_Obj(GameObject obj, Vector3 up, Vector3 front, float scale)
    {
        obj.transform.localScale = new Vector3(scale, scale, scale);
        Debug.Log(obj.name);

        Quaternion rotation_up = Quaternion.FromToRotation(up, Vector3.up);
        obj.transform.rotation = rotation_up;

        front = rotation_up * front;
        float w = Vector3.Angle(front, Vector3.forward);
        obj.transform.RotateAround(obj.transform.position, Vector3.up, w);

        //Reposition Obj
        //Renderer obj_renderer = obj.GetComponentInChildren<Renderer>();
        //Vector3 render_position_med = -obj_renderer.bounds.center;
        //obj.transform.localPosition = render_position_med;

        Renderer obj_renderer = obj.GetComponentInChildren<Renderer>();
        Vector3 render_position_min = -obj_renderer.bounds.min;
        Vector3 render_position_med = -obj_renderer.bounds.center;
        //obj.transform.localPosition = new Vector3(render_position_med.x, render_position_min.y, render_position_med.z);
        obj.transform.localPosition = new Vector3(render_position_med.x, render_position_med.y, render_position_med.z);


        GameObject oriented_obj = new GameObject(obj.name+"_center");
        obj.transform.SetParent(oriented_obj.transform, false);
        return oriented_obj;
 
    }


}
