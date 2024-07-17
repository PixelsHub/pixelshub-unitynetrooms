using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    public class PlayerAvatar : NetworkBehaviour
    {
        public NetworkPlayer Player { get; private set; }

        [SerializeField]
        private GameObject[] visualizationRoots;

        [Header("Head")]
        [SerializeField]
        protected NetworkTransform headRoot;

        private Renderer[] headRenderers;

        public override void OnNetworkSpawn()
        {
            if(NetworkPlayer.Players.TryGetValue(OwnerClientId, out var player))
            {
                Player = player;

                ApplyPlayerColor(player.Color);
                player.OnColorChanged += ApplyPlayerColor;
            }
            else
                Debug.LogError($"Invalid Avatar spawned. Could not find Player for Id={OwnerClientId}.");

            if(IsServer)
            {
                if(NetworkWorldOrigin.Instance != null)
                    SetAsChildOfOrigin(NetworkWorldOrigin.Instance);
                
                NetworkWorldOrigin.OnInstanceSet += SetAsChildOfOrigin;
            }

            if(IsLocalPlayer)
            {
                NetworkWorldOrigin.OnScaleChanged += LocalProcessWorldOriginScaleChanged;
            }

            MakeVisible(!IsLocalPlayer);
        }

        public override void OnNetworkDespawn()
        {
            if(Player != null)
            {
                Player.OnColorChanged -= ApplyPlayerColor;
            }

            if(IsServer)
            {
                NetworkWorldOrigin.OnInstanceSet -= SetAsChildOfOrigin;
            }

            if(IsLocalPlayer)
            {
                NetworkWorldOrigin.OnScaleChanged -= LocalProcessWorldOriginScaleChanged;
            }
        }

        protected virtual void Awake()
        {
            headRenderers = headRoot.GetComponentsInChildren<Renderer>();
        }

        protected virtual void LateUpdate()
        {
            if(IsLocalPlayer && LocalPlayerRig.Instance != null)
            {
                var t = LocalPlayerRig.Instance.Camera.transform;
                headRoot.transform.SetPositionAndRotation(t.position, t.rotation);
            }
        }

        protected virtual void MakeVisible(bool isVisible) 
        {
            foreach(var root in visualizationRoots)
            {
                if(root == null)
                {
                    Debug.Assert(false);
                    continue;
                }

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

        protected void LocalProcessWorldOriginScaleChanged()
        {
            var originScale = NetworkWorldOrigin.Transform.localScale;
            Vector3 compensatoryScale = new(1 / originScale.x, 1 / originScale.y, 1 / originScale.z);
            headRoot.transform.localScale = compensatoryScale;

            LocalSetWorldOriginCompensatoryScale(compensatoryScale);
        }

        protected virtual void LocalSetWorldOriginCompensatoryScale(Vector3 scale) { }

        protected virtual void ApplyPlayerColor(Color color)
        {
            foreach(var renderer in headRenderers)
                renderer.material.color = color;
        }
    }
}
