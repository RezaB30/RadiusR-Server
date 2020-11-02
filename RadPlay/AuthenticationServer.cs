using RezaB.Radius.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadPlay
{
    public class AuthenticationServer
    {
        protected static UdpClient server;

        public void Start()
        {
            Thread listeningThread = new Thread(new ParameterizedThreadStart(port => Listen((int)port)));
            listeningThread.Start(1812);
        }

        protected static void Listen(int port)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, port);
            server = new UdpClient(port);
            Console.WriteLine("Listening to port 1812");
            while (true)
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 0);
                var data = server.Receive(ref endpoint);
                Console.WriteLine("received data from: " + endpoint.ToString());

                Thread process = new Thread(new ThreadStart(() => ProcessPacket(data, endpoint)));
                process.Start();
                //break;
            }
        }

        protected static void ProcessPacket(byte[] data, IPEndPoint clientAddress)
        {

            var packet = new RadiusPacket(data);
            Console.WriteLine(Encoding.ASCII.GetString(data));
            StringBuilder logText = new StringBuilder();

            logText.AppendLine("Access request with id " + packet.Identifier + " from: " + clientAddress);
            logText.AppendLine("===============================================================");
            logText.AppendLine("Signiture: " + packet.Signiture);
            logText.AppendLine("received Packet:");
            foreach (var attribute in packet.Attributes)
            {
                logText.AppendLine(attribute.Type.ToString() + ": " + ((string.IsNullOrEmpty(attribute.EnumValue)) ? attribute.Value : attribute.EnumValue));
            }
            logText.AppendLine("===============================================================");
            Console.Write(logText);

            // ------doing-process--------

            // ---------------------------

            Console.WriteLine("Preparing sending packet...");
            var acceptPacket = packet.GetAcceptPacket();
            var toSendBytes = acceptPacket.GetBytes("123456");

            logText = new StringBuilder();
            logText.AppendLine("===============================================================");
            logText.AppendLine("response Packet with id " + acceptPacket.Identifier + ":");
            logText.AppendLine("Signiture: " + acceptPacket.Signiture);
            foreach (var attribute in acceptPacket.Attributes)
            {
                logText.AppendLine(attribute.Type.ToString() + ": " + ((string.IsNullOrEmpty(attribute.EnumValue)) ? attribute.Value : attribute.EnumValue));
            }
            logText.AppendLine("===============================================================");
            Console.Write(logText);

            Console.WriteLine("Sending response to " + clientAddress.ToString() + " ...");
            // sending response
            server.Send(toSendBytes, toSendBytes.Length, clientAddress);
            Console.WriteLine("Response sent.");
            //client.Close();
        }
    }
}
