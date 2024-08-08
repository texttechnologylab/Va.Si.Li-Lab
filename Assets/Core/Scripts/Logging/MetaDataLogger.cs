using System.Collections.Generic;
using UnityEngine;
using Ubiq.Rooms;
using Ubiq.Voip;
using System;
using Ubiq.XR;
using VaSiLi.SceneManagement;
using Ubiq.Messaging;
using SIPSorceryMedia.Abstractions;
using System.Threading.Tasks;
using Ubiq.Voip.Implementations.Dotnet;
using VaSiLi.Networking;
using System.Security.Cryptography;
using System.Text;
using Ubiq.Spawning;
using System.Linq;

namespace VaSiLi.Logging
{
    /// <summary>
    /// Class to aid in logging various messages to the api
    /// All messages are first queued to be sent and then handled asynchronisly 
    /// http://audio.vasililab.texttechnologylab.org/
    /// </summary>
    public class MetaDataLogger : MonoBehaviour
    {
        public string url;
        public RoomClient client;
        public VoipPeerConnectionManager voipPeer;
        public Transform player;
        public Transform playerCamera;
        public Transform leftHand;
        public Transform rightHand;
        public int sendPositionSizeThreshold;
        public int positionTrackingRate;
        private string playerId;
        private IDotnetVoipSource input;
        private string currentArea;
        private int messageCount = 0;
        private int positionCount = 0;
        private List<float> audioData = new List<float>();
        private IRoom currentRoom;
        private PlayerMessage currentPlayerMessage;
        private Queue<ObjectMessage> objectMessages = new Queue<ObjectMessage>();
        private Queue<PlayerMessage> playerMessages = new Queue<PlayerMessage>();
        private Queue<ButtonMessage> buttonMessages = new Queue<ButtonMessage>();
        private Queue<SpawnMessage> spawnMessages = new Queue<SpawnMessage>();
        private Queue<LogMessage> logMessages = new Queue<LogMessage>();

        public static Queue<MiscLogMessage> miscMessages = new Queue<MiscLogMessage>();

        private Task trackPositions;
        private Task messageWorker;

        private enum ObjectTrackType
        {
            UNCHANGED,
            STARTED,
            STOPPED,
        }
        private bool shouldTrack = true;
        void Start()
        {
            client.OnJoinedRoom.AddListener(JoinedRoom);
            // TODO: Support WebGL
            NetworkSpawnManager.Find(this).OnSpawned.AddListener(OnObjectSpawn);
            NetworkSpawnManager.Find(this).OnDespawned.AddListener(OnObjectDeSpawn);
            UseableObjectUser.onUse += OnUse;
            UseableObjectUser.onUnUse += OnUnUse;
            GraspableObjectGrasper.onGrasp += OnGrasp;
            GraspableObjectGrasper.onRelease += OnRelease;
            RoleManager.roleChanged += ChangedRole;
            RoleManager.roleChanged += LogSelectedMetaAvatar;
            LevelManager.levelStatus += ChangedLevel;
            DesktopUIRaycaster.buttonClicks += OnButtonClicked;
            XRUIRaycaster.buttonClicks += OnButtonClicked;
            LogToScreen.logFileData += LogFilesToDB;
            PositionTagUpdater.positonUpdate += UpdatePositionTag;
            messageWorker = StartMessageWorker();

            StartTrackers();
        }

        void OnEnable()
        {
            if (messageWorker != null && messageWorker.Status == TaskStatus.RanToCompletion)
            {
                Debug.LogWarning("Recovered message worker after disabling");
                messageWorker = StartMessageWorker();
            }
        }

        void Update()
        {
            if (shouldTrack && trackPositions != null && trackPositions.Status == TaskStatus.Faulted)
            {
                Debug.LogWarning("Player tracking task failed. Recovering...");
                trackPositions = TrackPlayerPosition();
            }
            if (messageWorker != null && messageWorker.Status == TaskStatus.Faulted)
            {
                Debug.LogWarning("Message worker task failed. Recovering...");
                messageWorker = StartMessageWorker();
            }
            if (input == null)
            {
                var go = GameObject.Find("Microphone Dotnet Voip Source");
                if (go != null)
                {
                    Debug.Log("Input resolved");
                    input = go.GetComponent<IDotnetVoipSource>();
                    if (input is MicrophoneDotnetVoipSource)
                    {
                        Debug.Log("Valid Input");
                        ((MicrophoneDotnetVoipSource)input).OnAudioPcmSample += OnAudioEncoded;
                    }
                }
            }

        }

