using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace PixelsHub.Netrooms
{
    public interface IPlayerAvatarColorTarget 
    {
        void ApplyPlayerColor(Color color);
    }

    [DisallowMultipleComponent]
    public class PlayerAvatar : NetworkBehaviour
    {
        public event Action<Color> OnPlayerColorChanged;

        public NetworkPlayer Player { get; private set; }

        [SerializeField]
        private GameObject[] visualizationRoots;

        [Header("Head")]
        [SerializeField]
        protected NetworkTransform headRoot;

        private IPlayerAvatarColorTarget[] colorTargets;

        public override void OnNetworkSpawn()
        {
            if(NetworkPlayer.Players.TryGetValue(OwnerClientId, out var player))
            {
                Player = player;

                colorTargets = GetComponentsInChildren<IPlayerAvatarColorTarget>();

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

        protected virtual void LocalProcessWorldOriginScaleChanged()
        {
            headRoot.transform.localScale = NetworkWorldOrigin.InverseLocalScale;
        }

        protected virtual void ApplyPlayerColor(Color color)
        {
            if(colorTargets != null)
                foreach(var colorTarget in colorTargets)
                    colorTarget.ApplyPlayerColor(color);

            OnPlayerColorChanged?.Invoke(color);
        }
    }
}
