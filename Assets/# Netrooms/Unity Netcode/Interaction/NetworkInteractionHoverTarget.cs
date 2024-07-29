using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    [RequireComponent(typeof(XRBaseInteractable))]
    public class NetworkInteractionHoverTarget : NetworkBehaviour
    {
        public event Action<bool> OnLocalPlayerAllowedToInteractChanged;

        public bool IsLocalPlayerAllowedToInteract
        {
            get => isLocalPlayerAllowedToInteract;
            set
            {
                if(isLocalPlayerAllowedToInteract != value)
                {
                    isLocalPlayerAllowedToInteract = value;
                    OnLocalPlayerAllowedToInteractChanged?.Invoke(value);
                }
            }
        }

        private XRBaseInteractable interactable;

        [SerializeField]
        private bool isLocalPlayerAllowedToInteract = true;

        private readonly NetworkList<ulong> hovers = 
            new(null, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        private void Awake()
        {
            interactable = GetComponent<XRBaseInteractable>();
        }


    }
}
