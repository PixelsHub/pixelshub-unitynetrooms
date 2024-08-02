using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    /// <summary>
    /// Synchronizes a XRGrabInteractable across all connected clients.
    /// <para>
    /// IMPORTANT: Remember to take into account the relative transformation towards world origin.
    /// </para>
    /// </summary>
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

        // Flag to ensure correctness in world origin lock/unlock requests
        private bool isWorldOriginLocked;

        /// <summary>
        /// Sets the target transformation of the interactable on self and all clients, if not selected by any user.
        /// Always local to world origin.
        /// </summary>
        public bool TrySetLocalTransformation(Transformation transformation, bool interpolate = false)
        {
            if(IsSelected)
                return false;

            originPosition = transform.localPosition;
            targetPosition = transformation.position;

            originRotation = transform.localRotation;
            targetRotation = transformation.rotation;

            originScale = transform.localScale;
            targetScale = transformation.scale;

            float timer = interpolate ? interpolationTime : 0;

            positionTimer = timer;
            rotationTimer = timer;
            scaleTimer = timer;

            ReplicateTransformationRpc(transformation.position, transformation.rotation, transformation.scale);
            return true;
        }

        /// <summary>
        /// Sets the target position of the interactable on self and all clients, if not selected by any user.
        /// Always local to world origin.
        /// </summary>
        public bool TrySetLocalPosition(Vector3 position, bool interpolate = false)
        {
            if(IsSelected)
                return false;

            originPosition = transform.localPosition;
            targetPosition = position;
            positionTimer = interpolate ? interpolationTime : 0;

            ReplicatePositionRpc(position);
            return true;
        }

        /// <summary>
        /// Sets the target rotation of the interactable on self and all clients, if not selected by any user.
        /// Always local to world origin.
        /// </summary>
        public bool TrySetLocalRotation(Quaternion rotation, bool interpolate = false)
        {
            if(IsSelected)
                return false;

            originRotation = transform.localRotation;
            targetRotation = rotation;
            rotationTimer = interpolate ? interpolationTime : 0;

            ReplicateRotationRpc(rotation);
            return true;
        }

        /// <summary>
        /// Sets the target scale of the interactable on self and all clients, if not selected by any user.
        /// Always local to world origin.
        /// </summary>
        public bool TrySetLocalScale(Vector3 scale, bool interpolate = false)
        {
            if(IsSelected)
                return false;

            originScale = transform.localScale;
            targetScale = scale;
            scaleTimer = interpolate ? interpolationTime : 0;

            ReplicateScaleRpc(scale);
            return true;
        }

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

            Debug.Assert(transform.parent == NetworkWorldOrigin.Transform);
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

        [Rpc(SendTo.ClientsAndHost)]
        private void ReplicateTransformationRpc(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            originPosition = transform.localPosition;
            targetPosition = position;
            positionTimer = interpolationTime;

            originRotation = transform.localRotation;
            targetRotation = rotation;
            rotationTimer = interpolationTime;

            originScale = transform.localScale;
            targetScale = scale;
            scaleTimer = interpolationTime;
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