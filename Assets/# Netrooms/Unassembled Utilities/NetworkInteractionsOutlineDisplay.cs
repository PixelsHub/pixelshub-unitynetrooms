using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.GraphicsTools;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixelsHub.Netrooms
{
    public class NetworkInteractionsOutlineDisplay : NetworkInteractionsDisplay<NetworkInteractionsOutlineDisplay>
    {
        protected override System CreateSystem() => new OutlineSystem();

        [SerializeField]
        private Material outlineMaterial;

        [SerializeField, Tooltip("Leave as null if stencil is not required.")]
        private Material outlineStencilMaterial;

        [SerializeField]
        private Color localGrabColor = Color.white;

        [SerializeField]
        private bool useRemoteLocalPlayerColor;

        private class OutlineSystem : System
        {
            private BaseMeshOutline outline;

            private int localSelectCount;

            private int remoteSelectCount;

            private bool IsLocalPlayerSelecting => localSelectCount > 0;

            private bool IsRemoteSelecting => remoteSelectCount > 0;

            protected override void InitializeInteractable(NetworkInteractable interactable)
            {
                if(!interactable.TryGetComponent(out outline))
                {
                    outline = interactable.gameObject.AddComponent<MeshOutlineHierarchy>();
                    outline.OutlineMaterial = owner.outlineMaterial;

                    if(owner.outlineStencilMaterial != null)
                    {
                        outline.StencilWriteMaterial = owner.outlineStencilMaterial;
                        outline.UseStencilOutline = true;
                    }
                }

                outline.enabled = false;
            }

            protected override void StartListeningToInteractable(NetworkInteractable interactable)
            {
                interactable.Interactable.selectEntered.AddListener(HandleLocalSelectEntered);
                interactable.Interactable.selectExited.AddListener(HandleLocalSelectExited);
                interactable.OnSelectStarted += HandleNetworkSelectStarted;
                interactable.OnSelectEnded += HandleNetworkSelectEnded;
                interactable.OnHoverStarted += HandleInteractableHoverStarted;
                interactable.OnHoverEnded += HandleInteractableHoverEnded;
            }

            protected override void StopListeningToInteractable(NetworkInteractable interactable)
            {
                interactable.Interactable.selectEntered.RemoveListener(HandleLocalSelectEntered);
                interactable.Interactable.selectExited.RemoveListener(HandleLocalSelectExited);
                interactable.OnSelectStarted -= HandleNetworkSelectStarted;
                interactable.OnSelectEnded -= HandleNetworkSelectEnded;
                interactable.OnHoverStarted -= HandleInteractableHoverStarted;
                interactable.OnHoverEnded -= HandleInteractableHoverEnded;
            }

            private void HandleLocalSelectEntered(SelectEnterEventArgs args)
            {
                localSelectCount++;

                if(remoteSelectCount > 0)
                    return;

                outline.enabled = true;
                outline.OutlineMaterial.color = owner.localGrabColor;
            }

            private void HandleLocalSelectExited(SelectExitEventArgs args)
            {
                localSelectCount--;
                Debug.Assert(localSelectCount >= 0);

                if(remoteSelectCount > 0)
                    return;

                outline.enabled = false;
            }

            private void HandleNetworkSelectStarted(NetworkPlayer player)
            {
                remoteSelectCount++;

                if(player == null)
                    return;

                outline.enabled = true;

                if(owner.useRemoteLocalPlayerColor || !player.IsLocalPlayer)
                    outline.OutlineMaterial.color = player.Color;
                else
                    outline.OutlineMaterial.color = owner.localGrabColor;
            }

            private void HandleNetworkSelectEnded(NetworkPlayer player)
            {
                remoteSelectCount--;
                Debug.Assert(remoteSelectCount >= 0);

                if(IsLocalPlayerSelecting)
                    outline.OutlineMaterial.color = owner.localGrabColor;
                else
                    outline.enabled = false;
            }

            private void HandleInteractableHoverStarted(NetworkPlayer player)
            {

            }

            private void HandleInteractableHoverEnded(NetworkPlayer player)
            {

            }
        }
    }
}