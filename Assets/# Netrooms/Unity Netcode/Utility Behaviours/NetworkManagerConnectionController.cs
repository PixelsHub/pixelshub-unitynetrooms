using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace PixelsHub.Netrooms
{
    public class NetworkManagerConnectionController : MonoBehaviour
    {
        private const string lastConnectionAddresskey = "NETWORK_CONNECTION_ADDRESS";

        private static string LastConnectionAddress
        {
            get => PlayerPrefs.GetString(lastConnectionAddresskey, "192.168.0.111");
            set => PlayerPrefs.SetString(lastConnectionAddresskey, value);
        }

        [SerializeField]
        private StringEvent onConnectionAddressSet;

        public void SetConnectionAddress(string address) 
        {
            address = address.Trim();

            UnityTransport transport = NetworkUtilities.Transport;

            if(address != string.Empty)
            {
                transport.ConnectionData.Address = address;
                LastConnectionAddress = address;
            }
            else
                address = transport.ConnectionData.Address;

            onConnectionAddressSet.Invoke(address);
        }

        public void StartServer() => NetworkManager.Singleton.StartServer();
        
        public void StartHost() => NetworkManager.Singleton.StartHost();
        
        public void StartClient() => NetworkManager.Singleton.StartClient();
        
        public void Shutdown() => NetworkManager.Singleton.Shutdown();
        
        private void Start()
        {
#if !UNITY_EDITOR
            SetConnectionAddress(LastConnectionAddress);
#else
            onConnectionAddressSet.Invoke(NetworkUtilities.TransportConnectionAddress);
#endif
        }
    }
}
