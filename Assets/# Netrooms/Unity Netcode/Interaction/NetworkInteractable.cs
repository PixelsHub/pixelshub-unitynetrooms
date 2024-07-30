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
        protected struct NetworkInteraction : INetworkSerializable
        {
            public bool isActive;
            public ulong clientId;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref isActive);
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

        public bool IsSelected => selection.Value.isActive;

        public ulong SelectionClientId => selection.Value.clientId;

        public bool IsSelectionClientLocal => SelectionClientId == NetworkPlayer.Local.OwnerClientId;

        public bool IsSelectedByLocalPlayer => IsSelected && IsSelectionClientLocal;

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

        protected readonly NetworkVariable<NetworkInteraction> selection = new();
        protected readonly NetworkVariable<NetworkInteraction> hover = new();

        public override void OnNetworkSpawn()
        {
            interactable.selectEntered.AddListener(HandleSelectEntered);
            interactable.selectExited.AddListener(HandleSelectExited);

            selection.OnValueChanged += HandleInteractionValueChanged;

            if(IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback += TryEndCurrentSelectByClient;
            }
        }

        public override void OnNetworkDespawn()
        {
            interactable.selectEntered.RemoveListener(HandleSelectEntered);
            interactable.selectExited.RemoveListener(HandleSelectExited);

            selection.OnValueChanged -= HandleInteractionValueChanged;

            if(selection.Value.isActive && selection.Value.clientId == OwnerClientId)
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
            if(IsSelected)
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

        private void HandleInteractionValueChanged(NetworkInteraction prevValue, NetworkInteraction newValue)
        {
            static NetworkPlayer GetPlayer(ulong clientId)
            {
                if(!NetworkPlayer.Players.TryGetValue(clientId, out var player))
                    Debug.LogError($"Could not find player (id={clientId}).");
                return player;
            }

            if(prevValue.isActive)
            {
                foreach(var c in interactable.colliders)
                    c.enabled = true;

                OnSelectEnded?.Invoke(GetPlayer(newValue.clientId));
            }

            if(newValue.isActive)
            {
                foreach(var c in interactable.colliders)
                    c.enabled = false;
                
                OnSelectStarted?.Invoke(GetPlayer(newValue.clientId));
            }
            else if(newValue.clientId != prevValue.clientId)
            {
                foreach(var c in interactable.colliders)
                    c.enabled = true;

                OnSelectEnded?.Invoke(GetPlayer(newValue.clientId));
            }
        }

        [Rpc(SendTo.Server)]
        private void InteractionSelectServerRpc(ulong clientId)
        {
            if(selection.Value.isActive)
            {
                Debug.LogWarning($"Received select request for blocked object from player ({clientId}). This is likely a race condition.");
                return;
            }

            var value = selection.Value;
            value.isActive = true;
            value.clientId = clientId;
            selection.Value = value;
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
            if(IsSelected && client == SelectionClientId)
                ServerEndSelect();
        }

        private void ServerEndSelect()
        {
            var value = selection.Value;
            value.isActive = false;
            selection.Value = value;
        }
    }
}