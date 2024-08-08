using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using System;

using System.Linq;
using System.Threading.Tasks;

namespace VaSiLi.WebCam
{
    public class WebCamClient : MonoBehaviour
    {
#if !UNITY_ANDROID
    public uint ID;

    WebCamTexture webCamTexture;
    NetworkStream stream;
    TcpClient client;

    Task t1;


    // Start is called before the first frame update
    void Start()
    {
        webCamTexture = new WebCamTexture(WebCamTexture.devices[0].name, 512, 512);
        webCamTexture.Play();

        client = new TcpClient("vasililab.texttechnologylab.org", 8089);
        stream = client.GetStream();

        byte[] idByte = BitConverter.GetBytes(ID).ToArray();

        stream.Write(idByte, 0, idByte.Length);
        t1 = new Task(() => StartStream());
        t1.Start();
    }

    byte[] bytes = new byte[0];
    bool ready = false;

    double frameTime = 1 / 30;

    void Update()
    {
        frameTime -= Time.deltaTime;
        if (ready || frameTime > 0)
        {
            return;
        }
        frameTime = 1 / 30;
        Texture2D tex = new Texture2D(webCamTexture.width, webCamTexture.height);
        tex.SetPixels(webCamTexture.GetPixels());
        tex.Apply();
        bytes = tex.EncodeToJPG();
        ready = true;
    }

    // Update is called once per frame
    void StartStream()
    {
        while (true)
        {
            if (!ready)
            {
                continue;
            }

            uint length = (uint)bytes.Length;
            byte[] response = BitConverter.GetBytes(length).Concat(bytes).ToArray();


            stream.Write(response, 0, response.Length);
            ready = false;
            t1.Wait(10);
        }


    }
    void OnDestroy()
    {
        stream.Close();
        client.Close();
    }
#endif
    }
}
