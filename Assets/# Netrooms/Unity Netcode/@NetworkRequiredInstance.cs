using System;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    /// <summary>
    /// Singleton NetworkBehaviour that is expected to exist and persist at the start of the scene.
    /// </summary>
    public abstract class NetworkPersistentSingleton<T> : NetworkBehaviour where T : NetworkPersistentSingleton<T>
    {
        public static event Action<T> OnInstanceSet;

        public static T Instance { get; private set; }

        public override void OnDestroy()
        {
            base.OnDestroy();

            if(Instance == this)
            {
                Instance = null;
                OnInstanceSet?.Invoke(null);
            }
        }

        private void Awake()
        {
            Debug.Assert(NetworkObject.DontDestroyWithOwner);

            if(Instance == null)
            {
                Instance = this as T;
                OnInstanceSet?.Invoke(Instance);
            }
            else
            {
                Debug.LogError($"Only one instance of {typeof(T)} is expected. Destroying in \"{name}\"...");
                Destroy(this);
            }
        }
    }

    /// <summary>
    /// Singleton NetworkBehaviour that is required on networking scenes and contains an editor check.
    /// </summary>
    public abstract class NetworkPersistenSingletonRequired<T> : NetworkPersistentSingleton<T> where T : NetworkPersistenSingletonRequired<T>
    {
#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EditorCheckComponentExistsOnScene()
        {
            if(FindFirstObjectByType<NetworkManager>() == null)
                return;

            var components = FindObjectsByType<T>(FindObjectsSortMode.None);

            if(components.Length == 0)
                Debug.LogError($"A Networking scene should include a {typeof(T)} component.");
            else if(components.Length > 1)
                Debug.LogError($"The scene contains more than one {typeof(T)} components. This is not supported.");
        }
#endif
    }
}
