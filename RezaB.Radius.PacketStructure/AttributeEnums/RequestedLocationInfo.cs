using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum RequestedLocationInfo
    {
        CIVIC_LOCATION = 1,
        GEO_LOCATION = 2,
        USERS_LOCATION = 4,
        NAS_LOCATION = 8,
        FUTURE_REQUESTS = 16,
        NONE = 32
    }
}
