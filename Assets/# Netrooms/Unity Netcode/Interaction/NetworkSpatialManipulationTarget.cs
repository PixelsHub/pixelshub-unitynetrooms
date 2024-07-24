using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkSpatialManipulationTarget : NetworkBehaviour
    {
        [Serializable]
        private struct NetworkInteraction : INetworkSerializable
        {
            public bool isSelected;
            public ulong clientId;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref isSelected);
                serializer.SerializeValue(ref clientId);
            }
        }

        public event Action<bool> OnLocalPlayerAllowedToManipulateChanged;

        public bool IsLocalPlayerAllowedToManipulate
        {
            get => isLocalPlayerAllowedToManipulate;
            set
            {
                if(isLocalPlayerAllowedToManipulate == value)
                {
                    isLocalPlayerAllowedToManipulate = value;
                    OnLocalPlayerAllowedToManipulateChanged?.Invoke(value);
                }
            }
        }

        [HideInInspector]
        public bool isLocalPlayerAllowedToManipulate = true;

        public bool IsSelected => interaction.Value.isSelected;

        public bool IsBlocked => interaction.Value.isSelected;

        public ulong InteractionClientId => interaction.Value.clientId;

        public bool IsInteractionClientLocal => InteractionClientId == NetworkPlayer.Local.OwnerClientId;

        public bool IsSelectedByLocalPlayer => IsSelected && IsInteractionClientLocal;

        [SerializeField]
        private XRBaseInteractable interactable;

        [Space(8)]
        [SerializeField]
        private float positionThreshold = 0.001f;

        [SerializeField]
        private float rotationThreshold = 0.01f;

        [SerializeField]
        private float scaleThreshold = 0.01f;

        [Space(8)]
        [SerializeField]
        private float interpolationTime = 0.2f;

        private readonly NetworkVariable<NetworkInteraction> interaction = new();

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 targetScale;

        private Vector3 originPosition;
        private Quaternion originRotation;
        private Vector3 originScale;

        private float positionTimer = -1;
        private float rotationTimer = -1;
        private float scaleTimer = -1;

        public void BeginManipulation() 
        {
            if(IsSelected)
            {
                Debug.LogWarning($"Attempted to manipulate a blocked manipulation target.");
                return;
            }

            if(!isLocalPlayerAllowedToManipulate)
            {
                Debug.LogWarning($"Local client is not allowed to manipualte.");
                return;
            }

            InteractionSelectServerRpc(NetworkPlayer.Local.OwnerClientId);
        }

        public void EndManipulation() 
        {
            if(IsSelectedByLocalPlayer)
            {
                EndManipulationServerRpc();
            }
        }

        public override void OnNetworkSpawn()
        {
            if(IsServer)
            {
                targetPosition = transform.localPosition;
                targetRotation = transform.localRotation;
                targetScale = transform.localScale;

                NetworkManager.Singleton.OnClientConnectedCallback += InitializeConnectedClient;
                NetworkManager.Singleton.OnClientDisconnectCallback += TryEndCurrentManipulationByClient;
            }

            if(IsClient)
            {
                interactable.selectEntered.AddListener(HandleSelectEntered);
                interactable.selectExited.AddListener(HandleSelectExited);
                interactable.hoverEntered.AddListener(HandleHoverEntered);
                interactable.hoverExited.AddListener(HandleHoverExited);
            }
        }

        public override void OnNetworkDespawn()
        {
            if(IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= InitializeConnectedClient;
                NetworkManager.Singleton.OnClientDisconnectCallback -= TryEndCurrentManipulationByClient;
            }
        }

        private void InitializeConnectedClient(ulong client)
        {
            RpcParams p = RpcTarget.Single(client, RpcTargetUse.Temp);
            InitializeTransformRpc(targetPosition, targetRotation, targetScale, p);
        }

        private void TryEndCurrentManipulationByClient(ulong client)
        {
            if(IsSelected && client == InteractionClientId)
                ServerEndManipulation();
        }

        private void HandleSelectEntered(SelectEnterEventArgs args)
        {
            if(IsBlocked)
                return;

            BeginManipulation();
        }

        private void HandleSelectExited(SelectExitEventArgs args)
        {
            if(!IsSelected)
                return;

            if(IsInteractionClientLocal)
            {
                EndManipulation();
            }
        }

        private void HandleHoverEntered(HoverEnterEventArgs args)
        {

        }

        private void HandleHoverExited(HoverExitEventArgs args)
        {

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
        private void EndManipulationServerRpc()
        {
            if(!IsSelected)
            {
                Debug.Assert(false);
                return;
            }

            ServerEndManipulation();
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void InitializeTransformRpc(Vector3 position, Quaternion rotation, Vector3 scale, RpcParams _)
        {
            targetPosition = position;
            targetRotation = rotation;
            targetScale = scale;

            transform.SetLocalPositionAndRotation(targetPosition, targetRotation);
            transform.localScale = targetScale;
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicatePositionRpc(Vector3 position) 
        {
            originPosition = transform.localPosition;
            targetPosition = position;
            positionTimer = interpolationTime;
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateRotationRpc(Quaternion rotation)
        {
            originRotation = transform.localRotation;
            targetRotation = rotation;
            rotationTimer = interpolationTime;
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateScaleRpc(Vector3 scale)
        {
            originScale = transform.localScale;
            targetScale = scale;
            scaleTimer = interpolationTime;
        }

        private void LateUpdate()
        {
            if(!IsSpawned)
                return;

            if(IsSelectedByLocalPlayer) // Local manipulation
            {
                if(Vector3.Distance(targetPosition, transform.localPosition) > positionThreshold)
                {
                    targetPosition = transform.localPosition;
                    ReplicatePositionRpc(targetPosition);
                }

                if(Quaternion.Angle(targetRotation, transform.localRotation) > rotationThreshold)
                {
                    targetRotation = transform.localRotation;
                    ReplicateRotationRpc(targetRotation);
                }

                if(Vector3.Distance(targetScale, transform.localScale) > scaleThreshold)
                {
                    targetScale = transform.localScale;
                    ReplicateScaleRpc(targetScale);
                }
            }
            else
            {
                if(positionTimer >= 0)
                {
                    positionTimer -= Time.unscaledDeltaTime;
                    float t = 1 - (positionTimer / interpolationTime);
                    transform.localPosition = Vector3.Lerp(originPosition, targetPosition, t);
                }
                else
                    transform.localPosition = targetPosition;

                if(rotationTimer >= 0)
                {
                    rotationTimer -= Time.unscaledDeltaTime;
                    float t = 1 - (rotationTimer / interpolationTime);
                    transform.localRotation = Quaternion.Slerp(originRotation, targetRotation, t);
                }
                else
                    transform.localRotation = targetRotation;

                if(scaleTimer >= 0)
                {
                    scaleTimer -= Time.unscaledDeltaTime;
                    float t = 1 - (scaleTimer / interpolationTime);
                    transform.localScale = Vector3.Lerp(originScale, targetScale, t);
                }
                else
                    transform.localScale = targetScale;
            }
        }

        private void ServerEndManipulation()
        {
            var value = interaction.Value;
            value.isSelected = false;
            value.clientId = 0;
            interaction.Value = value;
        }
    }
}