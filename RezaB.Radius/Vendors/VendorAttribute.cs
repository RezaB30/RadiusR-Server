using RezaB.Radius.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Vendors
{
    public abstract class VendorAttribute : RadiusAttribute
    {
        public abstract uint VendorID { get; }

        public abstract string VendorTypeName { get; }

        protected byte _vendorType;

        public enum Attributes { }

        public new string Value
        {
            get
            {
                if (IntegerList.Contains(_vendorType))
                {
                    return BitConverter.ToUInt32(RawValue.Take(4).Reverse().ToArray(), 0).ToString();
                }
                return Encoding.UTF8.GetString(RawValue, 0, RawValue.Length);
            }
            set
            {
                RawValue = Encoding.UTF8.GetBytes(value);
            }
        }

        public abstract short[] IntegerList { get; }

        public VendorAttribute(short vendorType, string value)
        {
            Type = AttributeType.VendorSpecific;
            _vendorType = (byte)vendorType;
            Value = value;
        }

        public VendorAttribute(byte[] data, ref int startIndex)
        {
            Type = AttributeType.VendorSpecific;
            // get length of attribute
            Length = data[startIndex];
            startIndex++;
            // skip vendor id
            startIndex += 4;
            // get vendor type
            _vendorType = data[startIndex];
            startIndex++;
            // get internal buffer length
            var internalLength = data[startIndex];
            startIndex++;
            // get value of attribute
            RawValue = data.Skip(startIndex).Take(internalLength - 2).ToArray();
            startIndex += internalLength - 2;

        }

        public VendorAttribute() { Type = AttributeType.VendorSpecific; }

        public override sealed byte[] GetBytes()
        {
            var bytes = new List<byte>();
            // attribute type = vendor specific
            bytes.Add((byte)Type);
            // leave room for attribute length
            bytes.Add(0);
            // add vendor id
            bytes.AddRange(BitConverter.GetBytes(VendorID).Reverse().ToArray());
            // seperate vendor attribute internal buffer
            var internalBytes = new List<byte>();
            // add vendor type
            internalBytes.Add(_vendorType);
            // leave room for vendor length
            internalBytes.Add(0);
            // add the value
            internalBytes.AddRange(RawValue);
            // set internal bytes length
            internalBytes[1] = (byte)internalBytes.Count();
            // add internal buffer to the whole attribute buffer
            bytes.AddRange(internalBytes);
            // set the length of attribute
            bytes[1] = (byte)bytes.Count();

            return bytes.ToArray();
        }
    }
}
