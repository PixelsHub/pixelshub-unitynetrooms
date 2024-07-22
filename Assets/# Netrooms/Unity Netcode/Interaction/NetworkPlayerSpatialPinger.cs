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

        public event Action<bool> OnLocalPlayerAllowedToPingChanged;

        public bool IsLocalPlayerAllowedToPing
        {
            get => isLocalPlayerAllowedToPing;
            set
            {
                if(isLocalPlayerAllowedToPing == value)
                {
                    isLocalPlayerAllowedToPing = value;
                    OnLocalPlayerAllowedToPingChanged?.Invoke(value);
                }
            }
        }

        private bool isLocalPlayerAllowedToPing = true;

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

            if(!isLocalPlayerAllowedToPing)
            {
                Debug.LogWarning($"Local client is not allowed to ping.");
                return;
            }

            if(NetworkPlayer.Local == null)
            {
                Debug.LogError("No local player found.");
                return;
            }

            var relativePosition = NetworkWorldOrigin.Transform.InverseTransformPoint(worldPosition);
            var relativeRotation = Quaternion.Inverse(NetworkWorldOrigin.Transform.rotation) * worldRotation;
            CreatePingRpc(relativePosition, relativeRotation.eulerAngles, NetworkPlayer.Local.OwnerClientId);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void CreatePingRpc(Vector3 relativePosition, Vector3 relativeEuler, ulong playerId) 
        {
            if(!IsSpawned)
                return;

            var ping = pool.Get(transform);

            bool playerFound = NetworkPlayer.Players.TryGetValue(playerId, out var player);

            if(!playerFound)
                Debug.LogError($"Could not find player (id={playerId}) for spatial ping.");

            var origin = NetworkWorldOrigin.Transform; // Relative to world origin
            var worldPosition = origin.TransformPoint(relativePosition);
            var worldRotation = origin.rotation * Quaternion.Euler(relativeEuler);

            ping.Play(worldPosition, worldRotation, player);
        }
    }
}