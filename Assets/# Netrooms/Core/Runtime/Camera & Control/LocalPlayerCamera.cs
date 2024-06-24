using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class LocalPlayerCamera : MonoBehaviour
    {
        public static LocalPlayerCamera Instance { get; private set; }

        public Transform Pivot => pivot;

        public Camera Camera => camera;

        [SerializeField]
        private Transform pivot;

        [SerializeField]
        private new Camera camera;

        private void OnEnable()
        {
            Instance = this;
        }

        private void OnDisable()
        {
            if(Instance == this)
                Instance = null;
        }
    }
}
