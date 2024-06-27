using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class PlayerAvatar : NetworkBehaviour
    {
        [SerializeField]
        private GameObject[] visualizationRoots;

        [Header("Head")]
        [SerializeField]
        protected NetworkTransform headRoot;

        private Renderer[] headRenderers;

        public override void OnNetworkSpawn()
        {
            if(IsServer)
            {
                if(NetworkWorldOrigin.Instance != null)
                    SetAsChildOfOrigin(NetworkWorldOrigin.Instance);
                
                NetworkWorldOrigin.OnInstanceSet += SetAsChildOfOrigin;
            }

            MakeVisible(!IsLocalPlayer);
        }

        public override void OnNetworkDespawn()
        {
            if(IsServer)
            {
                NetworkWorldOrigin.OnInstanceSet -= SetAsChildOfOrigin;
            }
        }

        protected virtual void Awake()
        {
            headRenderers = headRoot.GetComponentsInChildren<Renderer>();
        }

        protected virtual void LateUpdate()
        {
            if(IsLocalPlayer && LocalPlayerOrigin.Instance != null)
            {
                var t = LocalPlayerOrigin.Instance.Camera.transform;
                headRoot.transform.SetPositionAndRotation(t.position, t.rotation);
            }
        }

        protected virtual void MakeVisible(bool isVisible) 
        {
            foreach(var root in visualizationRoots)
            {
                if(root.activeSelf != isVisible)
                    root.SetActive(isVisible);
            }
        }

        private void SetAsChildOfOrigin(NetworkWorldOrigin origin)
        {
            if(origin == null)
                return;

            transform.SetParent(origin.transform);
        }

        protected virtual void ApplyPlayerColor(Color color) 
        {
            foreach(var renderer in headRenderers)
                renderer.material.color = color;
        }
    }
}
