using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    /// <summary>
    /// Components that are associated with a NetworkPlayer instance. They MUST be owned by the same client as the NetworkPlayer.
    /// </summary>
    public abstract class NetworkPlayerComponent : NetworkBehaviour
    {
        public NetworkPlayer Player { get; private set; }

        public override void OnNetworkSpawn()
        {
            if(NetworkPlayer.Players.TryGetValue(OwnerClientId, out var player))
                Player = player;
            else
                Debug.LogError($"Invalid {GetType()} spawned. Could not find Player for Id={OwnerClientId}.");
        }
    }
}