        void UpdatePositionTag(string tag)
        {
            Debug.Log("Area update: " + tag);
            currentArea = tag;
        }

        void OnAudioEncoded(AudioSamplingRatesEnum durationRtpUnits, float[] bytes)
        {
            if (shouldTrack)
                audioData.AddRange(bytes);
        }

        void ChangedRole(ApiRole? role)
        {
            if (RoleManager.CurrentRole?.mode != Mode.Player)
                shouldTrack = false;
            else
            {
                PlayerRoleLogin message = new PlayerRoleLogin()
                {
                    playerId = playerId,
                    localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                    role = RoleManager.CurrentRole?.name,
                    messageId = messageCount,
                };
                var result = JsonRequest.PostRequest(url + "/playerRoleLogIn", message).ContinueWith(a =>
                {
                    if (a.Result.StatusCode != System.Net.HttpStatusCode.Accepted)
                    {
                        Debug.LogError("Failed to send message: " + a.Result.StatusCode);
                    }
                }
                );
            }
        }

        void ChangedLevel(int lvl, LevelManager.Status status)
        {
            if (currentRoom == null)
                return;

            LevelStatus message = new LevelStatus()
            {
                playerId = playerId,
                roomId = currentRoom.UUID,
                sceneName = currentRoom.Name,
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                levelID = lvl,
                levelStatus = status.ToString(),
            };
            
            var result = JsonRequest.PostRequest(url + "/levelChange", message).ContinueWith(a =>
            {
                if (a.Result.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    Debug.LogError("Failed to send message: " + a.Result.StatusCode);
                }
            }
            );
        }

        void OnGrasp(Hand controller, MonoBehaviour grasped)
        {
            TransmitObjectData(grasped, controller, ObjectTrackType.UNCHANGED, ObjectTrackType.STARTED);
        }

        void OnRelease(Hand controller, MonoBehaviour grasped)
        {
            TransmitObjectData(grasped, controller, ObjectTrackType.UNCHANGED, ObjectTrackType.STOPPED);
        }

        void OnUse(Hand controller, MonoBehaviour used)
        {
            TransmitObjectData(used, controller, ObjectTrackType.STARTED, ObjectTrackType.UNCHANGED);
        }

        void OnUnUse(Hand controller, MonoBehaviour used)
        {
            TransmitObjectData(used, controller, ObjectTrackType.STOPPED, ObjectTrackType.UNCHANGED);
        }

        void OnButtonClicked(GameObject buttonClicked)
        {
            TransmitButtonData(buttonClicked);
        }

        void LogFilesToDB(string logString, LogType type, string stackTrace)
        {
            if (type != LogType.Log)
                TransmitLogData(logString, type, stackTrace);
        }

        void OnObjectSpawn(GameObject gameObject, IRoom room, IPeer peer, NetworkSpawnOrigin origin)
        {
            SpawnMessage message = new SpawnMessage()
            {
                name = gameObject?.name,
                children = gameObject?.GetComponentsInChildren<Transform>().Select(go => go.name).ToArray(),
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                room = room?.UUID,
                peer = peer?.uuid,
                origin = (int)origin
            };
            spawnMessages.Enqueue(message);
        }

        void OnObjectDeSpawn(GameObject gameObject, IRoom room, IPeer peer)
        {
            SpawnMessage message = new SpawnMessage()
            {
                name = gameObject?.name,
                children = gameObject?.GetComponentsInChildren<Transform>().Select(go => go.name).ToArray(),
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                room = room?.UUID,
                peer = peer?.uuid,
                origin = -1
            };
            spawnMessages.Enqueue(message);
        }


