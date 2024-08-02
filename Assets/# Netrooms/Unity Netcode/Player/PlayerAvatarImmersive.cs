using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class PlayerAvatarImmersive : PlayerAvatar
    {
        private struct NetworkPose : INetworkSerializable
        {
            public Vector3 position;
            public Quaternion rotation;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref position);
                serializer.SerializeValue(ref rotation);
            }
        }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        private class UpdateControllerParameters
        {
            public NetworkVariable<bool> isTracked;
            public NetworkVariable<NetworkPose> pose;
        }

        private class UpdateHandParameters
        {
            public XRHand hand;
            public NetworkVariable<bool> isHandTracked;
            public NetworkVariable<NetworkPose> pose;
            public Dictionary<XRHandJointID, Quaternion> jointCache;
            public int jointIterationStartIndex;
            public int jointIterationEndIndex;
            public Action<XRHandJointID, Quaternion> jointReplicationAction;

            public UpdateHandParameters(int jointIterationStartIndex, int jointIterationEndIndex) 
            {
                this.jointIterationStartIndex = jointIterationStartIndex;
                this.jointIterationEndIndex = jointIterationEndIndex;
            }
        }

        private static bool isLocalPlayerUpdatingControllers;
        private static bool isLocalPlayerUpdatingHands;

        private static XRHandSubsystem handSubsystem;
