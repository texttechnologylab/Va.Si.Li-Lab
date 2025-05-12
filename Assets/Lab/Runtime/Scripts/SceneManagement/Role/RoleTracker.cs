using UnityEngine;
using Ubiq.Messaging;
using Ubiq.Spawning;
using Ubiq.Rooms;
using VaSiLi.Networking;
using static VaSiLi.Networking.ResponsiveNetworking;

namespace VaSiLi.SceneManagement
{
    /// <summary>
    /// The RoleTracker keeps track of a users role and updates/synchronizes the state to other users
    /// </summary>
    public class RoleTracker : MonoBehaviour, INetworkSpawnable
    {
        public NetworkId NetworkId { get; set; }
        private NetworkContext context;
        private RoomClient roomClient;
        [SerializeField]
        private ApiRole? _role;

        public ApiRole? Role { get => _role; set => SetRole(value, true); }

        private void Start()
        {
            context = NetworkScene.Register(this);
        }

        private void Awake()
        {
            roomClient = GameObject.Find("Social Network Scene").GetComponent<RoomClient>();
            roomClient.OnPeerAdded.AddListener(SynchronizeData);
        }

        private void SynchronizeData(IPeer peer)
        {
            if (_role.HasValue)
                ResponsiveNetworking.SendJson(context.Id, _role.Value, SimpleMessageHandler);
        }

        public void SimpleMessageHandler(CallbackResult result)
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

                ResponsiveNetworking.SendJson(context.Id, result.message, SimpleMessageHandler, resultContext);
            }

        }

        private void SetRole(ApiRole? role, bool sendData)
        {
            _role = role;
            RoleManager.roleTrackerUpdated.Invoke(role);
            if (role.HasValue && sendData)
                context.SendJson(role.Value);
        }

        public void ProcessMessage(ReferenceCountedSceneGraphMessage msg)
        {
            ApiRole data = msg.FromJson<ApiRole>();
            SetRole(data, false);
        }
    }
}