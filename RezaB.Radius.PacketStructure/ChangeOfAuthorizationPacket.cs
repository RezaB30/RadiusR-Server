using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure
{
    public class ChangeOfAuthorizationPacket : DynamicAuthorizationExtentionPacket
    {
        public ChangeOfAuthorizationPacket(RadiusPacket basePacket, IEnumerable<RadiusAttribute> additionalAttributes) : base(basePacket, additionalAttributes)
        {
            Code = MessageTypes.CoARequest;
        }
    }
}
