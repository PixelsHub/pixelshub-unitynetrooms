using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class LocalPlayerRig : MonoBehaviour
    {
        public static event Action<LocalPlayerRig> OnInstanceSet;

        public static LocalPlayerRig Instance { get; private set; }

        public Transform Pivot => pivot;

        public Camera Camera => camera;

        [SerializeField]
        private Transform pivot;

#pragma warning disable CS0108
        [SerializeField]
        private Camera camera;
#pragma warning restore CS0108

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(Instance.gameObject);
                Debug.LogWarning($"New {GetType()} ({name}) has prompted destruction of existing object ({Instance.name}).");
            }

            Instance = this;
            OnInstanceSet?.Invoke(this);
        }

        private void OnDestroy()
        {
            if(Instance == this)
                Instance = null;
        }
    }
}
