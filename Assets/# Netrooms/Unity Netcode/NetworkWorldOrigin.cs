using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class NetworkWorldOrigin : NetworkPersistenSingletonRequired<NetworkWorldOrigin>
    {
        public static event Action OnPositionChanged;
        public static event Action OnRotationChanged;
        public static event Action OnScaleChanged;
        public static event Action OnAnyTransformationChanged;

        public static Transform Transform => Instance.transform;

        public static bool IsTransformationLocked => transformationLockRequests.Count > 0;

        public static Vector3 InverseLocalScale => inverseLocalScale;

        public static Vector3 Position { get; private set; }

        public static Quaternion Rotation { get; private set; }

        public static float Scale { get; private set; } = 1;

        private static readonly List<object> transformationLockRequests = new();

        private static Vector3 inverseLocalScale = Vector3.one;

        public static void AddLockTransformationRequest(object requester) 
        {
            if(!transformationLockRequests.Contains(requester))
                transformationLockRequests.Add(requester);
        }

        public static void RemoveLockTransformationRequest(object requester)
        {
            transformationLockRequests.Remove(requester);
        }

        public static void SetPostiion(Vector3 position)
        {
            if(IsTransformationLocked)
                return;

            if(position != Position)
            {
                Position = position;
                OnPositionChanged?.Invoke();
                OnAnyTransformationChanged?.Invoke();
            }
        }

        public static void SetRotation(Quaternion rotation)
        {
            if(IsTransformationLocked)
                return;

            if(rotation != Rotation)
            {
                Rotation = rotation;
                OnRotationChanged?.Invoke();
                OnAnyTransformationChanged?.Invoke();
            }
        }

        public static void SetScale(float scale)
        {
            if(IsTransformationLocked)
                return;

            if(scale > 0)
            {
                Scale = scale;
                inverseLocalScale = CalculateInverseScale(Vector3.one * scale);
                OnScaleChanged?.Invoke();
                OnAnyTransformationChanged?.Invoke();
            }
        }

        public static Transformation WorldToLocal(Transformation worldTransformation)
        {
            if(Instance == null)
            {
                return new()
                {
                    position = worldTransformation.position,
                    rotation = worldTransformation.rotation,
                    scale = worldTransformation.scale
                };
            }

            Transform transform = Transform;
            Vector3 lossyScale = transform.lossyScale;
            Vector3 scale = worldTransformation.scale;

            return new()
            {
                position = transform.InverseTransformPoint(worldTransformation.position),
                rotation = Quaternion.Inverse(transform.rotation) * worldTransformation.rotation,
                scale = new(scale.x / lossyScale.x, scale.y / lossyScale.y, scale.z / lossyScale.z)
            };
        }

        public static Transformation LocalToWorld(Transformation localTransformation)
        {
            if(Instance == null)
            {
                return new() 
                {
                    position = localTransformation.position,
                    rotation = localTransformation.rotation,
                    scale = localTransformation.scale
                };
            }

            Transform transform = Transform;
            Vector3 lossyScale = transform.lossyScale;
            Vector3 localScale = localTransformation.scale;

            return new()
            {
                position = transform.TransformPoint(localTransformation.position),
                rotation = transform.rotation * localTransformation.rotation,
                scale = new(localScale.x * lossyScale.x, localScale.y * lossyScale.y, localScale.z * lossyScale.z)
            };
        }

        public Vector3 testStartPosition = Vector3.zero;
        public Vector3 testRotation = Vector3.zero;
        public float testStartScale = 1;

        private void Start()
        {
            Debug.Assert(transform.parent == null);

            inverseLocalScale = CalculateInverseScale(transform.localScale);
        }

        private void OnEnable()
        {
            Position = testStartPosition;
            Rotation = Quaternion.Euler(testRotation);
            Scale = testStartScale;
        }

        private void LateUpdate()
        {
            if(transform.position != Position)
                transform.position = Position;

            if(transform.rotation != Rotation)
                transform.rotation = Rotation;

            if(transform.localScale.x != Scale || transform.localScale.y != Scale || transform.localScale.z != Scale)
                transform.localScale = new(Scale, Scale, Scale);
        }

        private static Vector3 CalculateInverseScale(Vector3 scale) => new(1 / scale.x, 1 / scale.y, 1 / scale.z);
    }
}
