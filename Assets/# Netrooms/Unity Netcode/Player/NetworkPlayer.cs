using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class NetworkPlayer : NetworkBehaviour
    {
        public static event Action<NetworkPlayer> OnPlayerSpawned;
        public static event Action<NetworkPlayer> OnPlayerDespawned;
        public static event Action<NetworkPlayer, string> OnPlayerKicked;

        public event Action<Color> OnColorChanged;

        public static NetworkPlayer Local { get; private set; }

        public static IReadOnlyDictionary<ulong, NetworkPlayer> Players => players;

        public string UserIdentifier => userIdentifier.Value.ToString();
        
        public PlayerDeviceCategory DeviceCategory => deviceCategory.Value;

        public Color Color => color.Value;

        public PlayerAvatar Avatar { get; private set; }

        private static readonly Dictionary<ulong, NetworkPlayer> players = new();

        private readonly NetworkVariable<bool> validated = new();

        private readonly NetworkVariable<FixedString512Bytes> userIdentifier = new
            (string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<PlayerDeviceCategory> deviceCategory = new
            (PlayerDeviceCategory.Undefined, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private readonly NetworkVariable<Color> color = new
            (Color.white, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField]
        private PlayerAvatar defaultAvatarPrefab;

        [SerializeField]
        private PlayerAvatar immersiveAvatarPrefab;

        public void AssignColor(Color color)
        {
            if(!IsServer)
            {
                Debug.Assert(false, "Assigning colors is only permitted from the server.");
                return;
            }

            this.color.Value = color;
            OnColorChanged?.Invoke(color);
        }

        public void Kick(string reason = null) 
        {
            if(!IsServer)
            {
                Debug.Assert(false);
                return;
            }

            if(IsLocalPlayer)
            {
                Debug.Assert(false, "Kicking the host player should never be attempted.");
                return;
            }

            StartCoroutine(KickCoroutine());

            IEnumerator KickCoroutine()
            {
                NotifyKickedPlayerRpc(reason);

                yield return null;

                NetworkObject.Despawn(true);

                yield return null;

                NetworkManager.Singleton.DisconnectClient(OwnerClientId);
            }
        }

        public override void OnNetworkSpawn()
        {
            if(IsLocalPlayer)
            {
                Debug.Log($"Spawning local player as user \"{User.LocalIdentifier}\".");

                userIdentifier.Value = User.LocalIdentifier;
                deviceCategory.Value = GetLocalPlayerDeviceCategory();

                if(IsServer) // Hosts do not need player validation
                {
                    validated.Value = true;
                    InitializeValidatedPlayerRpc();
                }
                else
                    ServerValidatePlayerConnectionRpc();
            }
            else
            {
                if(validated.Value) // Initialize alreay validated players
                    InitializePlayer();
            }
        }

        public override void OnNetworkDespawn()
        {
            if(IsLocalPlayer)
                Local = null;

            if(!IsServer)
            {
                color.OnValueChanged -= HandleColorChanged;
            }

            if(players.Remove(OwnerClientId))
                OnPlayerDespawned?.Invoke(this);
        }

        [Rpc(SendTo.Server)]
        private void ServerValidatePlayerConnectionRpc() 
        {
            if(PlayerConnectionRequirement.IsPlayerAllowedToConnect(this, out string failureReason))
            {
                validated.Value = true;
                InitializeValidatedPlayerRpc();
            }
            else
                Kick(failureReason);
        }

        [Rpc(SendTo.Everyone)]
        private void InitializeValidatedPlayerRpc() 
        {
            if(!IsSpawned)
                return;

            InitializePlayer();
        }
        
        private void InitializePlayer()
        {
            players.Add(OwnerClientId, this);

            if(IsLocalPlayer)
            {
                Local = this;

                if(IsServer)
                    SpawnPlayerAvatar(OwnerClientId, deviceCategory.Value);
                else
                    SpawnPlayerAvatarServerRpc(OwnerClientId, deviceCategory.Value);
            }

            if(!IsServer)
            {
                color.OnValueChanged += HandleColorChanged;
            }

            OnPlayerSpawned?.Invoke(this);
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void NotifyKickedPlayerRpc(string reason)
        {
            Debug.Log($"{(IsLocalPlayer ? "Local " : string.Empty)}Player has been kicked (Id={OwnerClientId})");
            OnPlayerKicked?.Invoke(this, reason);
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

        private void HandleColorChanged(Color prevColor, Color newColor)
        {
            OnColorChanged?.Invoke(newColor);
        }

        private PlayerDeviceCategory GetLocalPlayerDeviceCategory()
        {
            if(XRImmersiveness.IsActive)
                return PlayerDeviceCategory.ImmersiveXR;
            else
            {
                switch(SystemInfo.deviceType)
                {
                    case DeviceType.Handheld:
                        return PlayerDeviceCategory.Handheld;

                    case DeviceType.Desktop:
                        return PlayerDeviceCategory.Desktop;
                }
            }

            return PlayerDeviceCategory.Undefined;
        }
    }
}
