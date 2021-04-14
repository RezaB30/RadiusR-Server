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
        public ulong GetDownloadedBytes()
        {
            if (!Attributes.Any(attr => attr.Type == AttributeType.AcctOutputOctets))
                return 0;
            var lowerValue = uint.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctOutputOctets).Value);
            var higherValue = uint.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctOutputGigawords).Value);
            return GetUInt64FromUInt32(higherValue, lowerValue);
        }

        public ulong GetUploadedBytes()
        {
            if (!Attributes.Any(attr => attr.Type == AttributeType.AcctInputOctets))
                return 0;
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

        public bool IsValidRequest(string secret)
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

        protected byte[] GetUserPasswordBytes(string userPassword, string secret)
        {
            var rawPasswordBytes = Encoding.UTF8.GetBytes(userPassword);
            var paddingCount = 16 - (rawPasswordBytes.Length % 16);
            var paddingBytes = new byte[paddingCount];
            paddingBytes.AsParallel().ForAll(b => b = 0);
            rawPasswordBytes = rawPasswordBytes.Concat(paddingBytes).ToArray();
            var passwordChunks = rawPasswordBytes.Select((v, i) => new { Index = i, Value = (byte)v }).GroupBy(indexed => indexed.Index / 16).Select(g => g.Select(indexed => indexed.Value).ToArray()).ToArray();

            var hashingAlgorithm = MD5.Create();
            var key = hashingAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(secret).Concat(RequestAuthenticator).ToArray());
            var results = new List<byte>();
            for (int i = 0; i < passwordChunks.Length; i++)
            {
                var lastConvertedChunk = new List<byte>();
                for (int j = 0; j < passwordChunks[i].Length; j++)
                {
                    lastConvertedChunk.Add((byte)(passwordChunks[i][j] ^ key[j]));
                }
                results.AddRange(lastConvertedChunk);
                key = hashingAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(secret).Concat(lastConvertedChunk).ToArray());
            }

            return results.ToArray();
        }

        public bool HasValidPassword(string userPassword, string secret)
        {
            var passwordAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserPassword);
            if (passwordAttribute == null)
                return false;
            var encodedPassword = GetUserPasswordBytes(userPassword, secret);
            var passwordHash = Convert.ToBase64String(encodedPassword);
            return passwordHash == passwordAttribute.Value;
        }
    }
}
