using System.Net.Sockets;
using System.Text;

namespace HR_Products.Services
{
    public class ZKTecoRawService
    {
        private readonly string _ip;
        private readonly int _port;

        public ZKTecoRawService(string ip = "192.168.1.100", int port = 4370)
        {
            _ip = ip;
            _port = port;
        }

        // Example: Get device serial number
        public string GetSerialNumber()
        {
            using (var client = new TcpClient(_ip, _port))
            using (var stream = client.GetStream())
            {
                byte[] cmd = { 0x50, 0x00, 0x00, 0x00 }; // Command for serial#
                stream.Write(cmd, 0, cmd.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                return Encoding.ASCII.GetString(buffer, 8, bytesRead - 8); // Skip header
            }
        }

        // Add more commands as needed (see protocol docs)
    }
}