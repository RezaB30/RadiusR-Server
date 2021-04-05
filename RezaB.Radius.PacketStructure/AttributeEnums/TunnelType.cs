using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum TunnelType
    {
        PPTP = 1,
        L2F = 2,
        L2TP = 3,
        ATMP = 4,
        VTP = 5,
        AH = 6,
        IP_IP = 7,
        MIN_IP_IP = 8,
        ESP = 9,
        GRE = 10,
        DVS = 11,
        IPinIPTunneling = 12,
        VLAN = 13
    }
}
