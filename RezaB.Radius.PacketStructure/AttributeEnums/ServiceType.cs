using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum ServiceType
    {
        Login = 1,
        Framed = 2,
        CallbackLogin = 3,
        CallbackFramed = 4,
        Outbound = 5,
        Administrative = 6,
        NASPrompt = 7,
        AuthenticateOnly = 8,
        CallbackNASPrompt = 9,
        CallCheck = 10,
        CallbackAdministrative = 11,
        Voice = 12,
        Fax = 13,
        ModemRelay = 14,
        IAPPRegister = 15,
        IAPPAPCheck = 16,
        AuthorizeOnly = 17,
        FramedManagement = 18,
        AdditionalAuthorization = 19
    }
}
