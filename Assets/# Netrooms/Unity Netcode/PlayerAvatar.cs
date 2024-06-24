using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class PlayerAvatar : NetworkBehaviour
    {
        public NetworkPlayer NetworkPlayer { get; private set; }

        [SerializeField]
        private GameObject visualizationRoot;

        [Header("Head")]
        [SerializeField]
        private NetworkTransform headRoot;

        private Renderer[] headRenderers;

        public override void OnNetworkSpawn()
        {
            if(IsServer)
            {
                if(NetworkWorldOrigin.Instance != null)
                    SetAsChildOfOrigin(NetworkWorldOrigin.Instance);
                
                NetworkWorldOrigin.OnInstanceSet += SetAsChildOfOrigin;
            }

            if(IsLocalPlayer)
            {
                if(visualizationRoot.activeSelf)
                    visualizationRoot.SetActive(false);
            }
            else
            {
                if(!visualizationRoot.activeSelf)
                    visualizationRoot.SetActive(true);
            }
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
            NetworkPlayer = GetComponentInParent<NetworkPlayer>();
            Debug.Assert(NetworkPlayer != null, "PlayerAvatar must be a child of a NetworkPlayer on Awake.");

            headRenderers = headRoot.GetComponentsInChildren<Renderer>();
        }

        protected virtual void LateUpdate()
        {
            if(IsLocalPlayer && LocalPlayerCamera.Instance != null)
            {
                var t = LocalPlayerCamera.Instance.Camera.transform;
                headRoot.transform.SetPositionAndRotation(t.position, t.rotation);
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
