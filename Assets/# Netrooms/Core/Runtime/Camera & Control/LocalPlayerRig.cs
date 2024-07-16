using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#pragma warning disable CS0108
namespace PixelsHub.Netrooms
{
    public class LocalPlayerRig : MonoBehaviour
    {
        public static LocalPlayerRig Instance { get; private set; }

        public Transform Pivot => pivot;

        public Camera Camera => camera;

        [SerializeField]
        private Transform pivot;

        [SerializeField]
        private Camera camera;

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(Instance.gameObject);
                Debug.LogWarning($"New {GetType()} ({name}) has prompted destruction of existing object ({Instance.name}).");
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if(Instance == this)
                Instance = null;
        }
    }
}
#pragma warning restore CS0108
