using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

namespace PixelsHub.Netrooms
{
    public class NetworkUtilities
    {
        public static string GetServerAddress() => ((UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport).ConnectionData.Address;
    }
}
