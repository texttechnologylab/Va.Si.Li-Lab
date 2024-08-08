using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.Networking;
using Ubiq.Messaging;
using Ubiq.XR;
using Ubiq.Rooms;

public class MediaScreen : MonoBehaviour, IUseable
{
    NetworkContext context;
    RoomClient roomClient;

    VideoPlayer player;



    private string[] mediaURLs = {
        "https://www.texttechnologylab.org/wp-content/uploads/2018/08/LogoTTLabWhite.png",
        "watch?v=BBUg8mHT0iI",
        "watch?v=FTV4ofAs-PI",
        "https://www.texttechnologylab.org/wp-content/uploads/2020/08/VAnnotatoR.png",
        "watch?v=etmjra_CXOc",
        "https://pbs.twimg.com/media/FiqS9LlWQAALTGv?format=jpg&name=large",
        "https://www.texttechnologylab.org/wp-content/uploads/2020/08/VAnnotatoR_gross-960x464.jpg"
    };

    private const string YOUTUBE_DL_SERVER = "https://unity-youtube-dl-server.herokuapp.com/{0}&cli=yt-dlp";

    private int _index;
     public int Index {
        get {
            return _index;
        }

        private set {
            if(_index == value) return; 

            _index = value; 
            loadMediaContent();     
        }
    }

    // Start is called before the first frame update
    void Start()
    { 
        context = NetworkScene.Register(this);
        roomClient = GameObject.Find("Social Network Scene").GetComponent<RoomClient>();
        roomClient.OnJoinedRoom.AddListener(RequestState);
        player = GetComponent<VideoPlayer>();
        loadMediaContent();
    }  

    public void LoadNext(){
        Index = nfmod(Index+1, mediaURLs.Length);
        publishState(true);
    }

    public void LoadPrevious(){
        Index = nfmod(Index-1, mediaURLs.Length);
        publishState(true);
    }

    void IUseable.Use(Hand controller)
    {
        if(player.enabled){
            if(player.isPlaying)
                player.Pause();
            else
                player.Play();
        }

        publishState(false);
    }

    void IUseable.UnUse(Hand controller)
    {
    }

    void RequestState(IRoom room){
        context.SendJson(new Message()
        {
            requestState = true
        });
    }

    private void publishState(bool startVideo){
        context.SendJson(new Message()
        {
            requestState = false,
            index = Index,
            isPlaying = startVideo || (player.enabled && player.isPlaying),
            time = player.time,
        });
    }

    private void loadMediaContent(){
        string url = mediaURLs[Index];

        if(url.StartsWith("watch")){
            loadYoutubeVideo(url);
        }
        else
            loadImage(url);
    }

    private void loadImage(string url){
        player.enabled = false;
        StartCoroutine(downloadImage(url));
    }

    private IEnumerator downloadImage(string MediaUrl)
    {   
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(MediaUrl);
        yield return request.SendWebRequest();
        UnityWebRequest.Result result = request.result;
        if(result == UnityWebRequest.Result.ConnectionError) 
            Debug.Log(request.error);
        else{
            Texture2D tex = ((DownloadHandlerTexture) request.downloadHandler).texture;
            GetComponent<MeshRenderer>().material.mainTexture = tex;
            fitTexture(tex);
        }
    } 

    private void fitTexture(Texture2D tex){
        float aspectRatioTex = tex.width/tex.height;
   
        if(aspectRatioTex < 1)
            gameObject.transform.localScale = new Vector3(0.100000001f,3.41000009f,3.41000009f*(tex.width/(float)tex.height));
        else
        {
            float height = 7.42999983f*(tex.height/(float)tex.width);
            float width = 7.42999983f;

            if( height > 3.41000009f){
                width = 3.41000009f/height * 7.42999983f;
                height = 3.41000009f; 
            }

            gameObject.transform.localScale = new Vector3(0.100000001f,height,width);
        }
            
    } 

    private void loadYoutubeVideo(string url){
        gameObject.transform.localScale = new Vector3(0.100000001f,3.41000009f,7.42999983f);
        GetComponent<MeshRenderer>().material.mainTexture = null;
        player.enabled = true;
        player.url = string.Format(YOUTUBE_DL_SERVER, url);
        player.Play();
    }

    private int nfmod(float a,float b)
    {
        return (int)(a - b * Mathf.Floor(a / b));
    }

    private struct Message
    { 
        public bool requestState;
        public int index;
        public bool isPlaying;
        public double time;
    } 

    public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
    {
        var m = message.FromJson<Message>();

        if(m.requestState){
            publishState(false);
            return;
        }

        Index = m.index;

        if(player.enabled){
            player.time = m.time;
            if(m.isPlaying)
                player.Play();
            else
                player.Pause();
        }
    }
}
