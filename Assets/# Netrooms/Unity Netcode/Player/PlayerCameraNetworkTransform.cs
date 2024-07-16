using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode.Components;
using System;

namespace PixelsHub.Netrooms
{
    [DisallowMultipleComponent]
    [Obsolete]
    public class PlayerCameraNetworkTransform : NetworkTransform
    {
        protected override bool OnIsServerAuthoritative() => false;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if(IsServer)
            {
                if(NetworkWorldOrigin.Instance != null)
                    SetupLocalTransformParenting(NetworkWorldOrigin.Instance);

                NetworkWorldOrigin.OnInstanceSet += SetupLocalTransformParenting;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if(IsServer)
            {
                NetworkWorldOrigin.OnInstanceSet -= SetupLocalTransformParenting;
            }
        }

        private void LateUpdate()
        {
            if(IsLocalPlayer && LocalPlayerRig.Instance != null)
            {
                var t = LocalPlayerRig.Instance.Camera.transform;
                transform.SetPositionAndRotation(t.position, t.rotation);
            }
        }

        private void SetupLocalTransformParenting(NetworkWorldOrigin origin) 
        {
            if(origin == null)
                return;

            transform.SetParent(origin.transform);
        }
    }
}
