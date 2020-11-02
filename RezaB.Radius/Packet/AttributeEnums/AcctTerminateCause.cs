using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet.AttributeEnums
{
    public enum AcctTerminateCause
    {
        UserRequest = 1,
        LostCarrier = 2,
        LostService = 3,
        IdleTimeout = 4,
        SessionTimeout = 5,
        AdminReset = 6,
        AdminReboot = 7,
        PortError = 8,
        NASError = 9,
        NASRequest = 10,
        NASReboot = 11,
        PortUnneeded = 12,
        PortPreempted = 13,
        PortSuspended = 14,
        ServiceUnavailable = 15,
        Callback = 16,
        UserError = 17,
        HostRequest = 18,
        SupplicantRestart = 19,
        ReauthenticationFailure = 20,
        PortReinitialized = 21,
        PortAdministrativelyDisabled = 22,
        LostPower = 23
    }
}
