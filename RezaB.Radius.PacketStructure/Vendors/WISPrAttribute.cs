using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.Vendors
{
    class WISPrAttribute : VendorAttribute
    {
        public new enum Attributes
        {
            WISPrBandwidthMaxDown = 8,
            WISPrBandwidthMaxUp = 7,
            WISPrBandwidthMinDown = 6,
            WISPrBandwidthMinUp = 5,
            WISPrLocationId = 1,
            WISPrLocationName = 2,
            WISPrLogoffURL = 3,
            WISPrRedirectionURL = 4,
            WISPrSessionTerminateTime = 9
        }

        private static short[] _integerList = new short[0];

        public override short[] IntegerList
        {
            get
            {
                return _integerList;
            }
        }

        public override uint VendorID
        {
            get
            {
                return 2352;
            }
        }

        public Attributes VendorType
        {
            get
            {
                return (Attributes)_vendorType;
            }
        }

        public override string VendorTypeName
        {
            get
            {
                return VendorType.ToString();
            }
        }

        public WISPrAttribute(byte[] data, ref int startIndex) : base(data, ref startIndex) { }

        public WISPrAttribute(Attributes vendorType, string value) : base((short)vendorType, value) { }
    }
}
