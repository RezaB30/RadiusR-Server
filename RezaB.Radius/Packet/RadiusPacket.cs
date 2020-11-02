using NLog;
using RadiusR.DB;
using RadiusR.SMS;
using RezaB.Mikrotik;
using RezaB.Mikrotik.Extentions;
using RezaB.Mikrotik.Extentions.MultiTasking;
using RezaB.Radius.Caching;
using RezaB.Radius.Packet.AttributeEnums;
using RezaB.Radius.Server;
using RezaB.Radius.Vendors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Entity;
using RadiusR.DB.Enums;

namespace RezaB.Radius.Packet
{
    public partial class RadiusPacket
    {
        private static Logger logger = LogManager.GetLogger("console");
        //private static Logger tempLogger = LogManager.GetLogger("Temp");

        protected NasClientCredentials _nasClientCredentials;
        public MessageTypes Code { get; protected set; }

        public byte Identifier { get; protected set; }

        public string Signiture
        {
            get
            {
                var authenticator = RequestAuthenticator;
                if (Code == MessageTypes.AccessAccept || Code == MessageTypes.AccessReject)
                    authenticator = ResponseAuthenticator;
                if (authenticator == null)
                    return "Not Set";
                StringBuilder result = new StringBuilder("0x");
                foreach (var IdByte in authenticator)
                {
                    result.Append(IdByte.ToString("x2"));
                }
                return result.ToString();
            }
        }

        public ushort Length { get; protected set; }

        public byte[] RequestAuthenticator { get; protected set; }

        public byte[] ResponseAuthenticator { get; protected set; }

        public List<RadiusAttribute> Attributes { get; protected set; }

        public string UniqueSessionId
        {
            get
            {
                if (_uniqueSessionId == null)
                {
                    var usernameAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName);
                    var acctSessionIdAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionId);
                    var NASIPAddressAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASIPAddress);
                    var NASPortAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPort);
                    if (usernameAttribute == null || acctSessionIdAttribute == null || NASIPAddressAttribute == null || NASPortAttribute == null)
                    {
                        _uniqueSessionId = "";
                        return _uniqueSessionId;
                    }

