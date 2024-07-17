using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class PlayerAvatarImmersive : PlayerAvatar
    {
        private struct HandWrist : INetworkSerializable
        {
            public Vector3 wristPosition;
            public Quaternion wristRotation;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref wristPosition);
                serializer.SerializeValue(ref wristRotation);
            }
        }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        private struct UpdateHandParameters
        {
            public XRHand hand;
            public NetworkVariable<bool> isHandTracked;
            public NetworkVariable<HandWrist> handWrist;
            public Dictionary<XRHandJointID, Quaternion> jointCache;
            public int jointIterationStartIndex;
            public int jointIterationEndIndex;
            public Action<XRHandJointID, Quaternion> replicationAction;
        }

        private static XRHandSubsystem handSubsystem;
        private static bool isLocalPlayerUpdatingHands;
#endif

        private const NetworkVariableReadPermission defaultReadPermission = NetworkVariableReadPermission.Everyone;
        private const NetworkVariableWritePermission defaultWritePremission = NetworkVariableWritePermission.Owner;

        private readonly NetworkVariable<bool> isLeftHandTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<bool> isRightHandTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<HandWrist> leftHandWrist = new(default, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<HandWrist> rightHandWrist = new(default, defaultReadPermission, defaultWritePremission);

        // Inverted from local world origin scale
        private readonly NetworkVariable<Vector3> worldOriginCompensatoryScale = new(Vector3.one, defaultReadPermission, defaultWritePremission);

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

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
            if(IsLocalPlayer)
            {
                avatarHandLeft.SetHandActive(false);
                avatarHandRight.SetHandActive(false);

                lastLeftHandJointCache = new(PlayerAvatarXRHand.targetJoints.Length);
                lastRightHandJointCache = new(PlayerAvatarXRHand.targetJoints.Length);
                foreach(var jointId in PlayerAvatarXRHand.targetJoints)
                {
                    lastLeftHandJointCache.Add(jointId, default);
                    lastRightHandJointCache.Add(jointId, default);
                }

                // Coroutine start on OnEnable function will not have identified IsLocalPlayer before spawn
                if(!isLocalPlayerUpdatingHands)
                    StartCoroutine(UpdateLocalPlayerHandsCoroutine());
            }
            else
            {
#else
            if(!IsLocalPlayer)
            {
#endif
                avatarHandLeft.SetHandActive(isLeftHandTracked.Value);
                avatarHandRight.SetHandActive(isRightHandTracked.Value);
                SetHandWrist(avatarHandLeft, leftHandWrist.Value, false);
                SetHandWrist(avatarHandRight, rightHandWrist.Value, false);

                isLeftHandTracked.OnValueChanged += HandleLeftHandTrackedChanged;
                isRightHandTracked.OnValueChanged += HandleRightHandTrackedChanged;
                leftHandWrist.OnValueChanged += HandleLeftNetworkHandChanged;
                rightHandWrist.OnValueChanged += HandleRightNetworkHandChanged;

                worldOriginCompensatoryScale.OnValueChanged += HandleWorldOriginCompensatoryScaleChanged;
            }
        }
        
        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if(!IsLocalPlayer)
            {
                isLeftHandTracked.OnValueChanged = null;
                isRightHandTracked.OnValueChanged = null;
                leftHandWrist.OnValueChanged = null;
                rightHandWrist.OnValueChanged = null;

                worldOriginCompensatoryScale.OnValueChanged = null;
            }
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
            if(IsLocalPlayer && !isLocalPlayerUpdatingHands) // IsLocalPlayer will always be false until spawned
                StartCoroutine(UpdateLocalPlayerHandsCoroutine());
#endif
        }

        protected override void ApplyPlayerColor(Color color)
        {
            base.ApplyPlayerColor(color);

            avatarHandLeft.SetColor(color);
            avatarHandRight.SetColor(color);
        }

        protected override void LocalSetWorldOriginCompensatoryScale(Vector3 scale)
        {
            worldOriginCompensatoryScale.Value = scale;
        }

        private void HandleWorldOriginCompensatoryScaleChanged(Vector3 prev, Vector3 newScale) 
        {
            avatarHandLeft.SetMaterialScale(newScale);
            avatarHandRight.SetMaterialScale(newScale);
        }

        private void HandleLeftHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandLeft.SetHandActive(newValue);
            var wrist = leftHandWrist.Value;
            avatarHandLeft.SetWristPose(wrist.wristPosition, wrist.wristRotation, false);
        }

        private void HandleRightHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandRight.SetHandActive(newValue);
            var wrist = rightHandWrist.Value;
            avatarHandRight.SetWristPose(wrist.wristPosition, wrist.wristRotation, false);
        }

        private void HandleLeftNetworkHandChanged(HandWrist previous, HandWrist newValue)
        {
            SetHandWrist(avatarHandLeft, newValue);
        }

        private void HandleRightNetworkHandChanged(HandWrist previous, HandWrist newValue)
        {
            SetHandWrist(avatarHandRight, newValue);
        }

        private void SetHandWrist(PlayerAvatarXRHand avatarHand, HandWrist networkHand, bool interpolate = true)
        {
            avatarHand.SetWristPose(networkHand.wristPosition, networkHand.wristRotation, interpolate);
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateAvatarLeftHandJointRpc(XRHandJointID jointId, Quaternion value)
        {
            avatarHandLeft.ApplyJoint(jointId, value);
        }

        [Rpc(SendTo.NotMe)]
        private void ReplicateAvatarRightHandJointRpc(XRHandJointID jointId, Quaternion value)
        {
            avatarHandRight.ApplyJoint(jointId, value);
        }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        private IEnumerator UpdateLocalPlayerHandsCoroutine()
        {
            if(!IsLocalPlayer)
                throw new InvalidOperationException();

            // These values provide good results
            // Do NOT use serialized fields
            const int jointsPerBatch = 6;
            const float jointBatchUpdateTime = 1 / 30;

            int jointIterationStartIndex = 0;
            int jointIterationEndIndex = jointsPerBatch - 1;
            float jointBatchTimer = 0;

            isLocalPlayerUpdatingHands = true;

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

                UpdateHandParameters p = new()
                {
                    hand = handSubsystem.leftHand,
                    isHandTracked = isLeftHandTracked,
                    handWrist = leftHandWrist,
                    jointCache = lastLeftHandJointCache,
                    jointIterationStartIndex = jointIterationStartIndex,
                    jointIterationEndIndex = jointIterationEndIndex,
                    replicationAction = ReplicateAvatarLeftHandJointRpc
                };

                ReplicateLocalAvatarHand(p);

                yield return null;

                p.hand = handSubsystem.rightHand;
                p.isHandTracked = isRightHandTracked;
                p.handWrist = rightHandWrist;
                p.jointCache = lastRightHandJointCache;
                p.replicationAction = ReplicateAvatarRightHandJointRpc;

                ReplicateLocalAvatarHand(p);

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

        private void ReplicateLocalAvatarHand(UpdateHandParameters p) 
        {
            var hand = p.hand;
            var wrist = p.handWrist;

            if(hand.isTracked)
            {
                if(!p.isHandTracked.Value)
                    p.isHandTracked.Value = true;

                if(LocalXRHandReference.hands.TryGetValue(hand.handedness, out var localHand))
                {
                    var relativeTransform = headRoot.transform;

                    var value = wrist.Value;
                    value.wristPosition = relativeTransform.InverseTransformPoint(localHand.WristPosition);
                    value.wristRotation = Quaternion.Inverse(relativeTransform.rotation) * localHand.WristRotation;

                    if(Vector3.Distance(value.wristPosition, wrist.Value.wristPosition) > wristPositionThreshold ||
                        Quaternion.Angle(value.wristRotation, wrist.Value.wristRotation) > wristRotationAngleThreshold)
                    {
                        wrist.Value = value;
                    }

                    for(int i = p.jointIterationStartIndex; i <= p.jointIterationEndIndex; i++)
                    {
                        var joint = localHand.Joints[i];

                        Quaternion targetRotation = joint.transform.localRotation;

                        if(Quaternion.Angle(p.jointCache[joint.jointId], targetRotation) > jointRotationAngleThreshold)
                        {
                            p.jointCache[joint.jointId] = targetRotation;
                            p.replicationAction.Invoke(joint.jointId, targetRotation);
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
#endif

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
