﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure
{
    public enum AttributeType
    {
        UserName = 1,
        UserPassword = 2,
        CHAPPassword = 3,
        NASIPAddress = 4,
        NASPort = 5,
        ServiceType = 6,
        FramedProtocol = 7,
        FramedIPAddress = 8,
        FramedIPNetmask = 9,
        FramedRouting = 10,
        FilterID = 11,
        FramedMTU = 12,
        FramedCompression = 13,
        LoginIPHost = 14,
        LoginService = 15,
        LoginTCPPort = 16,
        ReplyMessage = 18,
        CallbackNumber = 19,
        CallbackId = 20,
        FramedRoute = 22,
        FramedIPXNetwork = 23,
        State = 24,
        Class = 25,
        VendorSpecific = 26,
        SessionTimeout = 27,
        IdleTimeout = 28,
        TerminationAction = 29,
        CalledStationId = 30,
        CallingStationId = 31,
        NASIdentifier = 32,
        ProxyState = 33,
        LoginLATService = 34,
        LoginLATNode = 35,
        LoginLATGroup = 36,
        FramedAppleTalkLink = 37,
        FramedAppleTalkNetwork = 38,
        FramedAppleTalkZone = 39,
        AcctStatusType = 40,
        AcctDelayTime = 41,
        AcctInputOctets = 42,
        AcctOutputOctets = 43,
        AcctSessionId = 44,
        AcctAuthentic = 45,
        AcctSessionTime = 46,
        AcctInputPackets = 47,
        AcctOutputPackets = 48,
        AcctTerminateCause = 49,
        AcctMultiSessionId = 50,
        AcctLinkCount = 51,
        AcctInputGigawords = 52,
        AcctOutputGigawords = 53,
        Unassigned = 54,
        EventTimestamp = 55,
        EgressVLANID = 56,
        IngressFilters = 57,
        EgressVLANName = 58,
        UserPriorityTable = 59,
        CHAPChallenge = 60,
        NASPortType = 61,
        PortLimit = 62,
        LoginLATPort = 63,
        TunnelType = 64,
        TunnelMediumType = 65,
        TunnelClientEndpoint = 66,
        TunnelServerEndpoint = 67,
        AcctTunnelConnection = 68,
        TunnelPassword = 69,
        ARAPPassword = 70,
        ARAPFeatures = 71,
        ARAPZoneAccess = 72,
        ARAPSecurity = 73,
        ARAPSecurityData = 74,
        PasswordRetry = 75,
        Prompt = 76,
        ConnectInfo = 77,
        ConfigurationToken = 78,
        EAPMessage = 79,
        MessageAuthenticator = 80,
        TunnelPrivateGroupID = 81,
        TunnelAssignmentID = 82,
        TunnelPreference = 83,
        ARAPChallengeResponse = 84,
        AcctInterimInterval = 85,
        AcctTunnelPacketsLost = 86,
        NASPortId = 87,
        FramedPool = 88,
        CUI = 89,
        TunnelClientAuthID = 90,
        TunnelServerAuthID = 91,
        NASFilterRule = 92,
        OriginatingLineInfo = 94,
        NASIPv6Address = 95,
        FramedInterfaceId = 96,
        FramedIPv6Prefix = 97,
        LoginIPv6Host = 98,
        FramedIPv6Route = 99,
        FramedIPv6Pool = 100,
        ErrorCause = 101,
        EAPKeyName = 102,
        DigestResponse = 103,
        DigestRealm = 104,
        DigestNonce = 105,
        DigestResponseAuth = 106,
        DigestNextnonce = 107,
        DigestMethod = 108,
        DigestURI = 109,
        DigestQop = 110,
        DigestAlgorithm = 111,
        DigestEntityBodyHash = 112,
        DigestCNonce = 113,
        DigestNonceCount = 114,
        DigestUsername = 115,
        DigestOpaque = 116,
        DigestAuthParam = 117,
        DigestAKAAuts = 118,
        DigestDomain = 119,
        DigestStale = 120,
        DigestHA1 = 121,
        SIPAOR = 122,
        DelegatedIPv6Prefix = 123,
        MIP6FeatureVector = 124,
        MIP6HomeLinkPrefix = 125,
        OperatorName = 126,
        LocationInformation = 127,
        LocationData = 128,
        BasicLocationPolicyRules = 129,
        ExtendedLocationPolicyRules = 130,
        LocationCapable = 131,
        RequestedLocationInfo = 132,
        FramedManagementProtocol = 133,
        ManagementTransportProtection = 134,
        ManagementPolicyId = 135,
        ManagementPrivilegeLevel = 136,
        PKMSSCert = 137,
        PKMCACert = 138,
        PKMConfigSettings = 139,
        PKMCryptosuiteList = 140,
        PKMSAID = 141,
        PKMSADescriptor = 142,
        PKMAuthKey = 143,
        DSLiteTunnelName = 144,
        MobileNodeIdentifier = 145,
        ServiceSelection = 146,
        PMIP6HomeLMAIPv6Address = 147,
        PMIP6VisitedLMAIPv6Address = 148,
        PMIP6HomeLMAIPv4Address = 149,
        PMIP6VisitedLMAIPv4Address = 150,
        PMIP6HomeHNPrefix = 151,
        PMIP6VisitedHNPrefix = 152,
        PMIP6HomeInterfaceID = 153,
        PMIP6VisitedInterfaceID = 154,
        PMIP6HomeIPv4HoA = 155,
        PMIP6VisitedIPv4HoA = 156,
        PMIP6HomeDHCP4ServerAddress = 157,
        PMIP6VisitedDHCP4ServerAddress = 158,
        PMIP6HomeDHCP6ServerAddress = 159,
        PMIP6VisitedDHCP6ServerAddress = 160,
        PMIP6HomeIPv4Gateway = 161,
        PMIP6VisitedIPv4Gateway = 162,
        EAPLowerLayer = 163,
        GSSAcceptorServiceName = 164,
        GSSAcceptorHostName = 165,
        GSSAcceptorServiceSpecifics = 166,
        GSSAcceptorRealmName = 167,
        FramedIPv6Address = 168,
        DNSServerIPv6Address = 169,
        RouteIPv6Information = 170,
        DelegatedIPv6PrefixPool = 171,
        StatefulIPv6AddressPool = 172,
        IPv66rdConfiguration = 173,
        AllowedCalledStationId = 174,
        EAPPeerId = 175,
        EAPServerId = 176,
        MobilityDomainId = 177,
        PreauthTimeout = 178,
        NetworkIdName = 179,
        EAPoLAnnouncement = 180,
        WLANHESSID = 180,
        WLANVenueInfo = 182,
        WLANVenueLanguage = 183,
        WLANVenueName = 184,
        WLANReasonCode = 185,
        WLANPairwiseCipher = 186,
        WLANGroupCipher = 187,
        WLANAKMSuite = 188,
        WLANGroupMgmtCipher = 189,
        WLANRFBand = 190,
    }
}
