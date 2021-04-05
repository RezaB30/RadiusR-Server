using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.Vendors
{
    public class MikrotikAttribute : VendorAttribute
    {
        public readonly uint _vendorId = 14988;

        public new enum Attributes
        {
            MikrotikRecvLimit = 1,
            MikrotikXmitLimit = 2,
            MikrotikGroup = 3,
            MikrotikWirelessForward = 4,
            MikrotikWirelessSkipDot1x = 5,
            MikrotikWirelessEncAlgo = 6,
            MikrotikWirelessEncKey = 7,
            MikrotikRateLimit = 8,
            MikrotikRealm = 9,
            MikrotikHostIP = 10,
            MikrotikMarkId = 11,
            MikrotikAdvertiseURL = 12,
            MikrotikAdvertiseInterval = 13,
            MikrotikRecvLimitGigawords = 14,
            MikrotikXmitLimitGigawords = 15,
            MikrotikWirelessPSK = 16,
            MikrotikTotalLimit = 17,
            MikrotikTotalLimitGigawords = 18,
            MikrotikAddressList = 19,
            MikrotikWirelessMPKey = 20,
            MikrotikWirelessComment = 21,
            MikrotikDelegatedIPv6Pool = 22,
            MikrotikDHCPOptionSet = 23,
            MikrotikDHCPOptionParamSTR1 = 24,
            MikortikDHCPOptionParamSTR2 = 25,
            MikrotikWirelessVLANID = 26,
            MikrotikWirelessVLANIDtype = 27,
            MikrotikWirelessMinsignal = 28,
            MikrotikWirelessMaxsignal = 29
        }

        public Attributes VendorType
        {
            get
            {
                return (Attributes)_vendorType;
            }
        }

        private static short[] _integerList = new short[]
        {
            1,2,4,5,6,10,13,14,15,17,18,26,27
        };

        public override uint VendorID
        {
            get
            {
                return _vendorId;
            }
        }

        public override short[] IntegerList
        {
            get
            {
                return _integerList;
            }
        }

        public override string VendorTypeName
        {
            get
            {
                return VendorType.ToString();
            }
        }

        public MikrotikAttribute(byte[] data, ref int startIndex) : base(data, ref startIndex) { }

        public MikrotikAttribute(Attributes vendorType, string value) : base((short)vendorType, value) { }
    }
}
