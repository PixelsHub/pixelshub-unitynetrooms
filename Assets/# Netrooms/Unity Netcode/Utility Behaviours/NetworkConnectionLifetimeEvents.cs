using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;
using System.Collections;

namespace PixelsHub.Netrooms
{
    public class NetworkConnectionLifetimeEvents : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onConnectionStarted;

        [SerializeField]
        private UnityEvent onConnectionStopped;

        private void OnEnable()
        {
            StartCoroutine(Coroutine());

            IEnumerator Coroutine()
            {
                while(NetworkManager.Singleton == null)
                    yield return null;

                if(enabled)
                {
                    NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
                    NetworkManager.Singleton.OnServerStopped += HandleServerStopped;
                    NetworkManager.Singleton.OnClientStarted += HandleClientStarted;
                    NetworkManager.Singleton.OnClientStopped += HandleClientStopped;
                }
            }
        }

        private void OnDisable()
        {
            if(NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnServerStopped -= HandleServerStopped;
            NetworkManager.Singleton.OnClientStarted -= HandleClientStarted;
            NetworkManager.Singleton.OnClientStopped -= HandleClientStopped;
        }

        private void HandleServerStarted()
        {
            if(NetworkManager.Singleton.IsHost)
                return;

            onConnectionStarted.Invoke();
        }

        private void HandleServerStopped(bool isHost)
        {
            if(isHost)
                return;

            onConnectionStopped.Invoke();
        }

        private void HandleClientStarted()
        {
            onConnectionStarted.Invoke();
        }

        private void HandleClientStopped(bool isHost)
        {
            onConnectionStopped.Invoke();
        }
    }
}
