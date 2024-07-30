using UnityEngine;
using Microsoft.MixedReality.GraphicsTools;
using UnityEngine.XR.Interaction.Toolkit;

namespace PixelsHub.Netrooms
{
    public class NetworkInteractionOutlineVisuals : MonoBehaviour
    {
        [SerializeField]
        private NetworkGrabInteractable grabTarget;

        [SerializeField]
        private MeshOutline outline;

        [SerializeField]
        private Color localGrabColor = Color.white;

        [SerializeField]
        private bool useRemoteLocalPlayerColor;

        private int localGrabsCount;

        private int remoteGrabsCount;

        private bool IsLocalPlayerGrabbing => localGrabsCount > 0;

        private bool IsRemoteGrabbing => remoteGrabsCount > 0;

        private void Awake()
        {
            outline.enabled = false;
        }

        private void OnEnable()
        {
            if(grabTarget != null)
            {
                grabTarget.Interactable.selectEntered.AddListener(HandleLocalGrabSelectEntered);
                grabTarget.Interactable.selectExited.AddListener(HandleLocalGrabSelectExited);
                grabTarget.OnSelectStarted += HandleNetworkGrabStarted;
                grabTarget.OnSelectEnded += HandleNetworkGrabEnded;
            }
        }

        private void OnDisable()
        {
            if(grabTarget != null)
            {
                grabTarget.Interactable.selectEntered.RemoveListener(HandleLocalGrabSelectEntered);
                grabTarget.Interactable.selectExited.RemoveListener(HandleLocalGrabSelectExited);
                grabTarget.OnSelectStarted -= HandleNetworkGrabStarted;
                grabTarget.OnSelectEnded -= HandleNetworkGrabEnded;
            }
        }

        private void HandleLocalGrabSelectEntered(SelectEnterEventArgs args) 
        {
            localGrabsCount++;

            if(remoteGrabsCount > 0)
                return;

            outline.enabled = true;
            outline.OutlineMaterial.color = localGrabColor;
        }

        private void HandleLocalGrabSelectExited(SelectExitEventArgs args)
        {
            localGrabsCount--;
            Debug.Assert(localGrabsCount >= 0);

            if(remoteGrabsCount > 0)
                return;

            outline.enabled = false;
        }

        private void HandleNetworkGrabStarted(NetworkPlayer player)
        {
            remoteGrabsCount++;

            if(player == null)
                return;

            outline.enabled = true;

            if(useRemoteLocalPlayerColor || !player.IsLocalPlayer)
                outline.OutlineMaterial.color = player.Color;
            else
                outline.OutlineMaterial.color = localGrabColor;
        }

        private void HandleNetworkGrabEnded(NetworkPlayer player) 
        {
            remoteGrabsCount--;
            Debug.Assert(remoteGrabsCount >= 0);

            if(IsLocalPlayerGrabbing)
                outline.OutlineMaterial.color = localGrabColor;
            else
                outline.enabled = false;
        }
    }
}