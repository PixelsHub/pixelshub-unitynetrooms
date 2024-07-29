using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class NetworkInteractable : NetworkBehaviour
    {
        [Serializable]
        protected struct NetworkSelect : INetworkSerializable
        {
            public bool isSelected;
            public ulong clientId;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref isSelected);
                serializer.SerializeValue(ref clientId);
            }
        }

        public event Action<NetworkPlayer> OnSelectStarted;
        public event Action<NetworkPlayer> OnSelectEnded;

        public event Action<bool> OnLocalPlayerAllowedToSelectChanged;

        public bool IsLocalPlayerAllowedToSelect
        {
            get => isLocalPlayerAllowedToSelect;
            set
            {
                if(isLocalPlayerAllowedToSelect != value)
                {
                    isLocalPlayerAllowedToSelect = value;
                    OnLocalPlayerAllowedToSelectChanged?.Invoke(value);
                }
            }
        }

        public bool IsSelected => interaction.Value.isSelected;

        public bool IsBlocked => interaction.Value.isSelected;

        public ulong InteractionClientId => interaction.Value.clientId;

        public bool IsInteractionClientLocal => InteractionClientId == NetworkPlayer.Local.OwnerClientId;

        public bool IsSelectedByLocalPlayer => IsSelected && IsInteractionClientLocal;

        public XRBaseInteractable Interactable 
        {
            get 
            {
                if(interactable == null)
                    interactable = GetComponent<XRBaseInteractable>();

                return interactable;
            }
        }

        protected XRBaseInteractable interactable;

        [SerializeField]
        private bool isLocalPlayerAllowedToSelect = true;

        protected readonly NetworkVariable<NetworkSelect> interaction = new();

        public override void OnNetworkSpawn()
        {
            interactable.selectEntered.AddListener(HandleSelectEntered);
            interactable.selectExited.AddListener(HandleSelectExited);

            interaction.OnValueChanged += HandleInteractionValueChanged;

            if(IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += TryEndCurrentSelectByClient;
            }
        }

        public override void OnNetworkDespawn()
        {
            interactable.selectEntered.RemoveListener(HandleSelectEntered);
            interactable.selectExited.RemoveListener(HandleSelectExited);

            interaction.OnValueChanged -= HandleInteractionValueChanged;

            if(interaction.Value.isSelected && interaction.Value.clientId == OwnerClientId)
            {
                NetworkPlayer.Players.TryGetValue(OwnerClientId, out var player);
                OnSelectEnded.Invoke(player);
            }

            if(IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= TryEndCurrentSelectByClient;
            }
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if(IsBlocked)
                return;

            if(!isLocalPlayerAllowedToSelect)
            {
                Debug.LogWarning($"Local client is not allowed to select.");
                return;
            }

            InteractionSelectServerRpc(NetworkPlayer.Local.OwnerClientId);
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if(IsSelectedByLocalPlayer)
                EndSelectionServerRpc();
        }

        private void HandleInteractionValueChanged(NetworkSelect prevValue, NetworkSelect newValue)
        {
            string PlayerNotFoundLog() => $"Could not find player (id={newValue.clientId}).";

            if(prevValue.isSelected)
            {
                if(!NetworkPlayer.Players.TryGetValue(newValue.clientId, out var player))
                    Debug.LogError(PlayerNotFoundLog());

                foreach(var c in interactable.colliders)
                    c.enabled = true;

                OnSelectEnded?.Invoke(player);
            }

            if(newValue.isSelected)
            {
                if(!NetworkPlayer.Players.TryGetValue(newValue.clientId, out var player))
                    Debug.LogError(PlayerNotFoundLog());
                
                foreach(var c in interactable.colliders)
                    c.enabled = false;
                
                OnSelectStarted?.Invoke(player);
            }
            else if(newValue.clientId != prevValue.clientId)
            {
                if(!NetworkPlayer.Players.TryGetValue(newValue.clientId, out var player))
                    Debug.LogError(PlayerNotFoundLog());

                foreach(var c in interactable.colliders)
                    c.enabled = true;

                OnSelectEnded?.Invoke(player);
            }
        }

        [Rpc(SendTo.Server)]
        private void InteractionSelectServerRpc(ulong clientId)
        {
            if(interaction.Value.isSelected)
            {
                Debug.LogWarning($"Received select request for blocked object from player ({clientId}). This is likely a race condition.");
                return;
            }

            var value = interaction.Value;
            value.isSelected = true;
            value.clientId = clientId;
            interaction.Value = value;
        }

        [Rpc(SendTo.Server)]
        private void EndSelectionServerRpc()
        {
            if(!IsSelected)
            {
                Debug.Assert(false);
                return;
            }

            ServerEndSelect();
        }

        private void TryEndCurrentSelectByClient(ulong client)
        {
            if(IsSelected && client == InteractionClientId)
                ServerEndSelect();
        }

        private void ServerEndSelect()
        {
            var value = interaction.Value;
            value.isSelected = false;
            interaction.Value = value;
        }
    }
}