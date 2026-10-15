using UnityEditor;

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
public static class MeshSaverEditor
{
    [MenuItem("CONTEXT/MeshFilter/Save Mesh...")]
    public static void SaveMeshInPlace(MenuCommand menuCommand)
    {
        MeshFilter mf = menuCommand.context as MeshFilter;
        Mesh m = mf.sharedMesh;

        // Create an instance of a simple scriptable object
        DummyObject dummyObject = ScriptableObject.CreateInstance<DummyObject>();
        dummyObject.name = "My scriptable asset";

        // Create the scriptable object asset
        AssetDatabase.CreateAsset(dummyObject, "Assets/dummyObject.asset");

        // Get the path of the scriptable object asset
        string dummyObjectPath = AssetDatabase.GetAssetPath(dummyObject);

        // Add the material to the scriptable object asset
        AssetDatabase.AddObjectToAsset(m, dummyObjectPath);
        //AssetDatabase.AddObjectToAsset(mf.gameObject.GetComponent<MeshRenderer>().materials[0], dummyObjectPath);
        Material[] mats = mf.gameObject.GetComponent<MeshRenderer>().materials;
        for (int i = 0; i < mats.Length; i++)
        {
            Material mat = mats[i];
            
            SaveMaterialTextures(mat, "Assets/");
            //mat.GetTexture("_MainTex");
            AssetDatabase.AddObjectToAsset(mat, dummyObjectPath);
        }
        // Serializing the changes in memory to disk
        AssetDatabase.SaveAssets();

        // Print the path of the created asset
        Debug.Log(AssetDatabase.GetAssetPath(dummyObject));
    }
    private static void SaveMaterialTextures(Material material, string materialPath)
    {
        Shader shader = material.shader;
        int propertyCount = ShaderUtil.GetPropertyCount(shader);

        for (int i = 0; i < propertyCount; i++)
        {
            if (ShaderUtil.GetPropertyType(shader, i) == ShaderUtil.ShaderPropertyType.TexEnv)
            {
                string propertyName = ShaderUtil.GetPropertyName(shader, i);
                Texture texture = material.GetTexture(propertyName);

                if (texture != null)
                {
                    try {
                        AssetDatabase.CreateAsset(texture, "Assets/Mats/" + i + ".asset");
                        AssetDatabase.AddObjectToAsset(texture, "Assets/Mats/" + i + ".asset");
                    } catch (System.Exception e) {
                        Debug.LogError("Failed to save texture: " + e.Message);
                    }
                }
            }
        }
    }
}

// The DummyObject class used in the example above
public class DummyObject : ScriptableObject
{
    public string objectName = "dummy";
}

#endif