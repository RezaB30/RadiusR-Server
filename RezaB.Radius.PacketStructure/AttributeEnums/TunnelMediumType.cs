using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum TunnelMediumType
    {
        IPv4 = 1,
        IPv6 = 2,
        NSAP = 3,
        HDLC = 4,
        BBN1822 = 5,
        _802 = 6,
        E163 = 7,
        E164 = 8,
        F69 = 9,
        X121 = 10,
        IPX = 11,
        Appletalk = 12,
        DecnetIV = 13,
        BanyanVines = 14,
        E164WithNSAPFormatSubaddress = 15
    }
}