        void JoinedRoom(IRoom room)
        {
            if (room.Name == "")
            {
                currentRoom = null;
                return;
            }
            else if (room.Name == "LEFT_ROOM")
            {
                currentRoom = null;
                audioData.Clear();
                shouldTrack = false;
                return;
            }
            currentRoom = room;
            audioData.Clear();
            using (var sha1 = new SHA1Managed())
            {
                playerId = new NetworkId(sha1.ComputeHash(Encoding.UTF8.GetBytes(currentRoom.UUID + currentRoom.Name + client.Me.uuid)), 0).ToString();
            }

            PlayerLogin message = new PlayerLogin()
            {
                playerId = playerId,
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                roomId = currentRoom.UUID,
                sceneName = currentRoom.Name,
                clientId = client.Me.uuid,
                messageId = messageCount,
            };
            var result = JsonRequest.PostRequest(url + "/playerLogIn", message).ContinueWith(a =>
            {
                if (a.Result.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    Debug.LogError("Failed to send message: " + a.Result.StatusCode);
                }
            }
            );


            StartTrackers();
        }
        private void StartTrackers()
        {
            shouldTrack = true;
            currentPlayerMessage = CreatePlayerMessage();
            trackPositions = TrackPlayerPosition();
        }


        private OVRPlugin.FaceState _currentFaceState;
        private OVRPlugin.EyeGazesState _currentEyeGazesState;

        private OVRPlugin.HandState _currentLeftHandState;
        private OVRPlugin.HandState _currentRightHandState;


        private async Task TrackPlayerPosition()
        {

            while (shouldTrack)
            {
                OVRPlugin.GetFaceState(OVRPlugin.Step.Render, -1, ref _currentFaceState);
                OVRPlugin.GetEyeGazesState(OVRPlugin.Step.Render, -1, ref _currentEyeGazesState);
                OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandLeft, ref _currentLeftHandState);
                OVRPlugin.GetHandState(OVRPlugin.Step.Render, OVRPlugin.Hand.HandRight, ref _currentRightHandState);
                
                currentPlayerMessage.count.Add(positionCount++);
                currentPlayerMessage.body.positions.Add(new PositionMessage { x = player.position.x, y = player.position.y, z = player.position.z });
                currentPlayerMessage.body.rotations.Add(new RotationMessage { x = player.rotation.x, y = player.rotation.y, z = player.rotation.z, w = player.rotation.w });
                currentPlayerMessage.body.cameraPositions.Add(new PositionMessage { x = playerCamera.position.x, y = playerCamera.position.y, z = playerCamera.position.z });
                currentPlayerMessage.body.cameraRotations.Add(new RotationMessage { x = playerCamera.rotation.x, y = playerCamera.rotation.y, z = playerCamera.rotation.z, w = playerCamera.rotation.w });
                currentPlayerMessage.leftHand.positions.Add(new PositionMessage { x = leftHand.position.x, y = leftHand.position.y, z = leftHand.position.z });
                currentPlayerMessage.leftHand.rotations.Add(new RotationMessage { x = leftHand.rotation.x, y = leftHand.rotation.y, z = leftHand.rotation.z, w = leftHand.rotation.w });
                currentPlayerMessage.rightHand.positions.Add(new PositionMessage { x = rightHand.position.x, y = rightHand.position.y, z = rightHand.position.z });
                currentPlayerMessage.rightHand.rotations.Add(new RotationMessage { x = rightHand.rotation.x, y = rightHand.rotation.y, z = rightHand.rotation.z, w = rightHand.rotation.w });
                currentPlayerMessage.metaMessage.faceStates.Add(_currentFaceState);
                currentPlayerMessage.metaMessage.eyeGazesStates.Add(_currentEyeGazesState);
                currentPlayerMessage.metaMessage.leftHandStates.Add(_currentLeftHandState);
                currentPlayerMessage.metaMessage.rightHandStates.Add(_currentRightHandState);

                if (currentPlayerMessage.body.positions.Count > sendPositionSizeThreshold)
                {
                    FinalizeAndEnqueuePlayerMessage();
                    currentPlayerMessage = CreatePlayerMessage();
                }
                //result_skel_LH = OVRPlugin.GetSkeleton(OVRPlugin.SkeletonType.HandLeft, out skel_LH);
                //result_skel_RH = OVRPlugin.GetSkeleton(OVRPlugin.SkeletonType.HandRight, out skel_RH);

                //result_mesh_LH = OVRPlugin.GetMesh(OVRPlugin.MeshType.HandLeft, out mesh_LH);
                //result_mesh_RH = OVRPlugin.GetMesh(OVRPlugin.MeshType.HandRight, out mesh_RH);

                //Debug.Log("!!!1" + skel_LH.ToJson());
                //Debug.Log("!!!2" + skel_RH.ToJson());
                //Debug.Log("!!!3" + mesh_LH.ToJson());
                //Debug.Log("!!!4" + mesh_RH.ToJson());

                await Task.Delay(positionTrackingRate);
            }

        }

