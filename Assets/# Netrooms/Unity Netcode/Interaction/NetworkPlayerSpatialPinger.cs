using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkPlayerSpatialPinger : NetworkBehaviour
    {
        public class Pool : GameObjectPool<PlayerSpatialPing> 
        {
            protected override void InitializeInstantiatedObject(PlayerSpatialPing obj)
            {
                obj.Initialize(this);
            }
        }

        [SerializeField]
        private Pool pool;

        public void Ping(Vector3 position, Vector3 euler)
        {
            Debug.Assert(IsClient);

            if(NetworkPlayer.Local == null)
            {
                Debug.LogError("No local player found.");
                return;
            }

            CreatePingRpc(position, euler, NetworkPlayer.Local.OwnerClientId);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CreatePingRpc(Vector3 position, Vector3 euler, ulong playerId) 
        {
            var ping = pool.Get(NetworkWorldOrigin.Transform);

            bool playerFound = NetworkPlayer.Players.TryGetValue(playerId, out var player);

            if(!playerFound)
                Debug.LogError($"Could not find player (id={playerId}) for spatial ping.");

            ping.Play(position, Quaternion.Euler(euler), player);
        }
    }
}
