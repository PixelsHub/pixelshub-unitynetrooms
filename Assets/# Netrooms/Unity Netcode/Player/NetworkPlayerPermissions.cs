using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkPlayerPermissions : NetworkRequiredInstance<NetworkPlayerPermissions>
    {
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
