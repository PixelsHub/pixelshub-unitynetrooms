#if UNITY_EDITOR
using UnityEngine;

namespace UnityEditor.PixelsHub
{
    public class EditorOnlyInstantiate : MonoBehaviour
    {
        [SerializeField]
        private Transform targetParent;

        [Space(8)]
        [SerializeField]
        private GameObject[] prefabs;

        private void Start()
        {
            foreach(var p in prefabs)
                Instantiate(p, targetParent);
        }
    }
}
#endif