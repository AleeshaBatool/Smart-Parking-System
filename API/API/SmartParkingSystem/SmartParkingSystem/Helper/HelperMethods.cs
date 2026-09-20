using System.Net.Sockets;
using System.Net;
using System.Net;
using System.Net.Sockets;

namespace SmartParkingSystem.Helper
{
    public static class HelperMethods
    {
        public static string GetLocalIpAddress()
        {
            string localIp = string.Empty;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530); // Connect to a remote server (Google DNS)
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIp = endPoint?.Address.ToString();
            }
            return localIp;
        }

    }
}
