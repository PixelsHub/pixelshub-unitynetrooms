using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    [RequireComponent(typeof(XRGrabInteractable))]
    public class NetworkGrabInteractable : NetworkInteractable
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

        private bool isWorldOriginLocked;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(transform.parent != NetworkWorldOrigin.Transform)
            {
                transform.SetParent(NetworkWorldOrigin.Transform);
                Debug.LogWarning($"Object {name} was not spawned as chlid of world origin. Forcing parenting...");
            }

            if(IsServer)
            {
                targetPosition = transform.localPosition;
                targetRotation = transform.localRotation;
                targetScale = transform.localScale;

                NetworkManager.Singleton.OnClientConnectedCallback += InitializeTransformOnConnectedClient;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if(IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= InitializeTransformOnConnectedClient;
            }

            if(isWorldOriginLocked)
            {
                isWorldOriginLocked = false;
                NetworkWorldOrigin.RemoveLockTransformationRequest(this);
            }
        }

        private void InitializeTransformOnConnectedClient(ulong client)
        {
            RpcParams p = RpcTarget.Single(client, RpcTargetUse.Temp);
            InitializeTransformOnClientRpc(targetPosition, targetRotation, targetScale, p);
        }

        protected override void Awake()
        {
            base.Awake();

            // Interactables must always return to their previous parent (world origin)
            if(interactable is XRGrabInteractable grabInteractable)
                grabInteractable.retainTransformParent = true;
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

        protected override void StartLocalPlayerSelect()
        {
            if(!isWorldOriginLocked)
            {
                isWorldOriginLocked = true;
                NetworkWorldOrigin.AddLockTransformationRequest(this);
            }
        }

        protected override void EndLocalPlayerSelect()
        {
            if(isWorldOriginLocked)
            {
                isWorldOriginLocked = false;
                NetworkWorldOrigin.RemoveLockTransformationRequest(this);
            }
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void InitializeTransformOnClientRpc(Vector3 position, Quaternion rotation, Vector3 scale, RpcParams _)
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
            var relative = NetworkWorldOrigin.WorldToLocal(new(transform.position, transform.rotation, transform.lossyScale));

            if(Vector3.Distance(targetPosition, relative.position) > positionThreshold)
            {
                targetPosition = relative.position;
                ReplicatePositionRpc(targetPosition);
            }

            if(Quaternion.Angle(targetRotation, relative.rotation) > rotationThreshold)
            {
                targetRotation = relative.rotation;
                ReplicateRotationRpc(targetRotation);
            }

            if(Vector3.Distance(targetScale, relative.scale) > scaleThreshold)
            {
                targetScale = relative.scale;
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