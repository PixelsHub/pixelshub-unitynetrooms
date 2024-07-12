using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public abstract class NetworkPersistentSingleton<T> : NetworkBehaviour where T : NetworkPersistentSingleton<T>
    {
        public static T Instance { get; private set; }

        public override void OnDestroy()
        {
            base.OnDestroy();
        
            if(Instance == this)
                Instance = null;

            Debug.Assert(NetworkObject.DontDestroyWithOwner);
        }

        private void Awake()
        {
            if(Instance == null)
                Instance = this as T;
            else
            {
                Debug.LogError($"Only one instance of {typeof(T)} is expected. Destroying in \"{name}\"...");
                Destroy(this);
            }
        }
    }
}
