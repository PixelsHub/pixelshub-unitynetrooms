using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace PixelsHub.Netrooms
{
    public class NetworkPlayerBasicUserVariables : NetworkPlayerComponent
    {
        public static NetworkPlayerBasicUserVariables Local { get; private set; }

        public static string LocalDisplayName 
        {
            get => localDisplayName;
            set
            {
                value ??= string.Empty;

                localDisplayName = value;

                if(Local != null)
                    Local.displayName.Value = value;
            }
        }

        public static string LocalRole
        {
            get => localRole;
            set
            {
                value ??= string.Empty;

                localRole = value;

                if(Local != null)
                    Local.role.Value = value;
            }
        }

        private static readonly string noRoleValue = string.Empty;

        private static string localDisplayName = string.Empty;
        private static string localRole = noRoleValue;

        protected readonly NetworkVariable<FixedString128Bytes> displayName = new(string.Empty, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        protected readonly NetworkVariable<FixedString128Bytes> role = new(noRoleValue, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        [SerializeField]
        private StringEvent onDisplayNameSet;

        [SerializeField]
        private StringEvent onRoleSet;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            displayName.OnValueChanged += HandleDisplayValueChanged;
            role.OnValueChanged += HandleRoleChanged;

            if(IsLocalPlayer)
            {
                Local = this;

                if(string.IsNullOrEmpty(localDisplayName))
                    localDisplayName = LocalPlayerUserIdentifier.Value;

                displayName.Value = localDisplayName;
                role.Value = localRole;
            }
            else
            {
                onDisplayNameSet.Invoke(displayName.Value.ToString());
                onRoleSet.Invoke(role.Value.ToString());
            }
        }

        public override void OnNetworkDespawn()
        {
            if(IsLocalPlayer)
            {
                Local = null;
            }

            displayName.OnValueChanged -= HandleDisplayValueChanged;
            role.OnValueChanged -= HandleRoleChanged;
        }

        private void HandleDisplayValueChanged(FixedString128Bytes prevValue, FixedString128Bytes newValue)
        {
            onDisplayNameSet.Invoke(newValue.ToString());
        }

        private void HandleRoleChanged(FixedString128Bytes prevValue, FixedString128Bytes newValue)
        {
            onRoleSet.Invoke(newValue.ToString());
        }
    }
}
