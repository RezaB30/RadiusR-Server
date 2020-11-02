using RezaB.Radius.Packet;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RadiusRService
{
    public partial class RadiusRService : ServiceBase
    {
        public RadiusRService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 1812);
            //UdpClient server = new UdpClient(1812);
            //IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 1812);
            //Console.WriteLine("Listening to port 1812");
            //var data = server.Receive(ref endpoint);
            //Console.WriteLine("received data from: ", endpoint.ToString());
            //var packet = new RadiusPacket(data);
            //Console.WriteLine(Encoding.UTF8.GetString(data));
            //Console.WriteLine("===============================================================");
            //Console.WriteLine("received Packet:");
            //foreach (var attribute in packet.Attributes)
            //{
            //    Console.WriteLine(attribute.Type.ToString() + ": " + ((string.IsNullOrEmpty(attribute.EnumValue)) ? attribute.Value : attribute.EnumValue));
            //}
            //Console.WriteLine("===============================================================");
            //Console.ReadKey();
        }

        protected override void OnStop()
        {
        }
    }
}
