using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Collections;

namespace PixelsHub.Netrooms
{
    public class NetworkPlayerBasicUserVariables : NetworkPlayerComponent
    {
        public static NetworkPlayerBasicUserVariables Local { get; private set; }

        private static readonly string noRoleValue = "NO_ROLE";

        protected readonly NetworkVariable<FixedString128Bytes> displayName = new(string.Empty);
        protected readonly NetworkVariable<FixedString128Bytes> role = new(string.Empty);

        [SerializeField]
        private StringEvent onDisplayNameSet;

        [SerializeField]
        private StringEvent onRoleSet;

        public void SetDisplayName(string value) => SetStringValue(value, displayName, onDisplayNameSet, SetDisplayNameServerRpc);

        public void SetRole(string value) => SetStringValue(value, role, onRoleSet, SetRoleServerRpc);
        
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(IsLocalPlayer)
            {
                Local = this;

                if(displayName.Value == string.Empty)
                {
                    if(IsServer)
                    {
                        displayName.Value = LocalPlayerUserIdentifier.Value;
                        onDisplayNameSet.Invoke(LocalPlayerUserIdentifier.Value);
                    }
                    else
                        SetDisplayNameServerRpc(LocalPlayerUserIdentifier.Value);
                }

                if(role.Value == string.Empty)
                {
                    if(IsServer)
                    {
                        role.Value = noRoleValue;
                        onRoleSet.Invoke(noRoleValue);
                    }
                    else
                        SetRoleServerRpc(noRoleValue);
                }
            }
            else
            {
                displayName.OnValueChanged += HandleDisplayValueChanged;
                onDisplayNameSet.Invoke(displayName.Value.ToString());

                role.OnValueChanged += HandleRoleChanged;
                onRoleSet.Invoke(role.Value.ToString());
            }
        }

        public override void OnNetworkDespawn()
        {
            if(IsLocalPlayer)
            {
                Local = null;
            }
            else
            {
                displayName.OnValueChanged -= HandleDisplayValueChanged;
                role.OnValueChanged -= HandleRoleChanged;
                onRoleSet.Invoke(role.Value.ToString());
            }
        }

        [Rpc(SendTo.Server)]
        private void SetDisplayNameServerRpc(FixedString128Bytes value)
        {
            displayName.Value = value;
            onDisplayNameSet.Invoke(value.ToString());
        }

        [Rpc(SendTo.Server)]
        private void SetRoleServerRpc(FixedString128Bytes value)
        {
            role.Value = value;
            onRoleSet.Invoke(value.ToString());
        }

        private void HandleDisplayValueChanged(FixedString128Bytes prevValue, FixedString128Bytes newValue)
        {
            onDisplayNameSet.Invoke(newValue.ToString());
        }

        private void HandleRoleChanged(FixedString128Bytes prevValue, FixedString128Bytes newValue)
        {
            onRoleSet.Invoke(newValue.ToString());
        }

        private void SetStringValue(string value, NetworkVariable<FixedString128Bytes> networkVariable, StringEvent stringEvent, Action<FixedString128Bytes> setValueRpc)
        {
            if(!IsServer && !IsLocalPlayer)
            {
                Debug.LogError($"{GetType()} cannot modify a non-local player.");
                return;
            }

            value = value.Trim();

            if(string.IsNullOrEmpty(value))
            {
                Debug.Assert(false);
                return;
            }

            if(IsServer)
            {
                networkVariable.Value = value;
                stringEvent.Invoke(value);
            }
            else
                setValueRpc.Invoke(value);
        }
    }
}
