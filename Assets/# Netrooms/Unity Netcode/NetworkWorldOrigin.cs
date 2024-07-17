using System;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class NetworkWorldOrigin : NetworkPersistenSingletonRequired<NetworkWorldOrigin>
    {
        public static event Action OnScaleChanged;

        public static Transform Transform => Instance.transform;

        private const float scaleChangeThreshold = 0.01f;

        private Vector3 lastScale;

        private void Update()
        {
            Vector3 scale = transform.localScale;
            if(ScaleAxisChanged(scale.x, lastScale.x) || ScaleAxisChanged(scale.y, lastScale.y) || ScaleAxisChanged(scale.z, lastScale.z))
            {
                lastScale = scale;
                OnScaleChanged?.Invoke();
            }
        }

        private static bool ScaleAxisChanged(float a, float b) 
        {
            return Mathf.Abs(a - b) > scaleChangeThreshold;
        }
    }
}
