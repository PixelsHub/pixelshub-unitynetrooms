using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace PixelsHub.Netrooms
{
    public class NetworkUtilities
    {
        public static UnityTransport Transport
        {
            get 
            {
                if(transport == null)
                    transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;

                return transport;
            }
        }

        public static string TransportConnectionAddress => Transport.ConnectionData.Address;

        private static UnityTransport transport;
    }
}
