using System;
using System.Collections.Generic;
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

        public static event Action<NetworkInteractable> OnInteractableCreated;
        public static event Action<NetworkInteractable> OnInteractableDestroyed;

        public event Action OnDestroyed;

        public event Action<NetworkPlayer> OnSelectStarted;
        public event Action<NetworkPlayer> OnSelectEnded;
        public event Action<NetworkPlayer> OnHoverStarted;
        public event Action<NetworkPlayer> OnHoverEnded;

        public event Action<bool> OnLocalPlayerAllowedToSelectChanged;

        public static IReadOnlyList<NetworkInteractable> Interactables => interactables;

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

        public bool IsSelected => selection.Value.isSelected;

        public ulong SelectionClientId => selection.Value.clientId;

        public bool IsSelectionClientLocal => SelectionClientId == NetworkPlayer.Local.OwnerClientId;

        public bool IsSelectedByLocalPlayer => IsSelected && IsSelectionClientLocal;

        public XRBaseInteractable Interactable => interactable;

        protected XRBaseInteractable interactable;

        private static readonly List<NetworkInteractable> interactables = new();

        [SerializeField]
        private bool isLocalPlayerAllowedToSelect = true;

        protected readonly NetworkVariable<NetworkSelect> selection = new();
        protected readonly NetworkList<ulong> hovers = new();

        public override void OnNetworkSpawn()
        {
            interactable.selectEntered.AddListener(HandleSelectEntered);
            interactable.selectExited.AddListener(HandleSelectExited);
            interactable.hoverEntered.AddListener(HandleHoverEntered);
            interactable.hoverExited.AddListener(HandleHoverExited);

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
            interactable.hoverEntered.RemoveListener(HandleHoverEntered);
            interactable.hoverExited.RemoveListener(HandleHoverExited);

            OnSelectStarted = null;
            OnSelectEnded = null;
            OnHoverStarted = null;
            OnHoverEnded = null;

            selection.OnValueChanged -= HandleInteractionValueChanged;

            if(selection.Value.isSelected && selection.Value.clientId == OwnerClientId)
            {
                NetworkPlayer.Players.TryGetValue(OwnerClientId, out var player);
                OnSelectEnded.Invoke(player);
            }

            if(IsServer)
            {
                NetworkManager.Singleton.OnClientDisconnectCallback -= TryEndCurrentSelectByClient;

                foreach(ulong hover in hovers)
                    NotifyHoverEndedRpc(hover);
            }
        }

        private void Awake()
        {
            if(interactable == null)
                TryGetComponent(out interactable);

            OnInteractableCreated?.Invoke(this);
            interactables.Add(this);
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            OnDestroyed?.Invoke();
            OnDestroyed = null;

            OnInteractableDestroyed?.Invoke(this);
            interactables.Remove(this);
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

            ServerStartSelectRpc(NetworkPlayer.Local.OwnerClientId);
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if(IsSelectedByLocalPlayer)
                ServerEndSelectRpc();
        }

        private void HandleHoverEntered(HoverEnterEventArgs args)
        {
            ServerStartHoverRpc(NetworkPlayer.Local.OwnerClientId);
        }

        private void HandleHoverExited(HoverExitEventArgs args)
        {
            ServerEndHoverRpc(NetworkPlayer.Local.OwnerClientId);
        }

        private static NetworkPlayer GetPlayer(ulong clientId)
        {
            if(!NetworkPlayer.Players.TryGetValue(clientId, out var player))
                Debug.LogError($"Could not find player (id={clientId}).");
            return player;
        }

        private void HandleInteractionValueChanged(NetworkSelect prevValue, NetworkSelect newValue)
        {
            if(prevValue.isSelected)
            {
                foreach(var c in interactable.colliders)
                    c.enabled = true;

                OnSelectEnded?.Invoke(GetPlayer(newValue.clientId));
            }

            if(newValue.isSelected)
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
        private void ServerStartSelectRpc(ulong clientId)
        {
            if(selection.Value.isSelected)
            {
                Debug.LogWarning($"Received select request for blocked object from player ({clientId}). This is likely a race condition.");
                return;
            }

            var value = selection.Value;
            value.isSelected = true;
            value.clientId = clientId;
            selection.Value = value;
        }

        [Rpc(SendTo.Server)]
        private void ServerEndSelectRpc()
        {
            if(!IsSelected)
            {
                Debug.Assert(false);
                return;
            }

            ServerEndSelect();
        }

        [Rpc(SendTo.Server)]
        private void ServerStartHoverRpc(ulong clientId)
        {
            hovers.Add(clientId);
            NotifyHoverStartedRpc(clientId);
        }

        [Rpc(SendTo.Server)]
        private void ServerEndHoverRpc(ulong clientId)
        {
            hovers.Remove(clientId);
            NotifyHoverEndedRpc(clientId);
        }

        [Rpc(SendTo.Everyone)]
        private void NotifyHoverStartedRpc(ulong clientId)
        {
            OnHoverStarted?.Invoke(GetPlayer(clientId));
        }

        [Rpc(SendTo.Everyone)]
        private void NotifyHoverEndedRpc(ulong clientId)
        {
            OnHoverEnded?.Invoke(GetPlayer(clientId));
        }

        private void TryEndCurrentSelectByClient(ulong client)
        {
            if(IsSelected && client == SelectionClientId)
                ServerEndSelect();
        }

        private void ServerEndSelect()
        {
            var value = selection.Value;
            value.isSelected = false;
            selection.Value = value;
        }
    }
}