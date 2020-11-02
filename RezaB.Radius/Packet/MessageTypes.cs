using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet
{
    public enum MessageTypes
    {
        AccessRequest = 1,
        AccessAccept = 2,
        AccessReject = 3,
        AccountingRequest = 4,
        AccountingResponse = 5,
        AccessChallenge = 11,
        StatusServer = 12,
        StatusClient = 13,
        ResourceFreeRequest = 21,
        ResourceFreeResponse = 22,
        ResourceQueryRequest = 23,
        ResourceQueryResponse = 24,
        AlternateResourceReclaimRequest = 25,
        NASRebootResponse = 27,
        NextPasscode = 29,
        NewPin = 30,
        TerminateSession = 31,
        PasswordExpired = 32,
        EventRequest = 33,
        EventResponse = 34,
        DisconnectRequest = 40,
        DisconnectACK = 41,
        DisconnectNAK = 42,
        CoARequest = 43,
        CoAACK = 44,
        CoANAK = 45,
        IPAddressAllocate = 50,
        IPAddressRelease = 51,
        ProtocolError = 52
    }
}
