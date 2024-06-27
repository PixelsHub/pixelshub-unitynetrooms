#if UNITY_EDITOR || UNITY_STANDALONE
using System.Net;
using System.Net.Sockets;
#endif
using UnityEngine;

namespace PixelsHub.Netrooms
{
    public class DeviceNetworkInformation
    {
        public static string LocalIPAddress
        {
            get 
            {
                localIPAddress ??= GetLocalIPAddress();
                return localIPAddress;
            }
        }

        private static string localIPAddress;

        private static string GetLocalIPAddress() 
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN
            return GetLocalIPAddressWindows();
#elif UNITY_ANDROID
            return GetLocalIPAddressAndroid();
#endif
        }

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
        private static string GetLocalIPAddressWindows() 
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach(var ip in host.AddressList)
            {
                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }

            Debug.LogError("No network adapters with an IPv4 address in the system could be found (Windows).");
            return string.Empty;
        }
#endif

#if UNITY_EDITOR || UNITY_ANDROID
        private static string GetLocalIPAddressAndroid() 
        {
            try
            {
                using var wifiManager = new AndroidJavaObject("android.net.wifi.WifiManager");
                using var wifiInfo = wifiManager.Call<AndroidJavaObject>("getConnectionInfo");

                int ipAddress = wifiInfo.Call<int>("getIpAddress");

                return string.Format("{0}.{1}.{2}.{3}",
                    (ipAddress & 0xff),
                    (ipAddress >> 8 & 0xff),
                    (ipAddress >> 16 & 0xff),
                    (ipAddress >> 24 & 0xff));
            }
            catch(System.Exception ex)
            {
                Debug.LogError($"Failed to get local IP address: {ex.Message}");
                return string.Empty;
            }
        }
#endif
    }
}
