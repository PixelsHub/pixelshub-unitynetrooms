using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class NetworkPlayer : NetworkBehaviour
    {
        public class LogEventId
        {
            public const string playerConnected = "PLAYER_CONNECTED";
            public const string playerDisconnected = "PLAYER_DISCONNECTED";
            public const string playerKicked = "PLAYER_KICKED";
        }

        /// <summary>
        /// Invoked for spawned players that have been validated by the server.
        /// </summary>
        public static event Action<NetworkPlayer> OnSpawnedPlayerValidated;

        /// <summary>
        /// Invoked for players that have been validated by the server and have despawned.
        /// </summary>
        public static event Action<NetworkPlayer> OnValidatedPlayerDespawned;

        public static event Action<NetworkPlayer, string> OnPlayerKicked;

        public event Action<Color> OnColorChanged;

        public static NetworkPlayer Local { get; private set; }

        public static IReadOnlyDictionary<ulong, NetworkPlayer> Players => players; // Key is OwnerClientId

        public string UserIdentifier => userIdentifier.Value.ToString();
        
        public PlayerDeviceCategory DeviceCategory => deviceCategory.Value;

        public Color Color => PlayerColoringScheme.GetColor(colorIndex.Value);

        public PlayerAvatar Avatar { get; private set; }

        private static readonly Dictionary<ulong, NetworkPlayer> players = new();

        private readonly NetworkVariable<bool> validated = new(false);

        private readonly NetworkVariable<FixedString512Bytes> userIdentifier = new(string.Empty);

        private readonly NetworkVariable<PlayerDeviceCategory> deviceCategory = new(PlayerDeviceCategory.Uninitialized);

        private readonly NetworkVariable<int> colorIndex = new
            (-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        [SerializeField]
        private PlayerAvatar defaultAvatarPrefab;

        [SerializeField]
        private PlayerAvatar immersiveAvatarPrefab;

        public static ulong[] PlayersToOwnerClientIdsArray()
        {
            ulong[] result = new ulong[players.Count];

            int i = 0;
            foreach(ulong id in players.Keys)
            {
                result[i] = id;
                i++;
            }

            return result;
        }

        public void Kick(string reason = null) 
        {
            if(!IsServer)
            {
                Debug.Assert(false, "Kicking must only be performed from the server.");
                return;
            }

            if(IsLocalPlayer)
            {
                Debug.Assert(false, "Kicking the host player is not allowed and should never be attempted.");
                return;
            }

            string[] logParams = new string[] { userIdentifier.Value.ToString(), reason };
            NetworkLogEvents.Add(LogEventId.playerKicked, Color, logParams);

            NetworkManager.Singleton.StartCoroutine(KickCoroutine());

            IEnumerator KickCoroutine()
            {
                NotifyKickedPlayerRpc(reason);
                yield return null;
                NetworkObject.Despawn(true);
                yield return null;
                NetworkManager.Singleton.DisconnectClient(OwnerClientId, reason);
            }
        }

        public override void OnNetworkSpawn()
        {
            if(IsLocalPlayer)
            {
                Debug.Log($"Spawning local player as user \"{LocalPlayerUserIdentifier.Value}\".");

                Local = this;

                if(IsHost) // Hosts do not need connection requirements validation
                {
                    userIdentifier.Value = LocalPlayerUserIdentifier.Value;
                    deviceCategory.Value = GetLocalPlayerDeviceCategory();
                    ServerFinalizeSpawnedPlayerValidation();
                }
                else
                    ServerValidateSpawnedLocalPlayerRpc(LocalPlayerUserIdentifier.Value, GetLocalPlayerDeviceCategory());
            }

            if(validated.Value)
                InitializeValidatedPlayer();
            else
                validated.OnValueChanged += HandleValidatedValueChanged;
        }

        public override void OnNetworkDespawn()
        {
            if(IsLocalPlayer)
                Local = null;

            validated.OnValueChanged -= HandleValidatedValueChanged;
            colorIndex.OnValueChanged -= HandleColorIndexChanged;

            if(players.Remove(OwnerClientId))
            {
                if(IsServer && !IsClient) // Only non-host servers report removal since host despawn means network shutdown
                {
                    NetworkPlayerSlots.Instance.RemovePlayerFromSlot(this);

                    string[] logParams = new string[] { $"{OwnerClientId}", $"{userIdentifier.Value}" };
                    NetworkLogEvents.Add(LogEventId.playerDisconnected, Color, logParams);
                }

                OnValidatedPlayerDespawned?.Invoke(this);
            }
        }

        [Rpc(SendTo.Server)]
        private void ServerValidateSpawnedLocalPlayerRpc(string userIdentifier, PlayerDeviceCategory deviceCategory)
        {
            this.userIdentifier.Value = userIdentifier;
            this.deviceCategory.Value = deviceCategory;

            if(PlayerConnectionRequirement.IsPlayerAllowedToConnect(this, out string failureReason))
                ServerFinalizeSpawnedPlayerValidation();
            else
                Kick(failureReason);
        }

        private void ServerFinalizeSpawnedPlayerValidation() 
        {
            Debug.Assert(IsServer);

            StartCoroutine(Coroutine());

            IEnumerator Coroutine()
            {
                while(NetworkPlayerSlots.Instance == null || !NetworkPlayerSlots.Instance.IsSpawned)
                    yield return null;

                if(IsSpawned) // Ensure no despawn ocurred during the wait
                {
                    if(NetworkPlayerSlots.Instance.TryAssignPlayerSlot(this, out int index))
                    {
                        colorIndex.Value = FindAvailableColorIndex(index);

                        validated.Value = true;

                        string[] logParams = new string[] { $"{OwnerClientId}", $"{userIdentifier.Value}" };
                        NetworkLogEvents.Add(LogEventId.playerConnected, Color, logParams);
                    }
                    else
                        Kick(NetworkPlayerSlots.limitReachedReason);
                }
            }
        }

        private void HandleValidatedValueChanged(bool wasValidated, bool isValidated)
        {
            if(isValidated)
            {
                validated.OnValueChanged -= HandleValidatedValueChanged;
                InitializeValidatedPlayer();
            }
        }

        private void InitializeValidatedPlayer()
        {
            StartCoroutine(Coroutine());

            IEnumerator Coroutine()
            {
                while(deviceCategory.Value == PlayerDeviceCategory.Uninitialized)
                    yield return null;

                if(IsSpawned) // Could have been despawned
                {
                    players.Add(OwnerClientId, this);

                    if(IsLocalPlayer)
                    {
                        if(IsServer)
                            SpawnPlayerAvatar(OwnerClientId, deviceCategory.Value);
                        else
                            SpawnPlayerAvatarServerRpc(OwnerClientId, deviceCategory.Value);
                    }

                    colorIndex.OnValueChanged += HandleColorIndexChanged;

                    OnSpawnedPlayerValidated?.Invoke(this);
                }
            }
        }

        [Rpc(SendTo.ClientsAndHost)]
        private void NotifyKickedPlayerRpc(string reason)
        {
            Debug.Log($"{(IsLocalPlayer ? "Local " : string.Empty)}Player has been kicked (Id={OwnerClientId}) - {reason}");
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

            Avatar = Instantiate(targetPrefab);
            Avatar.NetworkObject.SpawnAsPlayerObject(ownerClientId);
        }

        private void HandleColorIndexChanged(int prevColorIndex, int newColorIndex)
        {
            OnColorChanged?.Invoke(Color);
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

        private int FindAvailableColorIndex(int desirableIndex)
        {
            foreach(var player in players.Values)
            {
                // Desirable index is already used
                if(player.colorIndex.Value == desirableIndex)
                {
                    // Iterate and find first available color
                    return GetAvailableColorRecursive(0);

                    static int GetAvailableColorRecursive(int index)
                    {
                        foreach(var player in players.Values)
                            if(player.colorIndex.Value == index)
                                return GetAvailableColorRecursive(++index);

                        return index;
                    }
                }
            }

            return desirableIndex;
        }
    }
}
