using RadiusR.DB.Enums;
using RezaB.Radius.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius
{
    public class ChangeOfAccountingRequest
    {
        public RadiusClientPacket Packet { get; set; }

        public SMSType? SMSType { get; set; }

        public RadiusPacket.SubscriptionStateChange SubscriptionState { get; set; }
    }
}
