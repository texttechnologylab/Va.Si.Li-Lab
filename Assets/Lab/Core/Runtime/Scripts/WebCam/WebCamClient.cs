using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using Unity.WebRTC;
using Meta.Net.NativeWebSocket;
using System.Threading.Tasks;
namespace VaSiLi.WebCam
{
    [Serializable]
    public struct Message<T>
    {
        public string janus;
        public string transaction;
        public long session_id;
        public long handle_id;
        public T body;
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
    [Serializable]
    public struct UpdateBody
    {
        public string request;
        public List<Subscribe> subscribe;

    }
    [Serializable]
    public struct Subscribe
    {
        public long feed;
        public string mid;
    }
    [Serializable]
    public struct EventMessage<T>
    {
        public string janus;
        public string transaction;
        public long session_id;
        public long sender;
        public PluginData<T> plugindata;
    }
    [Serializable]
    public struct JSEPEventMessage<T>
    {

        public string janus;
        public string transaction;
        public long session_id;
        public long sender;
        public PluginData<T> plugindata;
        public JSEP jsep;
    }

    [Serializable]
    public struct PluginData<T>
    {
        public string plugin;
        public T data;
    }
    [Serializable]
    public struct VideoRoomJoinedData
    {
        public string videoroom;
        public long room;
        public string description;
        public long id;
        public long private_id;
        public List<VideoRoomPublishers> publishers;
    }
    [Serializable]
    public struct VideoRoomPublishers
    {
        public long id;
        public string display;
        public string audio_codec;
        public string video_codec;
        public List<ContentStream> streams;
    }
    [Serializable]
    public struct ContentStream
    {
        public string type;
        public int mindex;
        public string mid;
        public string codec;
    }
    [Serializable]
    public struct JSEP
    {
        public string type;
        public string sdp;
    }

    [Serializable]
    public struct JSEPMessage<T>
    {
        public string janus;
        public string transaction;
        public long session_id;
        public long handle_id;
        public JSEP jsep;
        public T body;
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
    [Serializable]
    public struct ConfigureBody
    {
        public string request;
        public bool audio;
        public bool video;
    }

    [Serializable]
    public struct JoinBody
    {
        public string request;
        public long room;
        public string ptype;
        public string display;
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
    [Serializable]
    public struct StartBody
    {
        public string request;
        public long room;
    }
    [Serializable]
    public struct JoinBodySubscribe
    {
        public string request;
        public long room;
        public string ptype;
        public List<Subscribe> streams;
        public bool use_msid;
        public long private_id;
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
    [Serializable]
    public struct PostJanusAttach
    {
        public string janus;
        public string transaction;
        public string plugin;
        public string opaque_id;
        public long session_id;
        public PostJanusAttach(string transaction, string plugin, string opaque_id, long session_id)
        {
            janus = "attach";
            this.transaction = transaction;
            this.plugin = plugin;
            this.opaque_id = opaque_id;
            this.session_id = session_id;
        }
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
    [Serializable]
    public struct ResponseMin
    {
        public string janus;
        public string transaction;
    }
    [Serializable]
    public struct ResponseTest<T>
    {
        public string janus;
        public string transaction;
        public T data;
    }

    [Serializable]
    public struct SessionResponseData
    {
        public long id;
    }
    [Serializable]
    public struct TrickleMessage
    {
        public string janus;
        public string transaction;
        public long session_id;
        public long handle_id;
        public Candidate candidate;
        public override string ToString()
        {
            return JsonUtility.ToJson(this);
        }
    }
    public class CoroutineManager
    {
        public static void WaitCoroutine(IEnumerator func)
        {
            while (func.MoveNext())
            {
                if (func.Current != null)
                {
                    IEnumerator num;
                    try
                    {
                        num = (IEnumerator)func.Current;
                    }
                    catch (InvalidCastException)
                    {
                        if (func.Current.GetType() == typeof(WaitForSeconds))
                            Debug.LogWarning("Skipped call to WaitForSeconds. Use WaitForSecondsRealtime instead.");
                        return;  // Skip WaitForSeconds, WaitForEndOfFrame and WaitForFixedUpdate
                    }
                    WaitCoroutine(num);
                }
            }
        }
    }
    [Serializable]
    public struct Candidate
    {
        public string candidate;
        public string sdpMid;
        public int sdpMLineIndex;
        public bool completed;
    }



    public class WebCamClient : MonoBehaviour
    {
        private long session_id;
        private long handle_id;
        private long subscribe_handle_id;
        WebCamTexture webCamTexture;
        VaSiLi.WebSocket.WebSocket wsClient;
        //public AudioSource input;
        public AudioSource output;
        private long private_id;
        public MeshRenderer frame;
        public new Camera camera;
        public RenderTexture mirrorTexture;
        private Dictionary<long, List<VideoRoomPublishers>> publishers = new Dictionary<long, List<VideoRoomPublishers>>();