        private void FinalizeAndEnqueuePlayerMessage()
        {
            currentPlayerMessage.audioData.base64 = Convert.ToBase64String(FloatPcmToWav(audioData.ToArray()));
            currentPlayerMessage.localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
            playerMessages.Enqueue(currentPlayerMessage);
            ClearData();
        }

        /// <summary>
        /// Aids in transmitting a message to the api
        /// </summary>
        /// <param name="url">The traget URL</param>
        /// <param name="message">The message to transmit</param>
        /// <param name="queue">The queue to insert the message on failure</param>
        /// <typeparam name="T">The valid serializable message type</typeparam>
        /// <returns>A Queue of messages that failed to be transmitted</returns>
        private async Task<bool> TransmitMessage<T>(string url, T message, Queue<T> queue)
        {
            try
            {
                var result = await JsonRequest.PostRequest(url, message);
                if (result.StatusCode != System.Net.HttpStatusCode.Accepted)
                {
                    Debug.LogWarning("Failed to send message: " + result.StatusCode);
                    queue.Enqueue(message);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                queue.Enqueue(message);
                Debug.LogError(ex);
            }
            return false;
        }

        private async Task StartMessageWorker()
        {
            while (this.enabled)
            {
                var tmpPlayerMessages = new Queue<PlayerMessage>();
                var tmpObjectMessages = new Queue<ObjectMessage>();
                var tmpButtonMessages = new Queue<ButtonMessage>();
                var tmpLogMessages = new Queue<LogMessage>();
                var tmpSpawnMessages = new Queue<SpawnMessage>();
                var tmpMiscMessages = new Queue<MiscLogMessage>();

                // TODO: TransmitMessages should return status code to more easily judge what to do with the message that didn't go through
                while (playerMessages.Count > 0)
                {
                    _ = TransmitMessage(url + "/player", playerMessages.Dequeue(), tmpPlayerMessages);
                }
                while (objectMessages.Count > 0)
                {
                    _ = TransmitMessage(url + "/object", objectMessages.Dequeue(), tmpObjectMessages);
                }
                while (buttonMessages.Count > 0)
                {
                    _ = TransmitMessage(url + "/special", buttonMessages.Dequeue(), tmpButtonMessages);
                }
                while (spawnMessages.Count > 0)
                {
                    _ = TransmitMessage(url + "/special", spawnMessages.Dequeue(), tmpSpawnMessages);
                }
                while (miscMessages.Count > 0)
                {
                    MiscLogMessage msg = miscMessages.Dequeue();
                    msg.playerId = playerId;
                    //Debug.Log("!!!Misc:" + JsonConvert.SerializeObject(msg));
                    _ = TransmitMessage(url + "/logMisc", msg, tmpMiscMessages);
                }
                //TODO: Disabled for now since logging can cause infinite loops
                /*while (logMessages.Count > 0)
                {
                    _ = TransmitMessage(url + "/log", logMessages.Dequeue(), tmpLogMessages);
                }*/
                // If we couldn't transmit a message to the api successfully write it to disk
                while (tmpPlayerMessages.Count > 0)
                {
                    var message = tmpPlayerMessages.Dequeue();
                    FileLogger.QueueForWrite(message);
                }
                while (tmpObjectMessages.Count > 0)
                {
                    var message = tmpObjectMessages.Dequeue();
                    FileLogger.QueueForWrite(message);
                }
                while (tmpButtonMessages.Count > 0)
                {
                    var message = tmpButtonMessages.Dequeue();
                    FileLogger.QueueForWrite(message);
                }
                // TODO: For now we just write the log messages to disk
                while (logMessages.Count > 0)
                {
                    var message = logMessages.Dequeue();
                    FileLogger.QueueForWrite(message);
                }


                // Wait a second before retrying to send messages
                await Task.Delay(1000);

            }
        }

        private PlayerMessage CreatePlayerMessage()
        {
            PlayerMessage message = new PlayerMessage()
            {
                playerId = playerId,
                localTime = -1,
                messageId = messageCount++,
                audioData = new PlayerAudioMessage() { base64 = "" },
                body = new PlayerBodyMessage()
                {
                    positions = new List<PositionMessage>(),
                    rotations = new List<RotationMessage>(),
                    cameraRotations = new List<RotationMessage>(),
                    cameraPositions = new List<PositionMessage>(),
                },
                leftHand = new PlayerHandMessage()
                {
                    positions = new List<PositionMessage>(),
                    rotations = new List<RotationMessage>(),
                },
                rightHand = new PlayerHandMessage()
                {
                    positions = new List<PositionMessage>(),
                    rotations = new List<RotationMessage>(),
                },
                metaMessage = new MetaMessage()
                {
                    faceStates = new List<OVRPlugin.FaceState>(),
                    eyeGazesStates = new List<OVRPlugin.EyeGazesState>(),
                    leftHandStates = new List<OVRPlugin.HandState>(),
                    rightHandStates = new List<OVRPlugin.HandState>(),
                },
                count = new List<int>()
            };
            return message;
        }

        private Byte[] FloatPcmToWav(float[] samples)
        {
            Int16[] intData = new Int16[samples.Length];

            Byte[] bytesData = new Byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = short.MaxValue; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                Byte[] byteArr = new Byte[2];
                byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }
            return bytesData;
        }
        private void TransmitObjectData(MonoBehaviour item, Hand hand, ObjectTrackType used, ObjectTrackType grasped)
        {
            HandController controller = hand.GetComponent<HandController>();
            if (!controller || currentRoom == null)
                return;
            ObjectMessage message = new ObjectMessage()
            {
                playerId = playerId,
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                referenceMessage = messageCount,
                objectId = NetworkScene.GetNetworkId(item).ToString(),
                objectName = item.name,
                hand = controller.Left ? "Left" : "Right",
            };

            if (used != ObjectTrackType.UNCHANGED)
                message.interaction = used == ObjectTrackType.STARTED ? "used" : "unused";
            if (grasped != ObjectTrackType.UNCHANGED)
                message.interaction = grasped == ObjectTrackType.STARTED ? "grasped" : "ungrasped";

            objectMessages.Enqueue(message);
        }

