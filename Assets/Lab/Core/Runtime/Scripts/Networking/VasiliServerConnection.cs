using Ubiq.Rooms;
using UnityEngine;

namespace VaSiLi.Networking
{
    /// <summary>
    /// Points the RoomClient at the Ubiq server from <see cref="VasiliEndpoints"/>.
    /// Runs in Awake, before RoomClient connects in Start.
    /// </summary>
    [RequireComponent(typeof(RoomClient))]
    public class VasiliServerConnection : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<RoomClient>().SetDefaultServer(VasiliEndpoints.Current.UbiqServerOrDefault());
        }
    }
}
