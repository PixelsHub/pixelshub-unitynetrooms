using System;
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class XRImmersivenessPrefabInstantiator : MonoBehaviour
    {
        [Serializable]
        private struct Prefabs
        {
            public GameObject defaultPrefab;
#if UNITY_EDITOR || XR_IMMERSIVE_BUILD
            public GameObject immersivePrefab;
#endif
        }

        [SerializeField]
        private Transform targetParent;

        [Space(8)]
        [SerializeField]
        private Prefabs[] prefabs;

        private void Start()
        {
#if UNITY_EDITOR || XR_IMMERSIVE_BUILD
            if(XRImmersiveness.IsActive)
                LoadImmersivePrefabs();
            else
#endif
                LoadDefaultPrefabs();
        }
        private void LoadDefaultPrefabs()
        {
            foreach(var p in prefabs)
                Instantiate(p.defaultPrefab, targetParent);
        }

#if UNITY_EDITOR || XR_IMMERSIVE_BUILD
        private void LoadImmersivePrefabs() 
        {
            foreach(var p in prefabs)
                Instantiate(p.immersivePrefab, targetParent);
        }
#endif
    }
}
