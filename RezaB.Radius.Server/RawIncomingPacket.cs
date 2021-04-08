using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server
{
    public class RawIncomingPacket
    {
        public byte[] Data { get; set; }

        public IPEndPoint EndPoint { get; set; }
    }
}
