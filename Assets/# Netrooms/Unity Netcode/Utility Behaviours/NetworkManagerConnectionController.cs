using UnityEngine;
using Unity.Netcode;

namespace PixelsHub.Netrooms
{
    public class NetworkManagerConnectionController : MonoBehaviour
    {
        public void StartServer() 
        {
            NetworkManager.Singleton.StartServer();
        }

        public void StartHost() 
        {
            NetworkManager.Singleton.StartHost();
        }

        public void StartClient() 
        {
            NetworkManager.Singleton.StartClient();
        }

        public void Shutdown() 
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}
