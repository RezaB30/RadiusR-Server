using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.AttributeEnums
{
    public enum NASPortType
    {
        Async = 0,
        Sync = 1,
        ISDNSync = 2,
        ISDNAsyncV120 = 3,
        ISDNAsyncV110 = 4,
        Virtual = 5,
        PIAFS = 6,
        HDLC = 7,
        X25 = 8,
        X75 = 9,
        G3Fax = 10,
        SDSL = 11,
        ADSLCAP = 12,
        ADSLDMT = 13,
        IDSL = 14,
        Ethernet = 15,
        xDSL = 16,
        Cable = 17,
        WirelessOther = 18,
        WirelessIEEE80211 = 19,
        TokenRing = 20,
        FDDI = 21,
        WirelessCDMA2000 = 22,
        WirelessUMTS = 23,
        Wireless1XEV = 24,
        IAPP = 25,
        FTTP = 26,
        WirelessIEEE80216 = 27,
        WirelessIEEE80220 = 28,
        WirelessIEEE80222 = 29,
        PPPoA = 30,
        PPPoEoA = 31,
        PPPoEoE = 32,
        PPPoEoVLAN = 33,
        PPPoEoQinQ = 34,
        xPON = 35,
        WirelessXGP = 36,
        WiMAXPreRelease8IWKFunction = 37,
        WIMAXWIFIIWK = 38,
        WIMAXSFF = 39,
        WIMAXHALMA = 40,
        WIMAXDHCP = 41,
        WIMAXLBS = 42,
        WIMAXWVS = 43
    }
}
