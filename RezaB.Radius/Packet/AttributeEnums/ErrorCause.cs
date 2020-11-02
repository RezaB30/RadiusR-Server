using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet.AttributeEnums
{
    public enum ErrorCause
    {
        ResidualSessionContextRemoved = 201,
        InvalidEAPPacket_Ignored = 202,
        UnsupportedAttribute = 401,
        MissingAttribute = 402,
        NASIdentificationMismatch = 403,
        InvalidRequest = 404,
        UnsupportedService = 405,
        UnsupportedExtension = 406,
        InvalidAttributeValue = 407,
        AdministrativelyProhibited = 501,
        RequestNotRoutable_Proxy = 502,
        SessionContextNotFound = 503,
        SessionContextNotRemovable = 504,
        OtherProxyProcessingError = 505,
        ResourcesUnavailable = 506,
        RequestInitiated = 507,
        MultipleSessionSelectionUnsupported = 508,
        LocationInfoRequired = 509,
        ResponseTooBig = 601
    }
}
