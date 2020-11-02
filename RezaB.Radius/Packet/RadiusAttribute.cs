using RezaB.Radius.Packet.AttributeEnums;
using RezaB.Radius.Vendors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet
{
    public class RadiusAttribute
    {
        public AttributeType Type { get; set; }

        public byte Length { get; set; }

        public string Value
        {
            get
            {
                return RadiusAttributeConvertor.GetValue(Type, RawValue);
            }
            set
            {
                RawValue = Encoding.UTF8.GetBytes(value);
            }
        }

        public byte[] RawValue { get; set; }

        public string EnumValue
        {
            get
            {
                switch (Type)
                {
                    case AttributeType.ServiceType:
                        return ((ServiceType)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.FramedProtocol:
                        return ((FramedProtocol)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.FramedRouting:
                        return ((FramedRouting)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.FramedCompression:
                        return ((FramedCompression)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.LoginService:
                        return ((LoginService)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.TerminationAction:
                        return ((TerminationAction)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.AcctStatusType:
                        return ((AcctStatusType)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.AcctAuthentic:
                        return ((AcctAuthentic)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.AcctTerminateCause:
                        return ((AcctTerminateCause)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.NASPortType:
                        return ((NASPortType)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.TunnelType:
                        return ((TunnelType)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.TunnelMediumType:
                        return ((TunnelMediumType)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.ARAPZoneAccess:
                        return ((ARAPZoneAccess)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.Prompt:
                        return ((Prompt)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.ErrorCause:
                        return ((ErrorCause)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.LocationInformation:
                        return ((LocationInformation)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.LocationCapable:
                        return ((LocationCapable)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.RequestedLocationInfo:
                        return ((RequestedLocationInfo)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.FramedManagementProtocol:
                        return ((FramedManagementProtocol)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    case AttributeType.ManagementTransportProtection:
                        return ((ManagementTransportProtection)BitConverter.ToUInt32(RawValue.Reverse().ToArray(),0)).ToString();
                    default:
                        return null;
                }
            }
        }

        public RadiusAttribute() { }

        public RadiusAttribute(AttributeType type, string value)
        {
            if (type == AttributeType.VendorSpecific)
                throw new InvalidOperationException("Can not add this type of attribute, use new vendor attribute instead!");
            RawValue = RadiusAttributeConvertor.GetBytes(type, value);
            Type = type;
            //Value = value;
        }

        public RadiusAttribute(string name, string value)
        {
            Type = AttributeType.VendorSpecific;
            Value = value;
        }

        public RadiusAttribute(AttributeType type, int value)
        {
            Type = type;
            RawValue = BitConverter.GetBytes((uint)value).Reverse().ToArray();
        }

        public static RadiusAttribute Read(byte[] data, ref int startIndex)
        {
            // get attribute type
            var type = (AttributeType)data[startIndex];
            startIndex++;
            RadiusAttribute result;
            // normal attributes
            if (type != AttributeType.VendorSpecific)
            {
                result = new RadiusAttribute();
                result.Type = type;
                // get length of attribute
                result.Length = data[startIndex];
                startIndex++;
                // get value of attribute
                result.RawValue = data.Skip(startIndex).Take(result.Length - 2).ToArray();
                startIndex += result.Length - 2;
            }
            // vendor specific attributes
            else
            {
                var vendor = (VendorTypes)BitConverter.ToUInt32(data.Skip(startIndex + 1).Take(4).Reverse().ToArray(), 0);
                switch (vendor)
                {
                    case VendorTypes.Mikrotik:
                        result = new MikrotikAttribute(data, ref startIndex);
                        break;
                    case VendorTypes.Microsoft:
                        result = new MicrosoftAttribute(data, ref startIndex);
                        break;
                    case VendorTypes.RedBack:
                        result = new RedBackAttribute(data, ref startIndex);
                        break;
                    case VendorTypes.Vendor529:
                        result = new Vendor529Attribute(data, ref startIndex);
                        break;
                    case VendorTypes.WISPr:
                        result = new WISPrAttribute(data, ref startIndex);
                        break;
                    default:
                        var length = data[startIndex];
                        startIndex += length - 1;
                        return null;
                }
            }

            return result;
        }

        public virtual byte[] GetBytes()
        {
            var data = new List<byte>();
            data.Add((byte)Type);
            data.Add(0);

            data.AddRange(RawValue);

            data[1] = (byte)data.Count();

            return data.ToArray();
        }
    }
}
