using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkClientLifetimeEvents : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onClientStarted;

        [SerializeField]
        private UnityEvent onClientStopped;

        private void OnEnable()
        {
            StartCoroutine(Coroutine());

            IEnumerator Coroutine()
            {
                while(NetworkManager.Singleton == null)
                    yield return null;

                if(enabled)
                {
                    NetworkManager.Singleton.OnClientStarted += HandleClientStarted;
                    NetworkManager.Singleton.OnClientStopped += HandleClieintStopped;
                }
            }
        }

        private void OnDisable()
        {
            if(NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnServerStarted -= HandleClientStarted;
            NetworkManager.Singleton.OnServerStopped -= HandleClieintStopped;
        }

        private void HandleClientStarted() 
        {
            onClientStarted.Invoke();
        }

        private void HandleClieintStopped(bool isHost)
        {
            onClientStopped.Invoke();
        }
    }
}
