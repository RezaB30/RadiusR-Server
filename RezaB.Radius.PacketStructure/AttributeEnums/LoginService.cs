using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum LoginService
    {
        Telnet = 0,
        Rlogin = 1,
        TCPClear = 2,
        PortMaster = 3,
        LAT = 4,
        X25PAD = 5,
        X25T3POS = 6,
        Unassigned = 7,
        TCPClearQuiet = 8,
    }
}
