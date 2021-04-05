using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure.Vendors
{
    class RedBackAttribute : VendorAttribute
    {
        public new enum Attributes
        {
            RedbackAgentRemoteId = 96,
            RedbackAgentCircuitId = 97
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

        public RedBackAttribute(byte[] data, ref int startIndex) : base(data, ref startIndex) { }

        public RedBackAttribute(Attributes vendorType, string value) : base((short)vendorType, value) { }
    }
}
