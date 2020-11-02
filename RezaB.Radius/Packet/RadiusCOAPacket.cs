using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RezaB.Radius.Server;

namespace RezaB.Radius.Packet
{
    public class RadiusCOAPacket : RadiusClientPacket
    {
        public RadiusCOAPacket(RadiusPacket basePacket, IEnumerable<RadiusAttribute> additionalAttributes, NasClientCredentials credentials) : base(basePacket, additionalAttributes, credentials)
        {
            Code = MessageTypes.CoARequest;
        }
    }
}
