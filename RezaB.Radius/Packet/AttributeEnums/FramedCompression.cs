using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet.AttributeEnums
{
    public enum FramedCompression
    {
        None = 0,
        VJ_TCP_IP_HeaderCompression = 1,
        IPXHeaderCompression = 2,
        StacLZSCompression = 3
    }
}
