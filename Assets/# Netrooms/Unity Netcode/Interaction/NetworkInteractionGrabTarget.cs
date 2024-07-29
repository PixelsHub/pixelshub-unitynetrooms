using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class NetworkInteractionGrabTarget : NetworkInteractable
    {
        [Space(8)]
        [SerializeField]
        private float positionThreshold = 0.001f;

        [SerializeField]
        private float rotationThreshold = 0.01f;

        [SerializeField]
        private float scaleThreshold = 0.01f;

        [Space(8)]
        [SerializeField]
        private float interpolationTime = 0.12f;

        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 targetScale;

        private Vector3 originPosition;
        private Quaternion originRotation;
        private Vector3 originScale;

        private float positionTimer = -1;
        private float rotationTimer = -1;
        private float scaleTimer = -1;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(IsServer)
            {
                targetPosition = transform.localPosition;
                targetRotation = transform.localRotation;
                targetScale = transform.localScale;

                NetworkManager.Singleton.OnClientConnectedCallback += InitializeConnectedClient;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if(IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= InitializeConnectedClient;
            }
        }

        private void LateUpdate()
        {
            if(!IsSpawned)
                return;

            if(IsSelectedByLocalPlayer)
                ProcessLocalGrabTransformation();
            else
                SetTargetTransformations();
        }

        private void InitializeConnectedClient(ulong client)
        {
            RpcParams p = RpcTarget.Single(client, RpcTargetUse.Temp);
            InitializeTransformRpc(targetPosition, targetRotation, targetScale, p);
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

        private void ProcessLocalGrabTransformation() 
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

        private void SetTargetTransformations() 
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
}