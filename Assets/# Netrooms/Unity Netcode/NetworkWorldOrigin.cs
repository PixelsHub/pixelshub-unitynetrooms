using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkWorldOrigin : NetworkBehaviour
    {
        public static event Action<NetworkWorldOrigin> OnInstanceSet;

        public static NetworkWorldOrigin Instance { get; private set; }

        public static Transform Transform => Instance.transform;

        public override void OnNetworkSpawn()
        {
            Instance = this;
            OnInstanceSet?.Invoke(this);
        }

        public override void OnNetworkDespawn()
        {
            if(Instance == this)
            {
                Instance = null;
                OnInstanceSet?.Invoke(null);
            }
        }
    }
}
