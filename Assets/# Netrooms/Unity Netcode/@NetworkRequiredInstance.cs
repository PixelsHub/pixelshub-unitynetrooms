using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkRequiredInstance<T> : NetworkBehaviour where T : NetworkRequiredInstance<T>
    {
        public static T Instance { get; private set; }

        public override void OnDestroy()
        {
            base.OnDestroy();
        
            if(Instance == this)
                Instance = null;
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

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EditorCheckComponentExistsOnScene()
        {
            if(FindFirstObjectByType<NetworkManager>() != null && FindFirstObjectByType<T>() == null)
            {
                Debug.LogError($"A Networking scene should include a {typeof(T)} component.");
            }
        }
#endif
    }
}
