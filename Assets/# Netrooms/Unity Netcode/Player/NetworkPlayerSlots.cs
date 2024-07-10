using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkPlayerSlots : NetworkBehaviour
    {
        private struct PlayerSlot : INetworkSerializable, IEquatable<PlayerSlot>
        {
            public ulong ownerClientId;
            public bool isConnected;

            public PlayerSlot(ulong ownerClientId, bool isConnected)
            {
                this.ownerClientId = ownerClientId;
                this.isConnected = isConnected;
            }

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref ownerClientId);
                serializer.SerializeValue(ref isConnected);
            }

            public readonly bool Equals(PlayerSlot other)
            {
                return ownerClientId == other.ownerClientId
                    && isConnected == other.isConnected;
            }
        }

        public static NetworkPlayerSlots Instance { get; private set; }

        private readonly NetworkList<PlayerSlot> playerSlots = new();

        public ulong GetPlayerId(int index)
        {
            return playerSlots[index].ownerClientId;
        }

        public NetworkPlayer GetPlayer(int index)
        {
            var slot = playerSlots[index];

            if(!slot.isConnected)
                return null;

            return NetworkPlayer.Players[slot.ownerClientId];
        }

        public bool TryGetPlayer(int index, out NetworkPlayer player)
        {
            if(index >= 0 && index < playerSlots.Count - 1)
            {
                var slot = playerSlots[index];

                if(slot.isConnected)
                    return NetworkPlayer.Players.TryGetValue(slot.ownerClientId, out player);
            }

            player = null;
            return false;
        }

        /// <summary>
        /// Get a list with players in the slots. Players who have disconnected will appear as null.
        /// </summary>
        public List<NetworkPlayer> GetPlayers()
        {
            List<NetworkPlayer> result = new(playerSlots.Count);

            for(int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];

                if(slot.isConnected)
                {
                    if(NetworkPlayer.Players.TryGetValue(slot.ownerClientId, out var player))
                        result.Add(player);
                    else
                    {
                        result.Add(null);
                        Debug.LogError($"Missing player (Id={slot.ownerClientId})");
                    }
                }
                else
                    result.Add(null);
            }

            return result;
        }

        public override void OnNetworkSpawn()
        {
            if(Instance == null)
            {
                Instance = this;

                if(IsServer)
                {
                    if(NetworkPlayer.Players.Count > 0)
                        foreach(var player in NetworkPlayer.Players.Values)
                            HandlePlayerSpawned(player);

                    NetworkPlayer.OnPlayerSpawned += HandlePlayerSpawned;
                    NetworkPlayer.OnPlayerDespawned += HandlePlayerDespawned;
                }
            }
            else
                Debug.Assert(false, "Onle one instance of NetworkRoom is expected.");
        }

        public override void OnNetworkDespawn()
        {
            if(Instance == this)
            {
                Instance = null;

                if(IsServer)
                {
                    playerSlots.Clear();

                    NetworkPlayer.OnPlayerSpawned -= HandlePlayerSpawned;
                    NetworkPlayer.OnPlayerDespawned -= HandlePlayerDespawned;
                }
            }
        }

        public void HandlePlayerSpawned(NetworkPlayer player)
        {
            Debug.Assert(IsServer);

            PlayerSlot playerSlot = new(player.OwnerClientId, true);

            if(!TrySetPlayerAtExistingEmptySlot(playerSlot, out int playerIndex))
                playerSlots.Add(playerSlot);
            
            player.AssignColor(PlayerColoringScheme.GetColor(playerIndex));
        }

        public void HandlePlayerDespawned(NetworkPlayer player)
        {
            Debug.Assert(IsServer);

            for(int i = 0; i < playerSlots.Count; i++)
            {
                if(playerSlots[i].ownerClientId == player.OwnerClientId)
                {
                    playerSlots[i] = new(player.OwnerClientId, false);
                    return;
                }
            }

            Debug.LogError($"Despawned player (Id={player.OwnerClientId}) could not be found at any player slot.");
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CheckComponentExistsOnScene() 
        {
            if(FindFirstObjectByType<NetworkManager>() != null && FindFirstObjectByType<NetworkPlayerSlots>() == null)
            {
                Debug.LogError("A Networking scene should include a NetworkPlayerSlots component.");
            }
        }
#endif

        private bool TrySetPlayerAtExistingEmptySlot(PlayerSlot playerSlot, out int playerIndex)
        {
            for(int i = 0; i < playerSlots.Count; i++)
            {
                if(!playerSlots[i].isConnected)
                {
                    playerIndex = i;
                    playerSlots[i] = playerSlot;
                    return true;
                }
            }

            playerIndex = playerSlots.Count;
            return false;
        }
    }
}
