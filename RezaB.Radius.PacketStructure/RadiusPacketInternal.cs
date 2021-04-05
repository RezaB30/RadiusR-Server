using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.PacketStructure
{
    public partial class RadiusPacket
    {
        private ulong GetDownloadedBytes()
        {
            var lowerValue = uint.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctOutputOctets).Value);
            var higherValue = uint.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctOutputGigawords).Value);
            return GetUInt64FromUInt32(higherValue, lowerValue);
        }

        private ulong GetUploadedBytes()
        {
            var lowerValue = uint.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctInputOctets).Value);
            var higherValue = uint.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctInputGigawords).Value);
            return GetUInt64FromUInt32(higherValue, lowerValue);
        }

        private ulong GetUInt64FromUInt32(uint high, uint low)
        {
            ulong result = high;
            result = result << 32;
            result = result | low;
            return result;
        }

        private bool IsValidRequest(string secret)
        {
            var toHashBytes = new List<byte>();
            toHashBytes.Add((byte)Code);
            toHashBytes.Add(Identifier);
            toHashBytes.AddRange(BitConverter.GetBytes(Length).Reverse().ToArray());
            var emptyBytes = new byte[16];
            for (int i = 0; i < emptyBytes.Length; i++)
            {
                emptyBytes[i] = 0;
            }
            toHashBytes.AddRange(emptyBytes);
            foreach (var attribute in Attributes)
            {
                toHashBytes.AddRange(attribute.GetBytes());
            }

            toHashBytes.AddRange(Encoding.UTF8.GetBytes(secret));
            var hashAlgorithm = MD5.Create();
            var hashedString = Encoding.UTF8.GetString(hashAlgorithm.ComputeHash(toHashBytes.ToArray()));
            var requestAuthenticatorString = Encoding.UTF8.GetString(RequestAuthenticator);
            return hashedString == requestAuthenticatorString;
        }
    }
}
