using RezaB.Radius.PacketStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAE
{
    public class DynamicAuthorizationClient : IDisposable
    {
        private UdpClient _client;

        public DynamicAuthorizationClient(int port, int timeout, IPAddress bindedIP = null)
        {
            _client = bindedIP == null ? new UdpClient(port) : new UdpClient(new IPEndPoint(bindedIP, port));
            _client.Client.ReceiveTimeout = _client.Client.SendTimeout = timeout;
        }

        public RadiusPacket Send(IPEndPoint DASEndPoint, DynamicAuthorizationExtentionPacket request, string secret)
        {
            var requestBytes = request.GetBytes(secret);
            _client.Send(requestBytes, requestBytes.Length, DASEndPoint);
            IPEndPoint responderEndPoint = null;
            var responseBytes = _client.Receive(ref responderEndPoint);
            var responsePacket = new RadiusPacket(responseBytes);
            return responsePacket;
        }

        public void Dispose()
        {
            _client.Close();
            _client.Dispose();
        }
    }
}
