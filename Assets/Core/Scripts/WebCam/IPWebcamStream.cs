using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace VaSiLi.WebCam
{
    public class IPWebcamStream : MonoBehaviour
    {

        public MeshRenderer frame;    //Mesh for displaying video
        public string ID;
        private string sourceURL = "http://vasililab.texttechnologylab.org:8081/cam/";
        private Texture2D texture;
        public Material error;
        private Stream stream;
        Task t1;
        private HttpWebRequest req;
        bool imgReady = false;
        byte[] imgBuffer = new byte[65536 * 4];

        void Start()
        {
            texture = new Texture2D(2, 2);
            // create HTTP request
            req = (HttpWebRequest)WebRequest.Create(sourceURL + ID);
            //Optional (if authorization is Digest)
            //req.Credentials = new NetworkCredential("username", "password");
            // get response
            req.BeginGetResponse(new AsyncCallback(FinishRequest), null);
        }

        void FinishRequest(IAsyncResult result)
        {
            try
            {
                WebResponse resp = req.EndGetResponse(result);
                stream = resp.GetResponseStream();
                t1 = new Task(() => GetFrame());
                t1.Start();
            }
            catch (Exception ex)
            {
                frame.material = error;
                Debug.LogError(ex);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (imgReady)
            {
                texture.LoadImage(imgBuffer);
                imgReady = false;
                frame.material.mainTexture = texture;
            }
        }

        void GetFrame()
        {
            Byte[] JpegData = new Byte[65536 * 4];

            while (true)
            {
                int bytesToRead = FindLength(stream);
                if (bytesToRead == -1)
                {
                    print("End of stream");
                    break;
                }

                int leftToRead = bytesToRead;

                while (leftToRead > 0)
                {
                    leftToRead -= stream.Read(JpegData, bytesToRead - leftToRead, leftToRead);
                }

                MemoryStream ms = new MemoryStream(JpegData, 0, bytesToRead, false, true);

                while (imgReady) { }

                imgBuffer = ms.GetBuffer();
                stream.ReadByte(); // CR after bytes
                stream.ReadByte(); // LF after bytes
                imgReady = true;
            }
        }

        int FindLength(Stream stream)
        {
            int b;
            string line = "";
            int result = -1;
            bool atEOL = false;

            while ((b = stream.ReadByte()) != -1)
            {
                if (b == 10) continue; // ignore LF char
                if (b == 13)
                { // CR
                    if (atEOL)
                    {  // two blank lines means end of header
                        stream.ReadByte(); // eat last LF
                        return result;
                    }
                    if (line.StartsWith("Content-Length:"))
                    {
                        result = Convert.ToInt32(line.Substring("Content-Length:".Length).Trim());
                    }
                    else
                    {
                        line = "";
                    }
                    atEOL = true;
                }
                else
                {
                    atEOL = false;
                    line += (char)b;
                }
            }
            return -1;
        }
    }
}
