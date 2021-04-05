using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum ARAPZoneAccess
    {
        OnlyAllowAccessToDefaultZone = 1,
        UseZoneFilterInclusively = 2,
        NotUsed = 3,
        UseZoneFilterExclusively = 4
    }
}