        async void Start()
        {
            //client.JoinRoom(12233, "Test123");

            StartCoroutine(WebRTC.Update());
            
            //LogFactory.Set(new CustomLoggerFactory());
            wsClient = new VaSiLi.WebSocket.WebSocket("ws://isengart.hucompute.org:8188", "janus-protocol");

            var rtcConf = new RTCConfiguration
            {
                iceServers = new RTCIceServer[] {
                 new RTCIceServer {
                    urls = new string[] {"turn:anduin.hucompute.org:3478"},
                    credential = "topsecretpassword",
                    username = "allie@oopcode.com"
                    }
                }
            };
            var rtcSubscribe = new RTCPeerConnection(ref rtcConf);
            var rtcPublish = new RTCPeerConnection(ref rtcConf);

            // Publish Audio
            var trackAudioIn = new AudioStreamTrack();
            var sendStream = new MediaStream();

            rtcPublish.AddTrack(trackAudioIn, sendStream);

            // Publish Video
            var gfxType = SystemInfo.graphicsDeviceType;
            var format = WebRTC.GetSupportedRenderTextureFormat(gfxType);

            var track = camera.CaptureStreamTrack(1280, 720);

            var sender = rtcPublish.AddTrack(track, sendStream);

            var parameters = sender.GetParameters();
            foreach (var encoding in parameters.encodings)
            {
                encoding.maxFramerate = 30;
                encoding.maxBitrate = 1000000;
                encoding.minBitrate = 250000;
            }
            sender.SetParameters(parameters);

            rtcSubscribe.OnIceCandidate = e =>
            {
                /*if (e.Candidate == null || e.Candidate.Contains("endOfCandidates"))
                {
                    var trickle = new TrickleMessage
                    {
                        janus = "trickle",
                        transaction = "trickle" + e.Candidate,
                        session_id = session_id,
                        handle_id = handle_id,
                        candidate = new Candidate
                        {
                            completed = true,
                        }
                    };
                    Debug.Log(trickle.ToString());
                    wsClient.SendText(trickle.ToString());
                }
                else
                {
                    var trickle = new TrickleMessage
                    {
                        janus = "trickle",
                        transaction = "trickle" + e.Candidate,
                        session_id = session_id,
                        handle_id = handle_id,
                        candidate = new Candidate
                        {
                            candidate = e.Candidate,
                            sdpMid = e.SdpMid,
                            sdpMLineIndex = (int)e.SdpMLineIndex
                        }
                    };
                    Debug.Log(trickle.ToString());
                    wsClient.SendText(trickle.ToString());
                }*/
            };

            rtcPublish.OnIceCandidate = e =>
            {
                /*Debug.Log("IceCandidate" + e + " \n  " + e.Candidate);
                var trickle = new TrickleMessage
                {
                    janus = "trickle",
                    transaction = "trickle" + e.Candidate,
                    session_id = session_id,
                    handle_id = handle_id,
                    candidate = new Candidate
                    {
                        candidate = e.Candidate,
                        sdpMid = e.SdpMid,
                        sdpMLineIndex = (int)e.SdpMLineIndex
                    }
                };
                Debug.Log(trickle.ToString());
                wsClient.SendText(trickle.ToString());*/


            };
            rtcPublish.OnIceConnectionChange += (state) =>
            {
                Debug.Log("ICE Connection: " + state);
            };

            var trackAudioOut = new AudioStreamTrack(output);
            var receiveStream = new MediaStream();
            receiveStream.OnAddTrack = (MediaStreamTrackEvent e) =>
            {
                if (e.Track is AudioStreamTrack track)
                {
                    // `AudioSource.SetTrack` is a extension method which is available 
                    // when using `Unity.WebRTC` namespace.
                    output.SetTrack(track);

                    // Please do not forget to turn on the `loop` flag.
                    output.loop = true;
                    output.Play();
                }
                else if (e.Track is VideoStreamTrack _track)
                {
                    _track.OnVideoReceived += texture =>
                    {
                        frame.material.mainTexture = texture;
                    };

                }
            };
            rtcSubscribe.OnTrack = (RTCTrackEvent e) =>
            {
                if (e.Track.Kind == TrackKind.Audio)
                {
                    Debug.Log("Audio track added.");
                    receiveStream.AddTrack(e.Track);
                }
                else if (e.Track.Kind == TrackKind.Video)
                {
                    Debug.Log("Video track added.");
                    receiveStream.AddTrack(e.Track);
                }
            };

            rtcSubscribe.OnNegotiationNeeded += () => Debug.Log("Negotiation Needed");
            rtcSubscribe.OnIceConnectionChange += (state) => Debug.Log("ICE Connection: " + state);


            rtcSubscribe.OnConnectionStateChange += (state) => Debug.Log("Webrtcup:  " + state);


            wsClient.OnOpen += async () =>
            {
                await wsClient.SendText("{\"janus\":\"create\",\"transaction\":\"SwS5ISwsnP9P\"}");
            };

            wsClient.OnError += (e) =>
            {
                Debug.Log("Connection Error! " + e);
            };


            wsClient.OnMessage += (data, offset, length) =>
            {
                Debug.Log("ReceivedMessage: " + Encoding.UTF8.GetString(data, offset, length));
                ResponseMin response = JsonUtility.FromJson<ResponseMin>(Encoding.UTF8.GetString(data, offset, length));
                switch (response.janus)
                {
                    case "success":
                        Debug.Log("Success");
                        ResponseTest<SessionResponseData> responseSession = JsonUtility.FromJson<ResponseTest<SessionResponseData>>(Encoding.UTF8.GetString(data, offset, length));
                        if (response.transaction == "AttachToRoom")
                        {
                            handle_id = responseSession.data.id;
                            JoinBody body = new JoinBody()
                            {
                                request = "join",
                                room = 1234,
                                ptype = "publisher",
                                display = "test-user"
                            };
                            Message<JoinBody> message = new Message<JoinBody>()
                            {
                                janus = "message",
                                transaction = "InitialJoin",
                                session_id = session_id,
                                handle_id = handle_id,
                                body = body
                            };
                            Debug.Log(message.ToString());
                            wsClient.SendText(message.ToString());
                        }
                        else if (response.transaction == "SubscribeAttach")
                        {
                            subscribe_handle_id = responseSession.data.id;
                            var subscribe = new List<Subscribe>();
                            foreach (var publisher in publishers[private_id])
                            {

                                foreach (var stream in publisher.streams)
                                {
                                    subscribe.Add(new Subscribe()
                                    {
                                        feed = publisher.id,
                                        mid = stream.mid
                                    });
                                }
                            }
                            JoinBodySubscribe subBody = new JoinBodySubscribe()
                            {
                                request = "join",
                                room = 1234,
                                ptype = "subscriber",
                                streams = subscribe,
                                use_msid = false,
                                private_id = private_id
                            };
                            Message<JoinBodySubscribe> message = new Message<JoinBodySubscribe>()
                            {
                                janus = "message",
                                transaction = "SubscribeJoin",
                                session_id = session_id,
                                handle_id = subscribe_handle_id,
                                body = subBody
                            };
                            Debug.Log(message.ToString());
                            wsClient.SendText(message.ToString());
                        }
                        else
                        {
                            session_id = responseSession.data.id;
                            Debug.Log(new PostJanusAttach("AttachToRoom", "janus.plugin.videoroom", "1234", session_id).ToString());
                            wsClient.SendText(new PostJanusAttach("AttachToRoom", "janus.plugin.videoroom", "1234", session_id).ToString());
                        }
                        break;
                    case "event":
                        EventMessage<VideoRoomJoinedData> eventMessage = JsonUtility.FromJson<EventMessage<VideoRoomJoinedData>>(Encoding.UTF8.GetString(data, offset, length));

                        if (eventMessage.transaction == "InitialJoin")
                        {
                            private_id = eventMessage.plugindata.data.private_id;
                            if (eventMessage.plugindata.data.publishers.Count > 0)
                            {
                                var subscribe = new List<Subscribe>();
                                publishers.Add(private_id, eventMessage.plugindata.data.publishers);
                                Debug.Log(new PostJanusAttach("SubscribeAttach", "janus.plugin.videoroom", "1234", session_id).ToString());
                                wsClient.SendText(new PostJanusAttach("SubscribeAttach", "janus.plugin.videoroom", "1234", session_id).ToString());
                                /*Message<UpdateBody> updateMessage = new Message<UpdateBody>()
                                {
                                    janus = "message",
                                    transaction = "SwS53",
                                    session_id = session_id,
                                    handle_id = handle_id,
                                    body = new UpdateBody()
                                    {
                                        request = "update",
                                        subscribe = subscribe
                                    }
                                };*/
                                //Debug.Log("SENDING" + updateMessage.ToString());
                                //wsClient.SendText(updateMessage.ToString());
                            }
                            else
                            {
                                /*JSEPMessage<ConfigureBody> jsepMessage = new JSEPMessage<ConfigureBody>()
                                {
                                    janus = "message",
                                    transaction = "Configuration",
                                    session_id = session_id,
                                    handle_id = handle_id,
                                    jsep = new JSEP()
                                    {
                                        type = "offer",
                                        sdp = sdpSend.Desc.sdp
                                    },
                                    body = new ConfigureBody()
                                    {
                                        request = "configure",
                                        audio = true,
                                        video = false
                                    }
                                };

                                Debug.Log(jsepMessage.ToString());
                                wsClient.SendText(jsepMessage.ToString());*/
                            }
                        }
                        else if (eventMessage.transaction == "SubscribeJoin")
                        {
                            Debug.Log("SubscribeJoin");
                            JSEPEventMessage<VideoRoomJoinedData> jSEPEventMessage = JsonUtility.FromJson<JSEPEventMessage<VideoRoomJoinedData>>(Encoding.UTF8.GetString(data, offset, length));

                            RTCSessionDescription sdpOffer = new RTCSessionDescription
                            {
                                type = RTCSdpType.Offer,
                                sdp = jSEPEventMessage.jsep.sdp
                            };

                            var result = rtcSubscribe.SetRemoteDescription(ref sdpOffer);
                            CoroutineManager.WaitCoroutine(result);

                            var answer = rtcSubscribe.CreateAnswer();
                            CoroutineManager.WaitCoroutine(answer);
                            RTCSessionDescription sdpAnswer = new RTCSessionDescription
                            {
                                type = RTCSdpType.Answer,
                                sdp = answer.Desc.sdp
                            };
                            var sdpReceive = rtcSubscribe.SetLocalDescription(ref sdpAnswer);

                            JSEPMessage<StartBody> startMessage = new JSEPMessage<StartBody>()
                            {
                                janus = "message",
                                transaction = "Start",
                                session_id = session_id,
                                handle_id = subscribe_handle_id,
                                body = new StartBody()
                                {
                                    request = "start",
                                    room = 1234
                                },
                                jsep = new JSEP()
                                {
                                    type = "answer",
                                    sdp = answer.Desc.sdp
                                }
                            };
                            Debug.Log(startMessage.ToString());
                            wsClient.SendText(startMessage.ToString());
                        }
                        else if (eventMessage.transaction == "Start")
                        {
                            var offer = rtcPublish.CreateOffer();
                            CoroutineManager.WaitCoroutine(offer);
                            RTCSessionDescription sdpSend = new RTCSessionDescription
                            {
                                type = RTCSdpType.Offer,
                                sdp = offer.Desc.sdp
                            };
                            var result = rtcPublish.SetLocalDescription(ref sdpSend);
                            CoroutineManager.WaitCoroutine(result);

                            JSEPMessage<ConfigureBody> jsepMessage = new JSEPMessage<ConfigureBody>()
                            {
                                janus = "message",
                                transaction = "Configuration",
                                session_id = session_id,
                                handle_id = handle_id,
                                jsep = new JSEP()
                                {
                                    type = "offer",
                                    sdp = sdpSend.sdp
                                },
                                body = new ConfigureBody()
                                {
                                    request = "configure",
                                    audio = true,
                                    video = true
                                }
                            };

                            Debug.Log(jsepMessage.ToString());
                            wsClient.SendText(jsepMessage.ToString());
                        }
                        else if (eventMessage.transaction == "Configuration")
                        {
                            JSEPEventMessage<VideoRoomJoinedData> jSEPEventMessage = JsonUtility.FromJson<JSEPEventMessage<VideoRoomJoinedData>>(Encoding.UTF8.GetString(data, offset, length));
                            RTCSessionDescription sdpAnswer = new RTCSessionDescription
                            {
                                type = RTCSdpType.Answer,
                                sdp = jSEPEventMessage.jsep.sdp
                            };
                            rtcPublish.SetRemoteDescription(ref sdpAnswer);
                        }
                        break;

                    case "timeout":
                        Debug.Log("Timeout");
                        break;
                }
            };
            wsClient.OnClose += (e) =>
            {
                Debug.Log("Connection closed123!: " + e);
            };

            await wsClient.Connect();
        }


        private float keepAliveTimer = 0f;

        void Update()
        {
            keepAliveTimer += Time.deltaTime;
            if (keepAliveTimer >= 10f)
            {
                keepAliveTimer = 0f;
                // Send keepAlive message every 10 seconds
                if (wsClient.State == WebSocketState.Open && session_id != 0)
                {
                    wsClient.SendText("{\"janus\":\"keepalive\",\"session_id\":" + session_id + ",\"transaction\":\"keepalive\"}");
                }
            }
        }

        async void OnDestroy()
        {
            await wsClient.Close();
        }

    }
}
