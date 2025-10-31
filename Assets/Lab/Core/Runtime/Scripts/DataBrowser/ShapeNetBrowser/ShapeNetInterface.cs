using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;
using static System.Environment;

public class ShapeNetInterface
{
    public const string WS = "http://shapenet.texttechnologylab.org/";
    public const string GET_OBJECT_ID = WS + "get?id=";

    public static string CACHE_DIR = Path.Combine(Application.persistentDataPath, "vasililab");

    public delegate void OnObjectLoaded(string filePath);
    public static IEnumerator DownloadModel(string objid, OnObjectLoaded onLoaded)
    {
        string _path = Path.Combine(CACHE_DIR, "objects", objid);
        if (Directory.Exists(_path))
        {
            onLoaded(_path);
            yield break;
        }
        else
        {
            Directory.CreateDirectory(_path);
        }

        UnityWebRequest request = UnityWebRequest.Get(GET_OBJECT_ID + objid);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError)
            Debug.Log(request.error);
        else
        {
            string zipFile = _path + ".zip";
            FileStream fileStream = new FileStream(zipFile, FileMode.Create);
            fileStream.Write(request.downloadHandler.data, 0, request.downloadHandler.data.Length);
            fileStream.Close();

            Directory.CreateDirectory(_path);
            System.IO.Compression.ZipFile.ExtractToDirectory(zipFile, _path);
            File.Delete(zipFile);
            onLoaded(_path);
        }
        yield break;
    }
}
