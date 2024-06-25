using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class LocalPlayerOrigin : MonoBehaviour
    {
        public static LocalPlayerOrigin Instance { get; private set; }

        public Transform Pivot => pivot;

        public Camera Camera => camera;

        [SerializeField]
        private Transform pivot;

        [SerializeField]
        private new Camera camera;

        private void Awake()
        {
            if(Instance != null)
            {
                Destroy(Instance.gameObject);
                Debug.LogWarning($"New LocalPlayerOrigin ({name}) has prompted destruction of existing object ({Instance.name}).");
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
