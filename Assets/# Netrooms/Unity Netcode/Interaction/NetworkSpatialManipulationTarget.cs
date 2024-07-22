using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkSpatialManipulationTarget : NetworkBehaviour
    {
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

        public bool isLocalPlayerAllowedToManipulate = true;

        public bool IsBeingManipulated => isBeingManipulated.Value;

        public ulong ManipulationClientId => manipulationClientId.Value;

        public bool IsManipulationClientLocal => ManipulationClientId == NetworkPlayer.Local.OwnerClientId;

        [SerializeField]
        private float positionThreshold = 0.001f;

        [SerializeField]
        private float rotationThreshold = 0.01f;

        [SerializeField]
        private float scaleThreshold = 0.01f;

        [Space(8)]
        [SerializeField]
        private float interpolationTime = 0.2f;

        private readonly NetworkVariable<bool> isBeingManipulated = new(false);
        private readonly NetworkVariable<ulong> manipulationClientId = new();

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 targetScale;

        private Vector3 originPosition;
        private Quaternion originRotation;
        private Vector3 originScale;

        private float positionTimer = -1;
        private float rotationTimer = -1;
        private float scaleTimer = -1;

        public bool switchManipulationTest;

        private void OnValidate()
        {
            if(switchManipulationTest)
            {
                switchManipulationTest = false;

                if(IsBeingManipulated)
                    EndManipulation();
                else
                    BeginManipulation();
            }
        }

        public void BeginManipulation() 
        {
            if(IsBeingManipulated)   
            {
                Debug.LogWarning($"Attempted to manipulate a blocked manipulation target.");
                return;
            }

            if(!isLocalPlayerAllowedToManipulate)
            {
                Debug.LogWarning($"Local client is not allowed to manipualte.");
                return;
            }

            BeginManipulationServerRpc(NetworkPlayer.Local.OwnerClientId);
        }

        public void EndManipulation() 
        {
            if(IsBeingManipulated && IsManipulationClientLocal)
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

                NetworkManager.Singleton.OnClientConnectedCallback += ServerHandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += ServerHandleClientDisconnected;
            }
        }

        public override void OnNetworkDespawn()
        {
            if(IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= ServerHandleClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback -= ServerHandleClientDisconnected;
            }
        }

        private void ServerHandleClientConnected(ulong client)
        {
            RpcParams p = RpcTarget.Single(client, RpcTargetUse.Temp);
            InitializeTransformRpc(targetPosition, targetRotation, targetScale, p);
        }

        private void ServerHandleClientDisconnected(ulong client)
        {
            if(IsBeingManipulated && client == ManipulationClientId)
            {
                ServerEndManipulation();
            }
        }

        [Rpc(SendTo.Server)]
        private void BeginManipulationServerRpc(ulong player)
        {
            if(isBeingManipulated.Value)
            {
                return;
            }

            isBeingManipulated.Value = true;
            manipulationClientId.Value = player;
        }

        [Rpc(SendTo.Server)]
        private void EndManipulationServerRpc()
        {
            if(!isBeingManipulated.Value)
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

        private void Update()
        {
            if(!IsSpawned)
                return;

            if(IsBeingManipulated && IsManipulationClientLocal) // Local manipulation
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
            isBeingManipulated.Value = false;
            manipulationClientId.Value = 0;
        }
    }
}