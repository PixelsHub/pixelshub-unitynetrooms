using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using AvatarJoint = PixelsHub.Netrooms.PlayerAvatarXRHand.AvatarJoint;

namespace PixelsHub.Netrooms
{
    /// <summary>
    /// Provides transform references for XR Hands.
    /// <para>This class has been created due to the unreliability of XRHand subsystem joint poses.</para>
    /// </summary>
    public class LocalXRHandReference : MonoBehaviour
    {
        public static readonly Dictionary<Handedness, LocalXRHandReference> hands = new(2);

        public Vector3 WristPosition => wrist.position;

        public Quaternion WristRotation => wrist.rotation;

        public AvatarJoint[] Joints => joints;

        [SerializeField]
        private Handedness handedness;

        [SerializeField]
        private Transform wrist;

        [Space(8)]
        [SerializeField]
        private AvatarJoint[] joints;

        private void Start()
        {
            Debug.Assert(!hands.ContainsKey(handedness));

            hands.Add(handedness, this);
        }

        private void OnDestroy()
        {
            hands.Remove(handedness);
        }

#if UNITY_EDITOR
        [Header("Editor")]
        [SerializeField]
        private string jointHierarchyNamePrefix;

        [SerializeField]
        private bool fetchJointsFromHierarchy;

        private void OnValidate()
        {
            if(fetchJointsFromHierarchy)
            {
                fetchJointsFromHierarchy = false;

                var targetJoints = PlayerAvatarXRHand.targetJoints;

                joints = new AvatarJoint[targetJoints.Length];

                for(int i = 0; i < targetJoints.Length; i++)
                {
                    joints[i] = new()
                    {
                        jointId = targetJoints[i],
                        transform = FindJointRecursive(transform, $"{jointHierarchyNamePrefix}{targetJoints[i]}")
                    };
                }

                static Transform FindJointRecursive(Transform parent, string name)
                {
                    for(int i = 0; i < parent.childCount; i++)
                    {
                        var child = parent.GetChild(i);

                        if(child.name == name)
                            return child;

                        var result = FindJointRecursive(child, name);

                        if(result != null)
                            return result;
                    }

                    return null;
                }
            }
        }
#endif
    }
}
