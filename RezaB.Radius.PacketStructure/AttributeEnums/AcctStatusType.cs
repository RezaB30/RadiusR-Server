using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum AcctStatusType
    {
        Start = 1,
        Stop = 2,
        InterimUpdate = 3,
        AccountingOn = 7,
        AccountingOff = 8,
        TunnelStart = 9,
        TunnelStop = 10,
        TunnelReject = 11,
        TunnelLinkStart = 12,
        TunnelLinkStop = 13,
        TunnelLinkReject = 14,
        Failed = 15
    }
}