                    var key = usernameAttribute.Value
                        + acctSessionIdAttribute.Value
                        + NASIPAddressAttribute.Value
                        + NASPortAttribute.Value;
                    var hashAlgorithm = MD5.Create();
                    var hashedBytes = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(key));

                    var results = new StringBuilder();
                    for (int i = 0; i < hashedBytes.Length; i++)
                    {
                        results.Append(hashedBytes[i].ToString("x2"));
                    }

                    _uniqueSessionId = results.ToString();
                }

                return _uniqueSessionId;
            }
        }

        private string _uniqueSessionId = null;

        public long UploadedBytes
        {
            get
            {
                if (!_uploadedBytes.HasValue)
                {
                    _uploadedBytes = (long)GetUploadedBytes();
                }
                return _uploadedBytes.Value;
            }
        }

        private long? _uploadedBytes;

        public long DownloadedBytes
        {
            get
            {
                if (!_downloadedBytes.HasValue)
                {
                    _downloadedBytes = (long)GetDownloadedBytes();
                }
                return _downloadedBytes.Value;
            }
        }

        private long? _downloadedBytes;

        public RadiusPacket(byte[] data, NasClientCredentials clientCredentials)
        {
            _nasClientCredentials = clientCredentials;
            var index = 0;
            Code = (MessageTypes)data[index];
            index++;
            Identifier = data[index];
            index++;
            Length = BitConverter.ToUInt16(data.Skip(index).Take(2).Reverse().ToArray(), 0);
            index += 2;
            RequestAuthenticator = data.Skip(index).Take(16).ToArray();
            index += 16;
            // read attributes
            Attributes = new List<RadiusAttribute>();
            while (index < Length)
            {
                Attributes.Add(RadiusAttribute.Read(data, ref index));
            }
        }

        protected RadiusPacket() { }

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

        protected bool HasValidPassword(Subscription dbSubscription, string secret)
        {
            var passwordAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserPassword);
            if (passwordAttribute == null)
                return false;
            var encodedPassword = GetUserPasswordBytes(dbSubscription.RadiusPassword, secret);
            var passwordHash = Convert.ToBase64String(encodedPassword);
            return passwordHash == passwordAttribute.Value;
        }

        public RadiusPacket GetResponse(PacketProcessingOptions options)
        {
            var responsePacket = new RadiusPacket()
            {
                Identifier = Identifier,
                RequestAuthenticator = RequestAuthenticator,
                Attributes = new List<RadiusAttribute>()
            };

            if (Code == MessageTypes.AccessRequest)
            {
                GetAuthenticationResponse(responsePacket, options);
                return responsePacket;
            }
            if (Code == MessageTypes.AccountingRequest)
            {
                GetAccountingResponse(ref responsePacket, options);
                return responsePacket;
            }

            return null;
        }

        public byte[] GetBytes(string secret)
        {
            var data = new List<byte>();
            // adding message code
            data.Add((byte)Code);
            // adding identifier
            data.Add(Identifier);
            // leave space for length and response authenticator values
            data.AddRange(new byte[2]);
            // add request authenticator for computing hash
            data.AddRange(RequestAuthenticator);
            // adding attributes
            foreach (var attribute in Attributes)
            {
                data.AddRange(attribute.GetBytes());
            }
            // set length
            var lengthInBytes = BitConverter.GetBytes((ushort)data.Count()).Reverse().ToArray();
            data[2] = lengthInBytes[0];
            data[3] = lengthInBytes[1];
            // calculate and set response authenticator
            var hashAlgorithm = MD5.Create();
            ResponseAuthenticator = hashAlgorithm.ComputeHash(data.Concat(Encoding.UTF8.GetBytes(secret)).ToArray());
            // set the value
            for (int i = 0; i < 16; i++)
            {
                data[i + 4] = ResponseAuthenticator[i];
            }
            // return array
            return data.ToArray();
        }

        public StringBuilder GetLog()
        {
            StringBuilder logText = new StringBuilder();
            logText.AppendLine(Code.ToString() + " Packet");
            logText.AppendLine("===============================================================");
            logText.AppendLine("Packet ID: " + Identifier);
            logText.AppendLine("Signiture: " + Signiture);
            logText.AppendLine("Packet data:");
            foreach (var attribute in Attributes)
            {
                logText.AppendLine(((attribute.Type == AttributeType.VendorSpecific) ? ((VendorAttribute)attribute).VendorTypeName + " (VendorSpecific)" : attribute.Type.ToString()) + ": " + ((string.IsNullOrEmpty(attribute.EnumValue)) ? attribute.Value : attribute.EnumValue));
            }
            logText.AppendLine("===============================================================");

            logger.Trace(logText);

            return logText;
        }

        public ChangeOfAccountingRequest GetClientRequest()
        {
            var usernameAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName);
            var AccountingStatusTypeAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctStatusType);
            var rateLimitAttribute = Attributes.FirstOrDefault(attr => attr is MikrotikAttribute && ((MikrotikAttribute)attr).VendorType == MikrotikAttribute.Attributes.MikrotikRateLimit);
            var currentRateLimit = rateLimitAttribute != null ? rateLimitAttribute.Value : null;
            var framedIPAddressAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedIPAddress);
            var isInExpiredPool = framedIPAddressAttribute == null || _nasClientCredentials.IsInExpiredPool(framedIPAddressAttribute.Value);
            if (usernameAttribute == null || AccountingStatusTypeAttribute == null /*|| currentRateLimit == null*/)
                return null;

            var username = usernameAttribute.Value;
            var accountingStatusType = (AcctStatusType)short.Parse(AccountingStatusTypeAttribute.Value);
            // if is acc stop packet return
            if (accountingStatusType != AcctStatusType.Start && accountingStatusType != AcctStatusType.InterimUpdate)
                return null;

            using (RadiusREntities db = new RadiusREntities())
            {
                var dbSubscription = FindSubscriber(username, db);
                // check subscription state
                var stateChange = CheckSubscriptionChange(dbSubscription);
                // check for expired (when bills payed should disconnect to change pool out of expired if has expired pool)
                if (!stateChange.ExpiredPoolValid && isInExpiredPool)
                {
                    return new ChangeOfAccountingRequest()
                    {
                        Packet = new RadiusDisconnectPacket(this, null, _nasClientCredentials)
                    };
                }
                // create response
                switch (stateChange.State)
                {
                    case SubscriptionStateChange.SubscriptionState.Expired:
                        // skip if in expired pool
                        if (isInExpiredPool)
                            return null;
                        // disconnect if not in expired pool
                        return new ChangeOfAccountingRequest()
                        {
                            Packet = new RadiusDisconnectPacket(this, null, _nasClientCredentials),
                            SMSType = SMSType.DebtDisconnection,
                            SubscriptionState = stateChange
                        };
                    case SubscriptionStateChange.SubscriptionState.Inactive:
                        return new ChangeOfAccountingRequest()
                        {
                            Packet = new RadiusDisconnectPacket(this, null, _nasClientCredentials)
                        };
                    case SubscriptionStateChange.SubscriptionState.Used80PercentQuota:
                        if (!dbSubscription.RadiusSMS.Any(sms => sms.SMSTypeID == (short)SMSType.Quota80 && sms.Date > stateChange.LastQuotaDate))
                        {
                            return new ChangeOfAccountingRequest()
                            {
                                SMSType = SMSType.Quota80,
                                SubscriptionState = stateChange
                            };
                        }
                        return null;
                    case SubscriptionStateChange.SubscriptionState.Active:
                        if (currentRateLimit != null && currentRateLimit != stateChange.RateLimit)
                        {
                            return new ChangeOfAccountingRequest()
                            {
                                Packet = new RadiusCOAPacket(this, new[] { new MikrotikAttribute(MikrotikAttribute.Attributes.MikrotikRateLimit, stateChange.RateLimit) }, _nasClientCredentials),
                            };
                        }
                        return null;
                    case SubscriptionStateChange.SubscriptionState.HardQuotaExpired:
                        // skip if in expired pool
                        if (isInExpiredPool)
                            return null;
                        return new ChangeOfAccountingRequest()
                        {
                            Packet = new RadiusDisconnectPacket(this, null, _nasClientCredentials),
                            SMSType = SMSType.HardQuota100,
                            SubscriptionState = stateChange
                        };
                    case SubscriptionStateChange.SubscriptionState.SoftQuotaExpired:
                        if (currentRateLimit != null && currentRateLimit != stateChange.RateLimit)
                        {
                            if (!dbSubscription.RadiusSMS.Any(sms => sms.SMSTypeID == (short)SMSType.SoftQuota100 && sms.Date > stateChange.LastQuotaDate))
                            {
                                return new ChangeOfAccountingRequest()
                                {
                                    Packet = new RadiusCOAPacket(this, new[] { new MikrotikAttribute(MikrotikAttribute.Attributes.MikrotikRateLimit, stateChange.RateLimit) }, _nasClientCredentials),
                                    SMSType = SMSType.SoftQuota100,
                                    SubscriptionState = stateChange
                                };
                            }
                            else
                            {
                                return new ChangeOfAccountingRequest()
                                {
                                    Packet = new RadiusCOAPacket(this, new[] { new MikrotikAttribute(MikrotikAttribute.Attributes.MikrotikRateLimit, stateChange.RateLimit) }, _nasClientCredentials),
                                    SubscriptionState = stateChange
                                };
                            }
                        }
                        return null;
                    case SubscriptionStateChange.SubscriptionState.SmartQuotaMaxPriceReached:
                        if (!dbSubscription.RadiusSMS.Any(sms => sms.SMSTypeID == (short)SMSType.SmartQuotaMax && sms.Date > stateChange.LastQuotaDate))
                        {
                            return new ChangeOfAccountingRequest()
                            {
                                SMSType = SMSType.SmartQuotaMax,
                                SubscriptionState = stateChange
                            };
                        }
                        return null;
                    case SubscriptionStateChange.SubscriptionState.SmartQuotaExpired:
                        if (!dbSubscription.RadiusSMS.Any(sms => sms.SMSTypeID == (short)SMSType.SmartQuota100 && sms.Date > stateChange.LastQuotaDate))
                        {
                            return new ChangeOfAccountingRequest()
                            {
                                SMSType = SMSType.SmartQuota100,
                                SubscriptionState = stateChange
                            };
                        }
                        return null;
                    default:
                        break;
                }
            }
            
            return null;
        }
    }
}
