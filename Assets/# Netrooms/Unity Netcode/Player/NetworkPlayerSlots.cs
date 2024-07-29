using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public struct PlayerSlot : INetworkSerializable, IEquatable<PlayerSlot>
    {
        public ulong playerId;
        public bool isConnected;

        public PlayerSlot(ulong playerId, bool isConnected)
        {
            this.playerId = playerId;
            this.isConnected = isConnected;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref playerId);
            serializer.SerializeValue(ref isConnected);
        }

        public readonly bool Equals(PlayerSlot o) => playerId == o.playerId && isConnected == o.isConnected;
    }

    public class NetworkPlayerSlots : NetworkPersistenSingletonRequired<NetworkPlayerSlots>
    {
        public const string limitReachedReason = "PLAYER_LIMIT_REACHED";

        public const int maximumAmount = 24;

        public int Count => playerSlots.Count;

        public IEnumerator<PlayerSlot> PlayerSlots => playerSlots.GetEnumerator();

        private readonly NetworkList<PlayerSlot> playerSlots = new();

        [SerializeField, Range(2, maximumAmount)]
        private int availableSlots = 12;

        public void SetAvailableSlots(int availableSlots)
        {
            if(!IsServer)
            {
                Debug.Assert(false);
                return;
            }

            if(availableSlots == playerSlots.Count)
                return;

            if(availableSlots < 2)
            {
                this.availableSlots = 2;
                Debug.LogError("Cannot set a slot count lower than 2");
            }
            else if(availableSlots > maximumAmount)
            {
                this.availableSlots = maximumAmount;
                Debug.LogError($"Cannot set a slot count higher than {maximumAmount}");
            }
            else
                this.availableSlots = availableSlots;

            ProcessNewAvailableSlotAmount();
        }

        public ulong GetPlayerId(int index)
        {
            return playerSlots[index].playerId;
        }

        public NetworkPlayer GetPlayer(int index)
        {
            var slot = playerSlots[index];

            if(!slot.isConnected)
                return null;

            return NetworkPlayer.Players[slot.playerId];
        }

        /// <summary>
        /// Returns the index of the given player. Will return -1 if player not found.
        /// </summary>
        public int GetPlayerIndex(NetworkPlayer player) => GetPlayerIndex(player.OwnerClientId);

        /// <summary>
        /// Returns the index of the given player. Will return -1 if player not found.
        /// </summary>
        public int GetPlayerIndex(ulong playerId)
        {
            for(int i = 0; i < playerSlots.Count; i++)
                if(playerSlots[i].isConnected && playerSlots[i].playerId == playerId)
                    return i;

            return -1;
        }

        public bool TryGetPlayer(int index, out NetworkPlayer player)
        {
            if(index >= 0 && index < playerSlots.Count - 1)
            {
                var slot = playerSlots[index];

                if(slot.isConnected)
                    return NetworkPlayer.Players.TryGetValue(slot.playerId, out player);
            }

            player = null;
            return false;
        }

        /// <summary>
        /// Get a list with players in the slots. Empty slots will appear as null.
        /// </summary>
        public List<NetworkPlayer> GetPlayers()
        {
            List<NetworkPlayer> result = new(playerSlots.Count);

            for(int i = 0; i < playerSlots.Count; i++)
            {
                var slot = playerSlots[i];

                if(slot.isConnected)
                {
                    if(NetworkPlayer.Players.TryGetValue(slot.playerId, out var player))
                        result.Add(player);
                    else
                    {
                        result.Add(null);
                        Debug.LogError($"Missing player (Id={slot.playerId})");
                    }
                }
                else
                    result.Add(null);
            }

            return result;
        }

        public override void OnNetworkSpawn()
        {
            Debug.Assert(Instance == this);

            if(IsServer)
            {
                playerSlots.Clear();

                for(int i = 0; i < maximumAmount; i++)
                    playerSlots.Add(new());
            }
        }

        public bool TryAssignPlayerSlot(NetworkPlayer player, out int slotIndex)
        {
            Debug.Assert(IsServer);

            PlayerSlot playerSlot = new(player.OwnerClientId, true);

            if(!TrySetPlayerAtExistingEmptySlot(playerSlot, out slotIndex))
                playerSlots.Add(playerSlot);

            return true;
        }

        public void RemovePlayerFromSlot(NetworkPlayer player)
        {
            Debug.Assert(IsServer);

            for(int i = 0; i < playerSlots.Count; i++)
            {
                if(playerSlots[i].playerId == player.OwnerClientId)
                {
                    var slot = playerSlots[i];
                    slot.isConnected = false;
                    playerSlots[i] = slot;
                    return;
                }
            }

            Debug.LogError($"Player (Id={player.OwnerClientId}) could not be found at any player slot for removal.");
        }

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

        private void ProcessNewAvailableSlotAmount()
        {
            Debug.Assert(IsServer);

            if(!IsSpawned)
            {
                Debug.Assert(playerSlots.Count == 0);
                return;
            }

            if(availableSlots > playerSlots.Count)
            {
                while(playerSlots.Count < availableSlots)
                    playerSlots.Add(new());
            }
            else
            {
                Queue<int> emptySlots = EnqueueEmptySlots();

                // Remaining players fill empty slots or are kicked
                while(playerSlots.Count > availableSlots)
                {
                    int i = availableSlots;

                    if(playerSlots[i].isConnected)
                    {
                        if(emptySlots.Count > 0)
                        {
                            int newIndex = emptySlots.Dequeue();
                            playerSlots[newIndex] = playerSlots[i];
                        }
                        else if(NetworkPlayer.Players.TryGetValue(playerSlots[i].playerId, out var player))
                            player.Kick();
                    }

                    playerSlots.RemoveAt(i);
                }
            }
        }

        private Queue<int> EnqueueEmptySlots() 
        {
            Queue<int> emptySlots = new();
            for(int i = 0; i < availableSlots; i++)
            {
                if(!playerSlots[i].isConnected)
                    emptySlots.Enqueue(i);
            }

            return emptySlots;
        }
    }
}
