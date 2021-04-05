using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure
{
    public static class RadiusAttributeConvertor
    {
        private static short[] IntegerList = new short[] 
        {
            5,6,7,10,12,13,15,16,27,28,29,37,38,40,41,42,43,45,46,47,48,49,51,52,53,55,56,57,61,62,64,65,72,73,75,76,83,85,86,101,131,132,133,134,136,163,177,178,182,185,186,187,188,189,190
        };

        private static short[] IPV4List = new short[]
        {
            4,8,9,14,23,149,150,157,158,161,162
        };

        public static string GetValue(AttributeType AttributeType, byte[] rawBytes)
        {
            if (IntegerList.Contains((short)AttributeType))
            {
                return BitConverter.ToUInt32(rawBytes.Take(4).Reverse().ToArray(),0).ToString();
            }
            if (IPV4List.Contains((short)AttributeType))
            {
                return string.Join(".", rawBytes.Take(4).Select(b => b.ToString()));
            }
            if (AttributeType == AttributeType.UserPassword)
            {
                return Convert.ToBase64String(rawBytes);
            }
            return Encoding.UTF8.GetString(rawBytes);
        }

        public static byte[] GetBytes(AttributeType AttributeType, string value)
        {
            if (AttributeType == AttributeType.VendorSpecific)
                throw new FormatException("Converting to bytes is not supported for vendor attributes in radius attribute converter, Use vendor attribute converter instead.");
            if (IntegerList.Contains((short)AttributeType))
            {
                return BitConverter.GetBytes(uint.Parse(value)).Reverse().ToArray();
            }
            if (IPV4List.Contains((short)AttributeType))
            {
                return value.Split(new char[] { '.' }).Select(s => byte.Parse(s)).ToArray();
            }

            return Encoding.UTF8.GetBytes(value);
        }
    }
}