        private void TransmitButtonData(GameObject buttonClicked)
        {
            if (currentRoom == null)
            {
                return;
            }
            ButtonMessage message = new ButtonMessage()
            {
                playerId = playerId,
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                referenceMessage = messageCount,
                buttonId = buttonClicked.GetInstanceID(),
                buttonName = buttonClicked.name,
                mode = "button"
            };
            buttonMessages.Enqueue(message);
        }

        private void TransmitLogData(string logString, LogType type, string stackTrace)
        {
            LogMessage message = new LogMessage()
            {
                playerId = playerId,
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                referenceMessage = messageCount,
                logMessage = logString,
                logType = type.ToString(),
                stacktrace = stackTrace,
            };
            logMessages.Enqueue(message);

        }

        private struct MetaAvatarLog
        {
            public string avatar_id;
        }

        private void LogSelectedMetaAvatar(ApiRole? role)
        {
            RoomClient roomClient = NetworkScene.Find(this).GetComponentInChildren<RoomClient>();
            MetaAvatarLog metaAvatarLog = new MetaAvatarLog() { avatar_id = roomClient.Me["ubiq.avatar.meta.userid"] };
            miscMessages.Enqueue(new MiscLogMessage()
            {
                localTime = (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds,
                jsonData = metaAvatarLog
            });
        }

        void ClearData()
        {
            audioData.Clear();
        }

        void OnApplicationQuit()
        {
            // Make sure the file logger finishes writing all the files
            FileLogger.OnApplicationQuit();
        }


    }

}
