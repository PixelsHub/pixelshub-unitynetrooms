using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
using UnityEngine.XR.Hands;
#endif

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

        private struct UpdateHandParameters
        {
            public XRHand hand;
            public NetworkVariable<bool> isHandTracked;
            public NetworkVariable<HandWrist> handWrist;
            public Dictionary<XRHandJointID, Quaternion> jointCache;
            public int jointIterationStartIndex;
            public int jointIterationEndIndex;
        }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        private static XRHandSubsystem handSubsystem;
        private static bool isLocalPlayerUpdatingHands;
#endif

        private const NetworkVariableReadPermission defaultReadPermission = NetworkVariableReadPermission.Everyone;
        private const NetworkVariableWritePermission defaultWritePremission = NetworkVariableWritePermission.Owner;

        [Header("Hands")]
        [SerializeField]
        private PlayerAvatarXRHand avatarHandLeft;

        [SerializeField]
        private PlayerAvatarXRHand avatarHandRight;

        [SerializeField]
        private float wristPositionThreshold = 0.008f;

        [SerializeField]
        private float wristRotationAngleThreshold = 0.15f;

        [SerializeField]
        private float jointRotationAngleThreshold = 0.35f;

        private readonly NetworkVariable<bool> isLeftHandTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<bool> isRightHandTracked = new(false, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<HandWrist> leftHandWrist = new(default, defaultReadPermission, defaultWritePremission);
        private readonly NetworkVariable<HandWrist> rightHandWrist = new(default, defaultReadPermission, defaultWritePremission);

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
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
                SetHandWrist(avatarHandLeft, leftHandWrist.Value);
                SetHandWrist(avatarHandRight, rightHandWrist.Value);

                isLeftHandTracked.OnValueChanged += HandleLeftHandTrackedChanged;
                isRightHandTracked.OnValueChanged += HandleRightHandTrackedChanged;
                leftHandWrist.OnValueChanged += HandleLeftNetworkHandChanged;
                rightHandWrist.OnValueChanged += HandleRightNetworkHandChanged;
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
            }
        }

        protected virtual void OnEnable()
        {
#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
            if(IsLocalPlayer && !isLocalPlayerUpdatingHands) // IsLocalPlayer will always be false until spawned
                StartCoroutine(UpdateLocalPlayerHandsCoroutine());
#endif
        }

        private void HandleLeftHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandLeft.SetHandActive(newValue);
        }

        private void HandleRightHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandRight.SetHandActive(newValue);
        }

        private void HandleLeftNetworkHandChanged(HandWrist previous, HandWrist newValue)
        {
            SetHandWrist(avatarHandLeft, newValue);
        }

        private void HandleRightNetworkHandChanged(HandWrist previous, HandWrist newValue)
        {
            SetHandWrist(avatarHandRight, newValue);
        }

        private void SetHandWrist(PlayerAvatarXRHand avatarHand, HandWrist networkHand)
        {
            avatarHand.SetWristPose(networkHand.wristPosition, networkHand.wristRotation);
        }

        [Rpc(SendTo.NotMe)]
        private void UpdateAvatarHandJointRpc(Handedness handedness, XRHandJointID jointId, Quaternion value)
        {
            if(handedness == Handedness.Left)
                avatarHandLeft.ApplyJoint(jointId, value);
            else if(handedness == Handedness.Right)
                avatarHandRight.ApplyJoint(jointId, value);
            else
                Debug.Assert(false);
        }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        private IEnumerator UpdateLocalPlayerHandsCoroutine()
        {
            if(!IsLocalPlayer)
                throw new System.InvalidOperationException();

            // These values provide good results
            // Do NOT make serialized fields
            const int jointsPerBatch = 6;
            const float jointBatchUpdateTime = 0.05f;

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
                    jointIterationEndIndex = jointIterationEndIndex
                };

                ReplicateLocalPlayerHand(p);

                yield return null;

                p.hand = handSubsystem.rightHand;
                p.isHandTracked = isRightHandTracked;
                p.handWrist = rightHandWrist;
                p.jointCache = lastRightHandJointCache;

                ReplicateLocalPlayerHand(p);

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

        private void ReplicateLocalPlayerHand(UpdateHandParameters p) 
        {
            var hand = p.hand;
            var wrist = p.handWrist;

            if(hand.isTracked)
            {
                if(!p.isHandTracked.Value)
                    p.isHandTracked.Value = true;

                if(LocalPlayerXRHandReference.hands.TryGetValue(hand.handedness, out var localHand))
                {
                    var origin = NetworkWorldOrigin.Instance.transform;

                    var value = wrist.Value;
                    value.wristPosition = origin.InverseTransformPoint(localHand.WristPosition);
                    value.wristRotation = Quaternion.Inverse(origin.rotation) * localHand.WristRotation;

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
                            UpdateAvatarHandJointRpc(hand.handedness, joint.jointId, targetRotation);
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
    }
}
