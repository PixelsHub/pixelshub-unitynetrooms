using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class NetworkPlayer : NetworkBehaviour
    {
        public static event Action<NetworkPlayer> OnLocalPlayerSpawned;
        public static event Action<NetworkPlayer> OnLocalPlayerObjectInstantiated;
        public static event Action<NetworkPlayer> OnLocalPlayerDespawned;

        public GameObject LocalPlayerInstance { get; private set; }

        public PlayerDeviceCategory DeviceCategory => deviceCategory.Value;

        private readonly NetworkVariable<PlayerDeviceCategory> deviceCategory = new
            (PlayerDeviceCategory.Unknown, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField]
        private GameObject defaultLocalPlayerPrefab;

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        [SerializeField]
        private GameObject immersiveLocalPlayerPrefab;
#endif

        [Space(8)]
        [SerializeField]
        private PlayerAvatar defaultAvatarPrefab;

        [SerializeField]
        private PlayerAvatar immersiveAvatarPrefab;

        private PlayerAvatar avatar;

        public override void OnNetworkSpawn()
        {
            if(IsLocalPlayer)
            {
                OnLocalPlayerSpawned?.Invoke(this);

                InstantiateLocalPlayerPrefab();
                InitializeLocalPlayerVariables();

                if(IsServer)
                    SpawnPlayerAvatar(OwnerClientId);
                else
                    SpawnPlayerAvatarServerRpc(OwnerClientId);

                OnLocalPlayerObjectInstantiated?.Invoke(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            if(IsLocalPlayer)
            {
                OnLocalPlayerDespawned?.Invoke(this);

                Destroy(LocalPlayerInstance);
                LocalPlayerInstance = null;
            }
        }

        private void InstantiateLocalPlayerPrefab()
        {
#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
            if(XRImmersiveness.IsActive)
                LocalPlayerInstance = Instantiate(immersiveLocalPlayerPrefab);
            else
                LocalPlayerInstance = Instantiate(defaultLocalPlayerPrefab);
#else
            localPlayerInstance = Instantiate(defaultLocalPlayerPrefab);
#endif
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
        private void SpawnPlayerAvatarServerRpc(ulong ownerClientId) 
        {
            SpawnPlayerAvatar(ownerClientId);
        }

        private void SpawnPlayerAvatar(ulong ownerClientId) 
        {
            PlayerAvatar targetPrefab;

            if(deviceCategory.Value == PlayerDeviceCategory.ImmersiveXR)
                targetPrefab = immersiveAvatarPrefab;
            else
                targetPrefab = defaultAvatarPrefab;
            
            avatar = Instantiate(targetPrefab, transform);
            avatar.NetworkObject.SpawnAsPlayerObject(ownerClientId);
        }
    }
}
