using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkServerLifetimeEvents : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onServerStarted;

        [SerializeField]
        private UnityEvent onServerStopped;

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
                }
            }
        }

        private void OnDisable()
        {
            if(NetworkManager.Singleton == null)
                return;

            NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
            NetworkManager.Singleton.OnServerStopped -= HandleServerStopped;
        }

        private void HandleServerStarted() 
        {
            onServerStarted.Invoke();
        }

        private void HandleServerStopped(bool isHost)
        {
            onServerStopped.Invoke();
        }
    }
}
