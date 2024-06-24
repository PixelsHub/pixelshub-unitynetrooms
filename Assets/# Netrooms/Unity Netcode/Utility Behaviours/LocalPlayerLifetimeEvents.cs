using UnityEngine;
using UnityEngine.Events;

namespace PixelsHub.Netrooms
{
    public class LocalPlayerLifetimeEvents : MonoBehaviour
    {
        [SerializeField]
        private UnityEvent onLocalPlayerSpawned;

        [SerializeField]
        private UnityEvent onLocalPlayerObjectInstantiated;

        [SerializeField]
        private UnityEvent onLocalPlayerDespawned;

        private void OnEnable()
        {
            NetworkPlayer.OnLocalPlayerSpawned += HandleLocalPlayerSpawned;
            NetworkPlayer.OnLocalPlayerObjectInstantiated += HandleLocalPlayerObjectInstantiated;
            NetworkPlayer.OnLocalPlayerDespawned += HandleLocalPlayerDespawned;
        }

        private void OnDisable()
        {
            NetworkPlayer.OnLocalPlayerSpawned -= HandleLocalPlayerSpawned;
            NetworkPlayer.OnLocalPlayerObjectInstantiated -= HandleLocalPlayerObjectInstantiated;
            NetworkPlayer.OnLocalPlayerDespawned -= HandleLocalPlayerDespawned;
        }

        private void HandleLocalPlayerSpawned(NetworkPlayer localPlayer) 
        {
            onLocalPlayerSpawned.Invoke();
        }

        private void HandleLocalPlayerObjectInstantiated(NetworkPlayer localPlayer)
        {
            onLocalPlayerObjectInstantiated.Invoke();
        }

        private void HandleLocalPlayerDespawned(NetworkPlayer localPlayer)
        {
            onLocalPlayerDespawned.Invoke();
        }
    }
}
