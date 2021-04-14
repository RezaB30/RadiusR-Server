using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAE.TestUnit
{
    class RadiusAttributeListItem
    {
        public RezaB.Radius.PacketStructure.AttributeType Type { get; set; }

        public RezaB.Radius.PacketStructure.Vendors.MikrotikAttribute.Attributes? MikrotikType { get; set; }

        public string Value { get; set; }

        public override string ToString()
        {
            return $"Attribute: {Type}, {(MikrotikType.HasValue ? $"Mikrotik Type: {MikrotikType}, " : null )}Value: {Value}";
        }
    }
}
