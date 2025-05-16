using System.Collections.Generic;
using System.Linq;
using Ubiq.Messaging;
using Ubiq.Rooms;
using Ubiq.Samples;
using UnityEngine;
using UnityEngine.Events;
using VaSiLi.Misc;
using VaSiLi.Networking;
using static VaSiLi.Networking.ResponsiveNetworking;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// Manager class for the level state
    /// Manages if a level can be completed/if a level has been completed
    /// Synchronizes the LevelState/Readystate among the users
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public enum Status
        {
            UNDEFINED,
            WAITING,
            READY,
            RUNNING
        }

        private enum MessageType
        {
            STATUS,
            LEVEL,
            SYNCH
        }

        private struct Message
        {
            public MessageType type;
            public string peerID;
            public int level;
            public Status status;
        }

        private Dictionary<string, Status> playerData = new Dictionary<string, Status>();
        private NetworkContext context;
        public static int CurrentLevelIndex { private set; get; }
        public static ApiLevel[] Levels => SceneManager.CurrentScene?.levels;
        public static ApiLevel? CurrentLevel => Levels[CurrentLevelIndex];
        public static UnityAction<int> levelChanged = delegate { };
        public static UnityAction<int, Status> levelStatus = delegate { };
        public static UnityAction<bool> allReady = delegate { };
        private static Status _status = Status.UNDEFINED;
        public Status CurrentStatus { set => SetStatus(value); get => _status; }
        public RoomClient roomClient;
        public SocialMenu socialMenu;

        public void Start()
        {
            context = NetworkScene.Register(this);
            roomClient.OnPeerAdded.AddListener(SynchronizeData);
            roomClient.OnPeerRemoved.AddListener(RemovePeer);
            RoleManager.roleChanged += OnRoleUpdate;
            CurrentLevelIndex = 0;
            TimeManager.timerUpdated += OnTimerUpdated;
        }

        public void CheckinStatus()
        {
            levelStatus.Invoke(CurrentLevelIndex, CurrentStatus);
        }

        private void SetStatus(Status status)
        {
            if (RoleManager.CurrentRole.HasValue && RoleManager.CurrentRole?.IsPlayer == true)
            {
                SendStatus(roomClient.Me.uuid, status);
                OnStatusUpdate(roomClient.Me.uuid, status);
            }
            levelStatus.Invoke(CurrentLevelIndex, status);
            _status = status;
        }

        public void OnRoleUpdate(ApiRole? role)
        {
            if (role?.IsPlayer == true)
                SetStatus(CurrentStatus == Status.UNDEFINED ? Status.WAITING : CurrentStatus);
            if (!role.HasValue)
            {
                Debug.Log("Reset data");
                ResetData();
            }
        }

        public void ResetData()
        {
            var status = Status.UNDEFINED;
            SendStatus(roomClient.Me.uuid, status);
            OnStatusUpdate(roomClient.Me.uuid, status);
            levelStatus.Invoke(CurrentLevelIndex, status);
            _status = status;
            CurrentLevelIndex = 0;
        }

        public void OnReady()
        {
            SetStatus(Status.READY);
        }

        public void OnUnReady()
        {
            SetStatus(Status.WAITING);
        }

        public void SendStatus(string peer, Status status)
        {
            if (RoleManager.CurrentRole.HasValue && RoleManager.CurrentRole?.IsPlayer == false)
                return;
            var msg = new Message()
            {
                type = MessageType.STATUS,
                level = CurrentLevelIndex,
                peerID = peer,
                status = status
            };

            ResponsiveNetworking.SendJson(context.Id, msg, SimpleStatusMessageHandler);

            //context.SendJson<Message>(msg);
        }

        public void SimpleStatusMessageHandler(CallbackResult result)
        {
            if (!result.success)
            {
                var resultContext = result.context;
                if (resultContext == null)
                {
                    resultContext = 1;
                }
                else
                {
                    int count = (int)resultContext;
                    if (count > 5)
                    {
                        Debug.LogError("A client couldn't process the message after 5 tries");
                        return;
                    }
                    count = count + 1;
                    resultContext = count;
                }

                ResponsiveNetworking.SendJson(context.Id, result.message, SimpleStatusMessageHandler, resultContext);
            }

        }

        private void RemovePeer(IPeer peer)
        {
            if (playerData.ContainsKey(peer.uuid))
                playerData.Remove(peer.uuid);
            TestAllReady();
        }

        private void TestAllReady()
        {
            if (RoleManager.CurrentRole?.IsAdmin == true)
            {
                if (playerData.All(pair => pair.Value == Status.READY || pair.Value == Status.UNDEFINED) && HasEnoughPlayers())
                    allReady.Invoke(true);
                else
                    allReady.Invoke(false);
            }
        }

        /// <summary>
        /// Test to see if enough players are in the scene/have readied up
        /// </summary>
        /// <returns>True if there's enough players; False otherwise</returns>
        public bool HasEnoughPlayers()
        {
            var players = playerData.Select(p => p.Key != roomClient.Me.uuid && p.Value != Status.UNDEFINED).Count();
            if (SceneManager.CurrentScene?.amountPlayersRequired <= playerData.Count)
                return true;
            else if (SceneManager.CurrentScene?.amountPlayersRequired <= players - 1)
            {
                Debug.Log("Missed self in list");
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// Tries to start the current level changes ever players state to RUNNING
        /// </summary>
        /// TODO: Rename to TryStartLevel
        public void StartLevel()
        {
            if (RoleManager.CurrentRole?.IsAdmin == true)
            {
                if (playerData.All(pair => pair.Value == Status.READY || pair.Value == Status.UNDEFINED))
                    UpdateAllStatus(Status.RUNNING);
            }
        }

        /// <summary>
        /// Tries to complete the level
        /// </summary>
        public void TryCompleteLevel()
        {
            if (RoleManager.CurrentRole?.IsAdmin == true)
            {
                if (playerData.All(pair => pair.Value == Status.RUNNING || pair.Value == Status.UNDEFINED))
                    TryProgressLevel();
            }
        }

        /// <summary>
        /// Tries to progess the level checking different conditions that need to be met defined by the level(api)
        /// </summary>
        private void TryProgressLevel()
        {
            // TODO: Check if the other level conditions have been met
            Debug.Log("!!!Delay" + CurrentLevelHasDelay());
            if (!CurrentLevelHasDelay())
            {
                StartNextLevel();
                // Run next level
            }
        }

        /// <summary>
        /// Starts the next level incrementing the currentlevel by one and setting the status of all users to WAITING
        /// </summary>
        private void StartNextLevel()
        {
            UpdateAllStatus(Status.WAITING);

            CurrentLevelIndex++;
            SendLevel(CurrentLevelIndex);
            OnLevelChanged(CurrentLevelIndex);
        }

        /// <summary>
        /// Called whenever the level gets changed be it locally or through a network message
        /// </summary>
        /// <param name="level">The new level value</param>
        private void OnLevelChanged(int level)
        {
            // Don't switch if we're already on this level
            if (CurrentLevelIndex == level)
                return;
            CurrentLevelIndex = level;
            if (RoleManager.CurrentRole?.IsPlayer == true)
            {
                // TODO: Move to seperate class
                socialMenu.Request();
            }
            Debug.Log("Level changed to level: " + level);
            levelChanged.Invoke(CurrentLevelIndex);

        }

        /// <summary>
        /// Helper function to set every players status
        /// </summary>
        /// <param name="status">The Status to be set</param>
        private void UpdateAllStatus(Status status)
        {
            CurrentStatus = status;
            foreach (var key in playerData.Keys.ToList())
            {
                if (playerData[key] != status)
                {
                    SendStatus(key, status);
                    OnStatusUpdate(key, status);
                }
            }
        }

        /// <summary>
        /// Sends the level to all currently connected clients
        /// </summary>
        /// <param name="level"></param>
        private void SendLevel(int level)
        {
            var msg = new Message()
            {
                type = MessageType.LEVEL,
                level = level,
            };

            ResponsiveNetworking.SendJson(context.Id, msg, SimpleStatusMessageHandler);


            //context.SendJson<Message>(msg);
        }

        /// <summary>
        /// Helper function to handle what to do when the status of a certain user gets updated
        /// A lot of this is necessary since we need to keep track of every users current status so
        /// the ready/start mechanism can work correctly
        /// </summary>
        /// <param name="user"></param>
        /// <param name="status"></param>
        private void OnStatusUpdate(string user, Status status)
        {
            Debug.Log($"{user} : {status}");
            // If we already have the player in our Dictionary
            if (playerData.ContainsKey(user) && playerData[user] != status)
            {
                var pStatus = playerData[user];
                pStatus = status;
                playerData[user] = pStatus;
            }
            else if (!playerData.ContainsKey(user))
            {
                playerData.Add(user, status);
            }
            // TODO: Devide status updates into global and specific ones
            if (user == roomClient.Me.uuid)
            {
                levelStatus.Invoke(CurrentLevelIndex, status);
                _status = status;
            }
            // Test if everyone is ready
            TestAllReady();
        }

        /// <summary>
        /// Delegate function to see if a current level with a time restriction should count as completed
        /// </summary>
        /// <param name="time"></param>
        private void OnTimerUpdated(int time)
        {
            if (!CurrentLevelHasDelay())
                return;
            var level = SceneManager.CurrentScene?.levels;

            if (time == level[CurrentLevelIndex].delay)
            {
                StartNextLevel();
            }
        }

        /// <summary>
        /// Test if the current level has a delay
        /// </summary>
        /// <returns>True if yes; false otherwise</returns>
        private bool CurrentLevelHasDelay()
        {
            if (SceneManager.CurrentScene == null)
                return false;
            var level = SceneManager.CurrentScene.Value.levels;

            if (level.Length <= CurrentLevelIndex || level[CurrentLevelIndex].delay == 0)
                return false;
            return true;
        }

        /// <summary>
        /// Delegate that synchronizes the current level and status with a newly connected client
        /// </summary>
        /// <param name="_peer"></param>
        private void SynchronizeData(IPeer _peer)
        {
            if (RoleManager.CurrentRole?.IsPlayer == false || CurrentStatus == Status.UNDEFINED)
                return;

            Message msg = new Message()
            {
                type = MessageType.SYNCH,
                peerID = roomClient.Me.uuid,
                level = CurrentLevelIndex,
                status = CurrentStatus
            };

            ResponsiveNetworking.SendJson(context.Id, msg, SimpleStatusMessageHandler);

        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage message)
        {
            var m = message.FromJson<Message>();
            switch (m.type)
            {
                case MessageType.STATUS:
                    OnStatusUpdate(m.peerID, m.status);
                    break;
                case MessageType.LEVEL:
                    OnLevelChanged(m.level);
                    break;
                case MessageType.SYNCH:
                    OnStatusUpdate(m.peerID, m.status);
                    CurrentStatus = m.status;
                    OnLevelChanged(m.level);
                    break;
            }

        }

        public static ApiLevelRoleDescription GetLevelDescription()
        {
            if (!CurrentLevel.HasValue || !CurrentLevel.Value.roleDescriptions.ContainsKey(RoleManager.CurrentRole.Value.id)) {
                return new ApiLevelRoleDescription() {description = new string[] {"No description found"}};
            }
            Dictionary<string, ApiLevelRoleDescription> locales = CurrentLevel.Value.roleDescriptions[RoleManager.CurrentRole.Value.id].locales;
            switch (LanguageManager.SelectedLanguage)
            {
                case LanguageManager.Language.EN:
                    if (locales.ContainsKey("en"))
                    {
                        return locales["en"];
                    }
                    break;
                case LanguageManager.Language.DE:
                    if (locales.ContainsKey("de"))
                    {
                        return locales["de"];
                    }
                    break;
                default:
                    if (locales.ContainsKey("en"))
                    {
                        return locales["en"];
                    }
                    break;

            }
            return new ApiLevelRoleDescription() {description = new string[] {"No description found"}};
        }

    }
}
