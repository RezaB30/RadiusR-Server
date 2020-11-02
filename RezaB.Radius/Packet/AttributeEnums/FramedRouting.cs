using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet.AttributeEnums
{
    public enum FramedRouting
    {
        None = 0,
        SendRoutingPackets = 1,
        ListenForRoutingPackets = 2,
        SendAndListen = 3
    }
}
