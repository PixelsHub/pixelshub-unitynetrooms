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
            private IMeshOutlineIndividual outline;

            private int localSelectCount;

            private int remoteSelectCount;

            private bool IsLocalPlayerSelecting => localSelectCount > 0;

            private bool IsRemoteSelecting => remoteSelectCount > 0;

            protected override void InitializeInteractable(NetworkInteractable interactable)
            {
                if(!interactable.TryGetComponent(out outline))
                {
                    var outline = interactable.gameObject.AddComponent<MeshOutlineHierarchyIndividual>();
                    outline.enabled = false;

                    outline.OutlineMaterial = owner.outlineMaterial;

                    if(owner.outlineStencilMaterial != null)
                    {
                        outline.StencilWriteMaterial = owner.outlineStencilMaterial;
                        outline.UseStencilOutline = true;
                    }

                    this.outline = outline;
                }
                else
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

                if(owner.useRemoteLocalPlayerColor && NetworkPlayer.Local != null)
                    outline.SetOutlineIndividualColor(NetworkPlayer.Local.Color);
                else
                    outline.SetOutlineIndividualColor(owner.localGrabColor);
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

                if(player.IsLocalPlayer) // Local player must have been set beforehand during local selection
                    return;

                outline.enabled = true;
                outline.SetOutlineIndividualColor(player.Color);
            }

            private void HandleNetworkSelectEnded(NetworkPlayer player)
            {
                remoteSelectCount--;
                Debug.Assert(remoteSelectCount >= 0);

                if(!player.IsLocalPlayer || !IsLocalPlayerSelecting)
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