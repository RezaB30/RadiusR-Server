using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Vendors
{
    class MicrosoftAttribute : VendorAttribute
    {
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public enum Attributes
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
        {
            MSCHAPChallenge = 11,
            MSCHAPDomain = 10,
            MSCHAPResponse = 1,
            MSCHAP2Response = 25,
            MSCHAP2Success = 26,
            MSMPPEEncryptionPolicy = 7,
            MSMPPEEncryptionTypes = 8,
            MSMPPERecvKey = 17,
            MSMPPESendKey = 16
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
                return 311;
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

        public MicrosoftAttribute(byte[] data, ref int startIndex) : base(data, ref startIndex) { }

        public MicrosoftAttribute(Attributes vendorType, string value) : base((short)vendorType, value) { }
    }
}
