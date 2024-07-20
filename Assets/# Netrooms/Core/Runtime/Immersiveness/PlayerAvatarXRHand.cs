using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace PixelsHub.Netrooms
{
    public class PlayerAvatarXRHand : MonoBehaviour
    {
        [Serializable]
        public class AvatarJoint
        {
            public XRHandJointID jointId;
            public Transform transform;
            public Quaternion startRotation;
            public Quaternion endRotation;
            public float interpolationTimer = jointInterpolationTime;

            public void SetRotation(Quaternion rotation) 
            {
                startRotation = transform.localRotation;
                endRotation = rotation;
                interpolationTimer = 0;
            }
        }

        public readonly static XRHandJointID[] targetJoints = new[]
        {
            XRHandJointID.ThumbMetacarpal,
            XRHandJointID.ThumbProximal,
            XRHandJointID.ThumbDistal,
            XRHandJointID.IndexMetacarpal,
            XRHandJointID.IndexProximal,
            XRHandJointID.IndexIntermediate,
            XRHandJointID.IndexDistal,
            XRHandJointID.MiddleMetacarpal,
            XRHandJointID.MiddleProximal,
            XRHandJointID.MiddleIntermediate,
            XRHandJointID.MiddleDistal,
            XRHandJointID.RingMetacarpal,
            XRHandJointID.RingProximal,
            XRHandJointID.RingIntermediate,
            XRHandJointID.RingDistal,
            XRHandJointID.LittleMetacarpal,
            XRHandJointID.LittleProximal,
            XRHandJointID.LittleIntermediate,
        };

        private const float wristInterpolationTime = 0.12f;
        private const float jointInterpolationTime = 0.15f;

        private static readonly int handColorProperty = Shader.PropertyToID("_EdgeColor");
        private static readonly int thumbColorProperty = Shader.PropertyToID("_ThumbColor");
        private static readonly int finger1ColorProperty = Shader.PropertyToID("_FingerColor_1");
        private static readonly int finger2ColorProperty = Shader.PropertyToID("_FingerColor_2");
        private static readonly int finger3ColorProperty = Shader.PropertyToID("_FingerColor_3");
        private static readonly int finger4ColorProperty = Shader.PropertyToID("_FingerColor_4");
        private static readonly int handScaleProperty = Shader.PropertyToID("_ObjectScale");

        [SerializeField]
        private GameObject rootObject;

        [SerializeField]
        private Transform wrist;

        [SerializeField]
        private AvatarJoint[] joints;

        [Header("Render")]
        [SerializeField]
        private Renderer targetRenderer;

        [SerializeField]
        private int targetMaterialIndex = 1;

        private Material TargetMaterial => targetRenderer.materials[targetMaterialIndex];

        private Vector3 startWristPosition;
        private Vector3 endWristPosition;
        private Quaternion startWristRotation;
        private Quaternion endWristRotation;
        private float wristInterpolationTimer;

        public void SetHandActive(bool isVisible)
        {
            if(rootObject.activeSelf != isVisible)
                rootObject.SetActive(isVisible);
        }

        public void SetColor(Color color)
        {
            color.a = 0.85f;
            TargetMaterial.SetColor(handColorProperty, color);

            color.a = 0.8f;
            TargetMaterial.SetColor(thumbColorProperty, color);
            TargetMaterial.SetColor(finger1ColorProperty, color);
            TargetMaterial.SetColor(finger2ColorProperty, color);
            TargetMaterial.SetColor(finger3ColorProperty, color);
            TargetMaterial.SetColor(finger4ColorProperty, color);
        }

        public void SetMaterialScale(Vector3 scale)
        {
            TargetMaterial.SetVector(handScaleProperty, scale);
        }

        public void SetWristPose(Vector3 position, Quaternion rotation, bool interpolate = true)
        {
            if(IsWristPoseBelowThreshold(position, rotation))
                return;

            if(interpolate)
            {
                startWristPosition = wrist.localPosition;
                startWristRotation = wrist.localRotation;
                endWristPosition = position;
                endWristRotation = rotation;

                wristInterpolationTimer = 0;
            }
            else
            {
                wristInterpolationTimer = wristInterpolationTime;
                wrist.SetLocalPositionAndRotation(position, rotation);
            }
        }

        public void ApplyJoint(XRHandJointID id, Quaternion rotation) 
        {
            for(int i = 0; i < joints.Length; i++)
            {
                if(joints[i].jointId == id)
                {
                    joints[i].SetRotation(rotation);
                    return;
                }
            }

            Debug.LogError($"Missing serialized joint for id \"{id}\".");
        }

        private void Awake()
        {
            // Ensure correct initial rotations
            for(int i = 0; i < joints.Length; i++)
                joints[i].endRotation = joints[i].transform.localRotation;
        }

        private void Update()
        {
            UpdateWristInterpolation();
            UpdateJointInterpolations();
        }

        private bool IsWristPoseBelowThreshold(Vector3 position, Quaternion rotation) 
        {
            return Vector3.Distance(position, wrist.position) < 0.002f 
                && Quaternion.Angle(rotation, wrist.rotation) < 0.5f;
        }

        private void UpdateWristInterpolation() 
        {
            if(wristInterpolationTimer <= wristInterpolationTime)
            {
                wristInterpolationTimer += Time.unscaledDeltaTime;

                float t = wristInterpolationTimer / wristInterpolationTime;

                wrist.SetLocalPositionAndRotation
                (
                    Vector3.Lerp(startWristPosition, endWristPosition, t),
                    Quaternion.Slerp(startWristRotation, endWristRotation, t)
                );
            }
        }

        private void UpdateJointInterpolations() 
        {
            for(int i = 0; i < joints.Length; i++)
            {
                var joint = joints[i];

                if(joint.interpolationTimer >= 1)
                    continue;

                joint.interpolationTimer += Time.unscaledDeltaTime;

                float t = joint.interpolationTimer / jointInterpolationTime;

                joint.transform.localRotation = Quaternion.Slerp(joint.startRotation, joint.endRotation, t);
            }
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
