using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class TextureLoader : MonoBehaviour
{

    public static Material LoadMaterialFile(string path, Dictionary<string, string> files)
    {
        //https://docs.unity3d.com/Manual/MaterialsAccessingViaScript.html


        Material m;
        if (files.ContainsKey("specularity")) //contrary to metallic (e.g. for skin)
        {
            m = new Material(Shader.Find("Custom/SpecularCutout"));
            m.EnableKeyword("_SPECGLOSSMAP");
            m.SetTexture("_SpecGlossMap", LoadPNG(path + files["specularity"]));
        }
        else
        {
            m = new Material(Shader.Find("Custom/StandardCutout")); ;
        }

        if (files.ContainsKey("basecolor")) //Main Texture
        {
            Debug.Log("basecolor is included");

            Texture2D mainT = LoadPNG(path + files["basecolor"]);
            if (files.ContainsKey("opacity")) //Special Transparence
            {
                TransferAlpha(mainT, LoadPNG(path + files["opacity"]));
            }
            m.mainTexture = mainT;
        }


        if (files.ContainsKey("normal"))
        {
            m.EnableKeyword("_NORMALMAP");
            m.SetTexture("_BumpMap", LoadPNG(path + files["normal"]));
        }

        if(files.ContainsKey("roughness") & files.ContainsKey("metallic"))
        {
            Texture2D met = LoadPNG(path + files["metallic"]);
            Texture2D rou = LoadPNG(path + files["roughness"]);
            TransferAlpha(met, rou);

            m.EnableKeyword("_METALLICGLOSSMAP");
            m.SetTexture("_MetallicGlossMap", met);
        }
        else if (files.ContainsKey("metallic"))
        {
            m.EnableKeyword("_METALLICGLOSSMAP");
            m.SetTexture("_MetallicGlossMap", LoadPNG(path + files["metallic"]));
        }
        else if (files.ContainsKey("roughness"))
        {
            
            Texture2D rou = LoadPNG(path + files["roughness"]);
            Texture2D met = new Texture2D(rou.width, rou.height);

            TransferAlpha(met, rou);
            m.EnableKeyword("_METALLICGLOSSMAP");
            m.SetTexture("_MetallicGlossMap", met);
        }

        if (files.ContainsKey("height"))
        {
            m.EnableKeyword("_PARALLAXMAP");
            m.SetTexture("_ParallaxMap", LoadPNG(path + files["height"]));
        }

        if (files.ContainsKey("ambientocclusion"))
        {
            //m.EnableKeyword("");
            m.SetTexture("_OcclusionMap", LoadPNG(path + files["ambientocclusion"]));
        }

        if (files.ContainsKey("mask"))
        {
            m.EnableKeyword("_DETAIL_MULX2");
            m.SetTexture("_DetailMask", LoadPNG(path + files["ambientocclusion"]));
        }


        if (files.ContainsKey("emission"))
        {
            //Dragon Scales 001
            m.EnableKeyword("_EMISSION");
            m.SetTexture("_EmissionMap", LoadPNG(path + files["ambientocclusion"]));
            
        }


        return m;
    }


    public static Texture2D LoadPNG(string filePath)
    {
        Debug.Log("load: " + filePath);
        Texture2D tex = null;
        byte[] fileData;

        if (File.Exists(filePath))
        {
            fileData = File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
        }
        else
        {
            Debug.Log("Could not find: " + filePath);
        }
        return tex;
    }


    private static void TransferAlpha(Texture2D A, Texture2D B)
    {
        if(A==null || B == null)
        {
            return;
        }
        //https://answers.unity.com/questions/902538/change-alpha-channel-of-a-texture-in-code.html
        Color pixA, pixB;
        for (int i = 0; i < A.width; i++)
        {
            for (int j = 0; j < A.height; j++)
            {
                // Read out pixel value at that location in both textures
                pixA = A.GetPixel(i, j);
                pixB = B.GetPixel(i, j);
                // Copy the alpha channel from B's pixel and assign it to A's pixel
                pixA.a = pixB.grayscale;
                A.SetPixel(i, j, pixA);
            }
        }
        // Apply the results
        A.Apply();
    }
}
