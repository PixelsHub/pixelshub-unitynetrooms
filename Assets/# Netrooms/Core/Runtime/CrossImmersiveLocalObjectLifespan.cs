using UnityEngine;
using PixelsHub.XR;

namespace PixelsHub.Netrooms
{
    public class CrossImmersiveLocalObjectLifespan : MonoBehaviour
    {
        [SerializeField]
        private GameObject defaultLocalObjectPrefab;

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
        [SerializeField]
        private GameObject immersiveLocalObjectPrefab;
#endif

        [Space(8)]
        [SerializeField]
        private bool createAtSamePose = true;

        [SerializeField]
        private bool createAsChild;

        private GameObject localObject;

        private void OnEnable()
        {
            InstantiateLocalObjectPrefab();
        }

        private void OnDisable()
        {
            Destroy(localObject);
        }

        private void InstantiateLocalObjectPrefab()
        {
            GameObject InternalInstantiate(GameObject prefab)
            {
                Transform parent = createAsChild ? transform : null;

                if(createAtSamePose)
                    return Instantiate(prefab, transform.position, transform.rotation, parent);
                else
                    return Instantiate(prefab, parent);
            }

#if UNITY_EDITOR || IMMERSIVE_XR_BUILD
            if (Immersiveness.IsActive)
                localObject = InternalInstantiate(immersiveLocalObjectPrefab);
            else
#endif
                localObject = InternalInstantiate(defaultLocalObjectPrefab);
        }
    }
}
