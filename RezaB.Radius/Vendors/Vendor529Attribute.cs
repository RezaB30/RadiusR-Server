using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Vendors
{
    class Vendor529Attribute : VendorAttribute
    {
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public enum Attributes
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
        {
            AscendClientGateway = 132,
            AscendDataRate = 197,
            AscendXmitRate = 255
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
                return 529;
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

        public Vendor529Attribute(byte[] data, ref int startIndex) : base(data, ref startIndex) { }

        public Vendor529Attribute(Attributes vendorType, string value) : base((short)vendorType, value) { }
    }
}
