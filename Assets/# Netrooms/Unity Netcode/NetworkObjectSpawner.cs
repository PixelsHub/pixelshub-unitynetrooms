using System;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkObjectSpawner : MonoBehaviour
    {
        [Serializable]
        private struct SpawnData
        {
            public NetworkObject prefab;
            public ulong ownerClientId;
            public bool destroyWithScene;
            public bool isPlayerObject;
        }

        [SerializeField]
        private bool spawnOnServerStart = true;

        [Space(8)]
        [SerializeField]
        private SpawnData[] spawnData;

        private NetworkManager networkManager;

        public void Spawn()
        {
            foreach(var data in spawnData)
                Spawn(data);
        }

        private void Start()
        {
            if(spawnOnServerStart)
            {
                networkManager = NetworkManager.Singleton;
                networkManager.OnServerStarted += Spawn;
            }
        }

        private void OnDestroy()
        {
            networkManager.OnServerStarted -= Spawn;
        }

        private void Spawn(SpawnData data)
        {
            var instance = Instantiate(data.prefab);
            instance.Spawn();
        }
    }
}
