using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms.Server
{
    public class NetworkHttpServer : HttpServer
    {
        private NetworkManager networkManager;

        private void Start()
        {
            networkManager = NetworkManager.Singleton;

            if(networkManager.IsServer)
                StartServerListening();

            networkManager.OnServerStarted += StartServerListening;
            networkManager.OnServerStopped += StopServerListening;
        }

        protected override void OnDestroy()
        {
            StopServerListening();

            networkManager.OnServerStarted -= StartServerListening;
            networkManager.OnServerStopped -= StopServerListening;

            base.OnDestroy();
        }

        private void StopServerListening(bool isHost) => StopServerListening();
    }
}
