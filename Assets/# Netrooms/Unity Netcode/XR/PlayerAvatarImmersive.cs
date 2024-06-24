using System;
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
        [Serializable]
        private class AvatarHand
        {
            public GameObject rootObject;
            public Transform wrist;
        }

        private struct NetworkHand : INetworkSerializable
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
        private XRHandSubsystem handSubsystem;
#endif

        [Header("Hands")]
        [SerializeField]
        private AvatarHand avatarHandLeft;

        [SerializeField]
        private AvatarHand avatarHandRight;

        private readonly NetworkVariable<bool> isLeftHandTracked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> isRightHandTracked = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<NetworkHand> networkLeftHand = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<NetworkHand> networkRightHand = new(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
            if(IsLocalPlayer)
            {
                avatarHandLeft.rootObject.SetActive(false);
                avatarHandRight.rootObject.SetActive(false);

                TrySetHandSubsystem();
            }
            else
            {
#else
            if(!IsLocalPlayer)
            {
#endif
                avatarHandLeft.rootObject.SetActive(isLeftHandTracked.Value);
                avatarHandRight.rootObject.SetActive(isRightHandTracked.Value);

                isLeftHandTracked.OnValueChanged += HandleLeftHandTrackedChanged;
                isRightHandTracked.OnValueChanged += HandleRightHandTrackedChanged;

                networkLeftHand.OnValueChanged += HandleLeftNetworkHandChanged;
                networkRightHand.OnValueChanged += HandleRightNetworkHandChanged;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if(!IsLocalPlayer)
            {
                isLeftHandTracked.OnValueChanged -= HandleLeftHandTrackedChanged;
                isRightHandTracked.OnValueChanged -= HandleRightHandTrackedChanged;

                networkLeftHand.OnValueChanged -= HandleLeftNetworkHandChanged;
                networkRightHand.OnValueChanged -= HandleRightNetworkHandChanged;
            }
        }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        protected override void LateUpdate()
        {
            base.LateUpdate();

            if(IsLocalPlayer)
            {
                if(handSubsystem != null)
                {
                    UpdateLocalHandTracking(handSubsystem.leftHand, isLeftHandTracked, networkLeftHand);
                    UpdateLocalHandTracking(handSubsystem.rightHand, isRightHandTracked, networkRightHand);
                }
                else
                    TrySetHandSubsystem();
            }
        }

        private void TrySetHandSubsystem()
        {
            if(handSubsystem != null)
                return;

            var subsystems = new List<XRHandSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            if(subsystems.Count > 0)
            {
                handSubsystem = subsystems[0];
                Debug.Log("Hand subsystem set");
            }
        }

        private void UpdateLocalHandTracking(XRHand hand, NetworkVariable<bool> isHandTracked, NetworkVariable<NetworkHand> networkHand) 
        {
            if(hand.isTracked)
            {
                if(!isHandTracked.Value)
                    isHandTracked.Value = true;

                if(hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out Pose pose))
                {
                    var value = networkHand.Value;
                    value.wristPosition = pose.position;
                    value.wristRotation = pose.rotation;
                    networkHand.Value = value;
                }
            }
            else
            {
                if(isHandTracked.Value)
                    isHandTracked.Value = false;
            }
        }
#endif

        private void HandleLeftHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandLeft.rootObject.SetActive(newValue);
        }

        private void HandleRightHandTrackedChanged(bool previousValue, bool newValue)
        {
            avatarHandRight.rootObject.SetActive(newValue);
        }

        private void HandleLeftNetworkHandChanged(NetworkHand previous, NetworkHand newValue)
        {
            ApplyNetworkHand(avatarHandLeft, newValue);
        }

        private void HandleRightNetworkHandChanged(NetworkHand previous, NetworkHand newValue)
        {
            ApplyNetworkHand(avatarHandRight, newValue);
        }

        private void ApplyNetworkHand(AvatarHand avatarHand, NetworkHand networkHand) 
        {
            avatarHand.wrist.position = networkHand.wristPosition;
            avatarHand.wrist.rotation = networkHand.wristRotation;
        }
    }
}
