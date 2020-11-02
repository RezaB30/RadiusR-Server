using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet.AttributeEnums
{
    public enum LocationCapable
    {
        CIVIC_LOCATION = 1,
        GEO_LOCATION = 2,
        USERS_LOCATION = 4,
        NAS_LOCATION = 8
    }
}