#endif

        private const NetworkVariableReadPermission defaultReadPermission = NetworkVariableReadPermission.Everyone;
        private const NetworkVariableWritePermission defaultWritePremission = NetworkVariableWritePermission.Owner;

        private readonly NetworkVariable<bool> isLeftControllerTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<bool> isRightControllerTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<NetworkPose> leftControllerPose = new(default, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<NetworkPose> rightControllerPose = new(default, defaultReadPermission, defaultWritePremission);

        private readonly NetworkVariable<bool> isLeftHandTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<bool> isRightHandTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<NetworkPose> leftHandPose = new(default, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<NetworkPose> rightHandPose = new(default, defaultReadPermission, defaultWritePremission);

        private readonly NetworkVariable<Vector3> worldOriginInverseScale = new(Vector3.one, defaultReadPermission, defaultWritePremission);

        [Header("Controllers")]
        [SerializeField]
        private PlayerAvatarXRController avatarControllerLeft;

        [SerializeField]
        private PlayerAvatarXRController avatarControllerRight;

        [Header("Hands")]
        [SerializeField]
        private PlayerAvatarXRHand avatarHandLeft;

        [SerializeField]
        private PlayerAvatarXRHand avatarHandRight;

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        [SerializeField]
        private float wristPositionThreshold = 0.008f;

        [SerializeField]
        private float wristRotationAngleThreshold = 0.15f;

        [SerializeField]
        private float jointRotationAngleThreshold = 0.35f;

        // Caches are used to detect changes in joint rotation with threshold comparison
        private Dictionary<XRHandJointID, Quaternion> lastLeftHandJointCache;
        private Dictionary<XRHandJointID, Quaternion> lastRightHandJointCache;
#endif

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(IsLocalPlayer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += InitializeJointsForConnectedClient;

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
                avatarControllerLeft.SetActive(false);
                avatarControllerLeft.SetActive(false);
                avatarHandLeft.SetActive(false);
                avatarHandRight.SetActive(false);

                lastLeftHandJointCache = new(PlayerAvatarXRHand.targetJoints.Length);
                lastRightHandJointCache = new(PlayerAvatarXRHand.targetJoints.Length);
                foreach(var jointId in PlayerAvatarXRHand.targetJoints)
                {
                    lastLeftHandJointCache.Add(jointId, default);
                    lastRightHandJointCache.Add(jointId, default);
                }

                // Coroutines start on OnEnable function will not have identified IsLocalPlayer before spawn
                if(!isLocalPlayerUpdatingControllers)
                    StartCoroutine(UpdateLocalPlayerControllersCoroutine());
                if(!isLocalPlayerUpdatingHands)
                    StartCoroutine(UpdateLocalPlayerHandsCoroutine());
#endif
            }
            else
            {
                avatarControllerLeft.SetActive(isLeftControllerTracked.Value);
                avatarControllerRight.SetActive(isRightControllerTracked.Value);
                SetPose(avatarControllerLeft, leftControllerPose.Value, false);
                SetPose(avatarControllerRight, rightControllerPose.Value, false);

                avatarHandLeft.SetActive(isLeftHandTracked.Value);
                avatarHandRight.SetActive(isRightHandTracked.Value);
                SetPose(avatarHandLeft, leftHandPose.Value, false);
                SetPose(avatarHandRight, rightHandPose.Value, false);

                isLeftControllerTracked.OnValueChanged += HandleLeftControllerTrackedChanged;
                isRightControllerTracked.OnValueChanged += HandleRightControllerTrackedChanged;
                leftControllerPose.OnValueChanged += HandleLeftControllerPoseChanged;
                rightControllerPose.OnValueChanged += HandleRightControllerPoseChanged;
                isLeftHandTracked.OnValueChanged += HandleLeftHandTrackedChanged;
                isRightHandTracked.OnValueChanged += HandleRightHandTrackedChanged;
                leftHandPose.OnValueChanged += HandleLeftHandPoseChanged;
                rightHandPose.OnValueChanged += HandleRightHandPoseChanged;

                worldOriginInverseScale.OnValueChanged += HandleWorldOriginInverseScaleChanged;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if(IsLocalPlayer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback -= InitializeJointsForConnectedClient;
            }
            else
            {
                isLeftControllerTracked.OnValueChanged = null;
                isRightControllerTracked.OnValueChanged = null;
                leftControllerPose.OnValueChanged = null;
                rightControllerPose.OnValueChanged = null;
                isLeftHandTracked.OnValueChanged = null;
                isRightHandTracked.OnValueChanged = null;
                leftHandPose.OnValueChanged = null;
                rightHandPose.OnValueChanged = null;

                worldOriginInverseScale.OnValueChanged = null;
            }
        }

        private void InitializeJointsForConnectedClient(ulong client)
        {
            foreach(var joint in lastLeftHandJointCache)
                ReplicateAvatarLeftHandJointRpc(joint.Key, joint.Value);

            foreach(var joint in lastRightHandJointCache)
                ReplicateAvatarRightHandJointRpc(joint.Key, joint.Value);
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
            if(IsLocalPlayer)
            {
                if(!isLocalPlayerUpdatingControllers)
                    StartCoroutine(UpdateLocalPlayerControllersCoroutine());

                if(!isLocalPlayerUpdatingHands)
                    StartCoroutine(UpdateLocalPlayerHandsCoroutine());
            }
#endif
        }

        protected override void ApplyPlayerColor(Color color)
        {
            base.ApplyPlayerColor(color);

            avatarControllerLeft.SetColor(color);
            avatarControllerRight.SetColor(color);
            avatarHandLeft.SetColor(color);
            avatarHandRight.SetColor(color);
        }

        protected override void LocalProcessWorldOriginScaleChanged()
        {
            base.LocalProcessWorldOriginScaleChanged();
            worldOriginInverseScale.Value = NetworkWorldOrigin.InverseLocalScale;
        }

        private void HandleWorldOriginInverseScaleChanged(Vector3 prev, Vector3 newScale) 
        {
            avatarHandLeft.SetMaterialScale(newScale);
            avatarHandRight.SetMaterialScale(newScale);
        }

        private void HandleLeftControllerTrackedChanged(bool previousValue, bool newValue)
        {
            avatarControllerLeft.SetActive(newValue);
            var pose = leftControllerPose.Value;
            avatarControllerLeft.SetPose(pose.position, pose.rotation, false);
        }

        private void HandleRightControllerTrackedChanged(bool previousValue, bool newValue)
        {
            avatarControllerRight.SetActive(newValue);
            var pose = rightControllerPose.Value;
            avatarControllerRight.SetPose(pose.position, pose.rotation, false);
        }

        private void HandleLeftHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandLeft.SetActive(newValue);
            var pose = leftHandPose.Value;
            avatarHandLeft.SetPose(pose.position, pose.rotation, false);
        }

        private void HandleRightHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandRight.SetActive(newValue);
            var pose = rightHandPose.Value;
            avatarHandRight.SetPose(pose.position, pose.rotation, false);
        }

        private void HandleLeftControllerPoseChanged(NetworkPose previous, NetworkPose newValue)
        {
            SetPose(avatarControllerLeft, newValue);
        }

        private void HandleRightControllerPoseChanged(NetworkPose previous, NetworkPose newValue)
        {
            SetPose(avatarControllerRight, newValue);
        }

        private void HandleLeftHandPoseChanged(NetworkPose previous, NetworkPose newValue)
        {
            SetPose(avatarHandLeft, newValue);
        }

        private void HandleRightHandPoseChanged(NetworkPose previous, NetworkPose newValue)
        {
            SetPose(avatarHandRight, newValue);
        }

        private void SetPose(PlayerAvatarXRController target, NetworkPose networkPose, bool interpolate = true)
        {
            target.SetPose(networkPose.position, networkPose.rotation, interpolate);
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateAvatarLeftHandJointRpc(XRHandJointID jointId, Quaternion value)
        {
            avatarHandLeft.SetJointLocalRotation(jointId, value);
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateAvatarRightHandJointRpc(XRHandJointID jointId, Quaternion value)
        {
            avatarHandRight.SetJointLocalRotation(jointId, value);
        }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        private IEnumerator UpdateLocalPlayerControllersCoroutine()
        {
            if(!IsLocalPlayer)
                throw new InvalidOperationException();

            isLocalPlayerUpdatingControllers = true;

            while(enabled)
            {
                foreach(var controller in LocalXRControllerReference.controllers)
                {
                    UpdateControllerParameters p;

                    switch(controller.Handedness)
                    {
                        case InteractorHandedness.Left:
                            p = new() 
                            {
                                isTracked = isLeftControllerTracked,
                                pose = leftControllerPose
                            };
                            break;

                        case InteractorHandedness.Right:
                            p = new()
                            {
                                isTracked = isRightControllerTracked,
                                pose = rightControllerPose
                            };
                            break;

                        default:
                        case InteractorHandedness.None:
                            continue;
                    }

                    var pose = p.pose;

                    if(controller.IsActive)
                    {
                        if(!p.isTracked.Value)
                            p.isTracked.Value = true;

                        var relativeTransform = headRoot.transform;

                        var value = pose.Value;
                        value.position = relativeTransform.InverseTransformPoint(controller.transform.position);
                        value.rotation = Quaternion.Inverse(relativeTransform.rotation) * controller.transform.rotation;

                        if(Vector3.Distance(value.position, pose.Value.position) > wristPositionThreshold ||
                            Quaternion.Angle(value.rotation, pose.Value.rotation) > wristRotationAngleThreshold)
                        {
                            pose.Value = value;
                        }
                    }
                    else
                    {
                        if(p.isTracked.Value)
                            p.isTracked.Value = false;
                    }

                    yield return null;
                }

                yield return null;
            }

            isLocalPlayerUpdatingControllers = false;
        }

        private IEnumerator UpdateLocalPlayerHandsCoroutine()
        {
            if(!IsLocalPlayer)
                throw new InvalidOperationException();

            isLocalPlayerUpdatingHands = true;

            // These values provide good results
            // Do NOT use serialized fields
            const int jointsPerBatch = 8;
            const float jointBatchUpdateFramesPerSecond = 30;
            const float jointBatchUpdateTime = 1 / jointBatchUpdateFramesPerSecond;

            int jointIterationStartIndex = 0;
            int jointIterationEndIndex = jointsPerBatch - 1;
            float jointBatchTimer = 0;

            while(enabled)
            {
                if(handSubsystem == null)
                {
                    WaitForSecondsRealtime wait = new(0.5f);

                    List<XRHandSubsystem> handSubsystemsFiller = new(1);

                    while(enabled && handSubsystem == null)
                    {
                        SubsystemManager.GetSubsystems(handSubsystemsFiller);

                        if(handSubsystemsFiller.Count > 0)
                            handSubsystem = handSubsystemsFiller[0];

                        yield return wait;
                    }
                }

                UpdateHandParameters p = new(jointIterationStartIndex, jointIterationEndIndex);

                SetHandParametersForLeftHand(ref p);
                SendLocalAvatarHandDataToOtherClients(p);
                yield return null;
                SetHandParametersForRightHand(ref p);
                SendLocalAvatarHandDataToOtherClients(p);

                jointBatchTimer += Time.unscaledDeltaTime;

                if(jointBatchTimer >= jointBatchUpdateTime)
                {
                    jointBatchTimer = 0;

                    jointIterationStartIndex += jointsPerBatch;
                    jointIterationEndIndex += jointsPerBatch;

                    if(jointIterationStartIndex >= PlayerAvatarXRHand.targetJoints.Length)
                    {
                        jointIterationStartIndex = 0;
                        jointIterationEndIndex = jointsPerBatch - 1;
                    }
                    else if(jointIterationEndIndex >= PlayerAvatarXRHand.targetJoints.Length)
                    {
                        jointIterationEndIndex = PlayerAvatarXRHand.targetJoints.Length - 1;
                    }
                }
                
                yield return null;
            }

            isLocalPlayerUpdatingHands = false;
        }

        private void SetHandParametersForLeftHand(ref UpdateHandParameters p)
        {
            p.hand = handSubsystem.leftHand;
            p.isHandTracked = isLeftHandTracked;
            p.pose = leftHandPose;
            p.jointCache = lastLeftHandJointCache;
            p.jointReplicationAction = ReplicateAvatarLeftHandJointRpc;
        }

        private void SetHandParametersForRightHand(ref UpdateHandParameters p)
        {
            p.hand = handSubsystem.rightHand;
            p.isHandTracked = isRightHandTracked;
            p.pose = rightHandPose;
            p.jointCache = lastRightHandJointCache;
            p.jointReplicationAction = ReplicateAvatarRightHandJointRpc;
        }

        private void SendLocalAvatarHandDataToOtherClients(UpdateHandParameters p) 
        {
            var hand = p.hand;
            var pose = p.pose;

            if(hand.isTracked)
            {
                if(!p.isHandTracked.Value)
                    p.isHandTracked.Value = true;

                if(LocalXRHandReference.hands.TryGetValue(hand.handedness, out var localHand))
                {
                    var relativeTransform = headRoot.transform;

                    var value = pose.Value;
                    value.position = relativeTransform.InverseTransformPoint(localHand.WristPosition);
                    value.rotation = Quaternion.Inverse(relativeTransform.rotation) * localHand.WristRotation;

                    if(Vector3.Distance(value.position, pose.Value.position) > wristPositionThreshold ||
                        Quaternion.Angle(value.rotation, pose.Value.rotation) > wristRotationAngleThreshold)
                    {
                        pose.Value = value;
                    }

                    for(int i = p.jointIterationStartIndex; i <= p.jointIterationEndIndex; i++)
                    {
                        var joint = localHand.Joints[i];

                        Quaternion targetRotation = joint.transform.localRotation;

                        if(Quaternion.Angle(p.jointCache[joint.jointId], targetRotation) > jointRotationAngleThreshold)
                        {
                            p.jointCache[joint.jointId] = targetRotation;
                            p.jointReplicationAction.Invoke(joint.jointId, targetRotation);
                        }
                    }
                }
                else
                    Debug.LogError("Missing local hand reference.");
            }
            else
            {
                if(p.isHandTracked.Value)
                    p.isHandTracked.Value = false;
            }
        }
#endif // UNITY_EDITOR || IMMERSIVE_XR_BUILD

        private void OnValidate()
        {
            const string invalidHandParentingLog = "Avatar Hands should be children of the head transform.";

            if(avatarHandLeft != null)
                Debug.Assert(avatarHandLeft.transform.IsChildOf(headRoot.transform), invalidHandParentingLog);

            if(avatarHandRight != null)
                Debug.Assert(avatarHandRight.transform.IsChildOf(headRoot.transform), invalidHandParentingLog);
        }
    }
}
