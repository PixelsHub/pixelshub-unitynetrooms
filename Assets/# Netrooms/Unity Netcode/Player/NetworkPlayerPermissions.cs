using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkPlayerPermissions : NetworkPersistentSingleton<NetworkPlayerPermissions>
    {
        public static event Action<ulong, string> OnClientPermissionSetRequested;

        [Rpc(SendTo.Server)]
        public void SetPermissionsServerRpc(string newPermissionsJson, RpcParams rpcParams = default) 
        {
            OnClientPermissionSetRequested?.Invoke(rpcParams.Receive.SenderClientId, newPermissionsJson);
        }

        public override void OnNetworkSpawn()
        {
            if(IsServer)
            {
                PlayerPermissions.OnPermissionsChanged += HandlePermissionsChanged;
                NetworkPlayer.OnSpawnedPlayerValidated += HandleSpawnedPlayerValidated;
            }
        }

        public override void OnNetworkDespawn()
        {
            if(IsServer)
            {
                PlayerPermissions.OnPermissionsChanged -= HandlePermissionsChanged;
                NetworkPlayer.OnSpawnedPlayerValidated -= HandleSpawnedPlayerValidated;
            }
        }

        private void HandlePermissionsChanged() 
        {
            Debug.Assert(IsServer);

            ReplicatePermissionsClientRpc(PlayerPermissions.RegisterJson);
        }

        private void HandleSpawnedPlayerValidated(NetworkPlayer player)
        {
            if(player.IsHost)
                return;

            RpcParams rpcParams = RpcTarget.Single(player.OwnerClientId, RpcTargetUse.Temp);
            ReplicatePermissionsClientRpc(PlayerPermissions.RegisterJson, rpcParams);
        }

        [Rpc(SendTo.NotServer, AllowTargetOverride = true)]
        private void ReplicatePermissionsClientRpc(string permissionsJson, RpcParams _ = default) 
        {
            PlayerPermissions.ParsePermissions(permissionsJson);
        }
    }
}
