using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class NetworkPlayer : NetworkBehaviour
    {
        public static IReadOnlyDictionary<ulong, NetworkPlayer> Players => players;

        public PlayerDeviceCategory DeviceCategory => deviceCategory.Value;

        public PlayerAvatar Avatar { get; private set; }

        private static readonly Dictionary<ulong, NetworkPlayer> players = new();

        private readonly NetworkVariable<PlayerDeviceCategory> deviceCategory = new
            (PlayerDeviceCategory.Unknown, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField]
        private PlayerAvatar defaultAvatarPrefab;

        [SerializeField]
        private PlayerAvatar immersiveAvatarPrefab;

        public override void OnNetworkSpawn()
        {
            players.Add(OwnerClientId, this);

            if(IsLocalPlayer)
            {
                InitializeLocalPlayerVariables();

                if(IsServer)
                    SpawnPlayerAvatar(OwnerClientId, deviceCategory.Value);
                else
                    SpawnPlayerAvatarServerRpc(OwnerClientId, deviceCategory.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            players.Remove(OwnerClientId);
        }

        private void InitializeLocalPlayerVariables() 
        {
            if(XRImmersiveness.IsActive)
            {
                deviceCategory.Value = PlayerDeviceCategory.ImmersiveXR;
            }
            else
            {
                switch(SystemInfo.deviceType)
                {
                    case DeviceType.Handheld:
                        deviceCategory.Value = PlayerDeviceCategory.Handheld;
                        break;

                    case DeviceType.Desktop:
                        deviceCategory.Value = PlayerDeviceCategory.Desktop;
                        break;
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void SpawnPlayerAvatarServerRpc(ulong ownerClientId, PlayerDeviceCategory deviceCategory) 
        {
            SpawnPlayerAvatar(ownerClientId, deviceCategory);
        }

        private void SpawnPlayerAvatar(ulong ownerClientId, PlayerDeviceCategory deviceCategory) 
        {
            PlayerAvatar targetPrefab;

            if(deviceCategory == PlayerDeviceCategory.ImmersiveXR)
                targetPrefab = immersiveAvatarPrefab;
            else
                targetPrefab = defaultAvatarPrefab;

            Avatar = Instantiate(targetPrefab, transform);
            Avatar.NetworkObject.SpawnAsPlayerObject(ownerClientId);
        }
    }
}
