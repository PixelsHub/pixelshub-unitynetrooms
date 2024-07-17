using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkPlayerSpatialPinger : NetworkBehaviour
    {
        [Serializable]
        public class Pool : GameObjectPool<PlayerSpatialPing> 
        {
            [HideInInspector]
            public NetworkPlayerSpatialPinger owner;

            protected override void InitializeInstantiatedObject(PlayerSpatialPing obj)
            {
                obj.OnPlayEnded += Pool;
            }
        }

        [SerializeField]
        private Pool pool;

        public void Ping(Vector3 worldPosition, Quaternion worldRotation)
        {
            if(!IsSpawned)
            {
                Debug.LogWarning("Spatial Pings should not be performed if not connected.");
                return;
            }

            Debug.Assert(IsClient);

            if(NetworkPlayer.Local == null)
            {
                Debug.LogError("No local player found.");
                return;
            }

            var localPosition = NetworkWorldOrigin.Transform.InverseTransformPoint(worldPosition);
            var localRotation = Quaternion.Inverse(NetworkWorldOrigin.Transform.rotation) * worldRotation;
            CreatePingRpc(localPosition, localRotation.eulerAngles, NetworkPlayer.Local.OwnerClientId);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CreatePingRpc(Vector3 localPosition, Vector3 localEuler, ulong playerId) 
        {
            if(!IsSpawned)
                return;

            var ping = pool.Get(transform);

            bool playerFound = NetworkPlayer.Players.TryGetValue(playerId, out var player);

            if(!playerFound)
                Debug.LogError($"Could not find player (id={playerId}) for spatial ping.");

            var origin = NetworkWorldOrigin.Transform; // Relative to world origin
            var worldPosition = origin.TransformPoint(localPosition);
            var worldRotation = origin.rotation * Quaternion.Euler(localEuler);

            ping.Play(worldPosition, worldRotation, player);
        }
    }
}
