using RadiusR.DB;
using RadiusR.DB.Enums;
using RadiusR.DB.ModelExtentions;
using RezaB.Radius.Packet.AttributeEnums;
using RezaB.Radius.Server;
using RezaB.Radius.Vendors;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Transactions;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Data.Common;
using NLog;

namespace RezaB.Radius.Packet
{
    public partial class RadiusPacket
    {
        private static Logger dbLogger = LogManager.GetLogger("db");

        private Server.Caching.ServerDefaultsCache.ServerDefaults _serverCache = null;
        private Server.Caching.ServerDefaultsCache.ServerDefaults ServerCache
        {
            get
            {
                if (_serverCache != null)
                    return _serverCache;
                _serverCache = RadiusServer.ServerCache.ServerSettings.GetSettings();
                return _serverCache;
            }
        }

        private void AccessReject()
        {
            Code = MessageTypes.AccessReject;
        }

        private RadiusAccounting CreateAccountingRecord(Subscription dbSubscription, RadiusAttribute framedIPAddressAttribute)
        {
            var clientIPInfo = _nasClientCredentials.GetClientIPInfo(framedIPAddressAttribute.Value);

            return new RadiusAccounting()
            {
                SubscriptionID = dbSubscription.ID,
                CalledStationID = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CalledStationId) != null ? Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CalledStationId).Value : null,
                CallingStationID = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CallingStationId) != null ? Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CallingStationId).Value : null,
                DownloadBytes = DownloadedBytes,
                UploadBytes = UploadedBytes,
                FramedIPAddress = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedIPAddress).Value,
                FramedProtocol = short.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedProtocol).Value),
                NASIP = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASIPAddress).Value,
                NASPort = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPort) != null ? Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPort).Value : null,
                NASportType = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPortType) != null ? Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPortType).Value : null,
                ServiceType = short.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.ServiceType).Value),
                SessionID = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionId).Value,
                SessionTime = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime) != null ? long.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime).Value) : 0,
                StartTime = DateTime.Now,
                Username = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName).Value,
                UniqueID = UniqueSessionId,
                RadiusAccountingIPInfo = !string.IsNullOrEmpty(dbSubscription.StaticIP) ? new RadiusAccountingIPInfo()
                {
                    LocalIP = framedIPAddressAttribute.Value,
                    RealIP = framedIPAddressAttribute.Value,
                    PortRange = "0-65535"
                } : clientIPInfo != null ? new RadiusAccountingIPInfo()
                {
                    LocalIP = clientIPInfo.LocalIP,
                    RealIP = clientIPInfo.RealIP,
                    PortRange = clientIPInfo.PortRange
                } : null
            };
        }

        private void GetAuthenticationResponse(DbConnection connection, RadiusPacket responsePacket, PacketProcessingOptions ProcessingOptions)
        {
            using (RadiusREntities db = new RadiusREntities(connection))
            {
                db.Database.Log = dbLogger.Trace;
                db.Configuration.AutoDetectChangesEnabled = false;
                // check user password
                logger.Trace("Checking password.");
                var username = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName).Value;
                var dbSubscription = FindSubscriber(db, username);
                if (dbSubscription == null)
                {
                    responsePacket.AccessReject();
                    return;
                }
                if (!HasValidPassword(dbSubscription, _nasClientCredentials.Secret))
                {
                    responsePacket.AccessReject();
                    return;
                }

                // if reached simultaneous use cap
                logger.Trace("Checking simultaneous use.");
                if (HasMaxSimultaneousUse(connection, dbSubscription))
                {
                    responsePacket.AccessReject();
                    return;
                }

                // if the account is expired
                logger.Trace("Checking active.");
                var currentState = CheckSubscriptionChange(connection, dbSubscription);
                //if (currentState.State == SubscriptionStateChange.SubscriptionState.Inactive)
                //{
                //    responsePacket.AccessReject();
                //    return;
                //}

                //var expiredStates = new[]
                //{
                //    SubscriptionStateChange.SubscriptionState.Expired,
                //    SubscriptionStateChange.SubscriptionState.HardQuotaExpired,
                //    //SubscriptionStateChange.SubscriptionState.Inactive
                //};
                // expired pool
                if (currentState.ExpiredPoolValid)
                {
                    // set expired pool if set
                    if (_nasClientCredentials.ExpiredPools != null && _nasClientCredentials.ExpiredPools.Any())
                    {
                        var rnd = new Random();
                        var expiredPoolName = _nasClientCredentials.ExpiredPools.ToArray()[rnd.Next(_nasClientCredentials.ExpiredPools.Count())].PoolName;
                        responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedPool, expiredPoolName));
                    }
                    // reject if no expired pool is set
                    else
                    {
                        responsePacket.AccessReject();
                        return;
                    }
                }

                //if (expiredStates.Contains(currentState.State))
                //{
                //    responsePacket.AccessReject();
                //    return;
                //}

                // request is accepted here
                responsePacket.Code = MessageTypes.AccessAccept;
                // set rate limit if tariff no queue is false and is not expired
                if (!dbSubscription.Service.NoQueue && !currentState.ExpiredPoolValid)
                    responsePacket.Attributes.Add(new MikrotikAttribute(MikrotikAttribute.Attributes.MikrotikRateLimit, currentState.RateLimit));
                responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedProtocol, ServerCache.FramedProtocol));
                responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.AcctInterimInterval, ServerCache.AccountingInterimInterval));
                // set static IP if available and not expired
                if (!string.IsNullOrEmpty(dbSubscription.StaticIP) && !currentState.ExpiredPoolValid)
                    responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedIPAddress, dbSubscription.StaticIP));
                else
                    responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedIPAddress, "255.255.255.255"));
                return;
            }
        }

        private void GetAccountingResponse(DbConnection connection, ref RadiusPacket responsePacket, PacketProcessingOptions ProcessingOptions)
        {
            responsePacket.Code = MessageTypes.AccountingResponse;
            var AccountingStatusTypeAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctStatusType);
            var usernameAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName);
            var framedIPAddressAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedIPAddress);
            if (string.IsNullOrEmpty(UniqueSessionId) || AccountingStatusTypeAttribute == null || usernameAttribute == null)
            {
                responsePacket = null;
                return;
            }
            if (!IsValidRequest())
            {
                responsePacket = null;
                return;
            }
            using (var db = new RadiusREntities(connection))
            {
                db.Database.Log = dbLogger.Trace;
                var accountingStatusType = (AcctStatusType)short.Parse(AccountingStatusTypeAttribute.Value);
                var username = usernameAttribute.Value;
                var dbSubscription = db.Subscriptions.FirstOrDefault(s => s.Username == username);
                if (dbSubscription == null)
                {
                    responsePacket = null;
                    return;
                }
                // process accounting message
                switch (accountingStatusType)
                {
                    case AcctStatusType.Start:
                        {
                            try
                            {
                                // try to find the accounting record first
                                var foundAccountingRecord = FindAccountingRecord(db, dbSubscription.ID);
                                if (foundAccountingRecord != null)
                                {
                                    var foundRecord = new RadiusAccounting()
                                    {
                                        ID = foundAccountingRecord.ID,
                                        StopTime = null,
                                        UpdateTime = DateTime.Now,
                                        TerminateCause = null
                                    };
                                    db.RadiusAccountings.Attach(foundRecord);
                                    db.Entry(foundRecord).Property(fr => fr.StopTime).IsModified = true;
                                    db.Entry(foundRecord).Property(fr => fr.UpdateTime).IsModified = true;
                                    db.Entry(foundRecord).Property(fr => fr.TerminateCause).IsModified = true;
                                    db.Configuration.ValidateOnSaveEnabled = false;
                                }
                                // if not found add a new one
                                else
                                {
                                    var newAccountingRecord = CreateAccountingRecord(dbSubscription, framedIPAddressAttribute);
                                    db.RadiusAccountings.Add(newAccountingRecord);
                                }
                                // save
                                db.SaveChanges();
                            }
                            catch
                            {
                                responsePacket = null;
                                return;
                            }
                            return;
                        }
                    case AcctStatusType.Stop:
                        {
                            // check if relevant record exists in database
                            var interrimAccountingRecord = FindAccountingRecord(db, dbSubscription.ID);
                            if (interrimAccountingRecord == null)
                            {
                                responsePacket = null;
                                return;
                            }
                            // set prequisites
                            var today = DateTime.Now.Date;
                            var uploadedBytesChange = UploadedBytes - interrimAccountingRecord.UploadBytes;
                            var downloadedBytesChange = DownloadedBytes - interrimAccountingRecord.DownloadBytes;
                            // set daily accounting record
                            {
                                var _newRecord = false;
                                var dailyAccountingRecord = db.RadiusDailyAccountings.Where(dra => dra.Date == today && dra.SubscriptionID == dbSubscription.ID).FirstOrDefault();
                                if (dailyAccountingRecord == null)
                                {
                                    dailyAccountingRecord = new RadiusDailyAccounting()
                                    {
                                        SubscriptionID = dbSubscription.ID,
                                        Date = DateTime.Now,
                                        DownloadBytes = 0,
                                        UploadBytes = 0,
                                        Username = dbSubscription.Username
                                    };
                                    _newRecord = true;
                                }
                                dailyAccountingRecord.DownloadBytes += downloadedBytesChange;
                                dailyAccountingRecord.UploadBytes += uploadedBytesChange;
                                if (_newRecord)
                                    db.RadiusDailyAccountings.Add(dailyAccountingRecord);
                            }
                            // set accounting record
                            interrimAccountingRecord.DownloadBytes = DownloadedBytes;
                            interrimAccountingRecord.UploadBytes = UploadedBytes;
                            interrimAccountingRecord.StopTime = DateTime.Now;
                            interrimAccountingRecord.SessionTime = long.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime).Value);
                            var terminateCauseAttribute = Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctTerminateCause);
                            if (terminateCauseAttribute == null)
                            {
                                responsePacket = null;
                                return;
                            }

                            short parsed;
                            if (short.TryParse(terminateCauseAttribute.Value, out parsed))
                            {
                                interrimAccountingRecord.TerminateCause = parsed;
                            }
                            // update
                            var foundRecord = new RadiusAccounting()
                            {
                                ID = interrimAccountingRecord.ID,
                                DownloadBytes = interrimAccountingRecord.DownloadBytes,
                                UploadBytes = interrimAccountingRecord.UploadBytes,
                                StopTime = interrimAccountingRecord.StopTime,
                                SessionTime = interrimAccountingRecord.SessionTime,
                                TerminateCause = interrimAccountingRecord.TerminateCause
                            };
                            db.RadiusAccountings.Attach(foundRecord);
                            db.Entry(foundRecord).Property(fr => fr.DownloadBytes).IsModified = true;
                            db.Entry(foundRecord).Property(fr => fr.UploadBytes).IsModified = true;
                            db.Entry(foundRecord).Property(fr => fr.StopTime).IsModified = true;
                            db.Entry(foundRecord).Property(fr => fr.SessionTime).IsModified = true;
                            db.Entry(foundRecord).Property(fr => fr.TerminateCause).IsModified = true;
                            db.Configuration.ValidateOnSaveEnabled = false;
                            // save to database
                            db.SaveChanges();

                            return;
                        }
                    case AcctStatusType.InterimUpdate:
                        {
                            // check if relevant record exists in database
                            var accountingRecord = FindAccountingRecord(db, dbSubscription.ID);
                            RadiusAccounting foundRecord = null;
                            // add new record if not found
                            if (accountingRecord == null)
                            {
                                foundRecord = CreateAccountingRecord(dbSubscription, framedIPAddressAttribute);
                                db.RadiusAccountings.Add(foundRecord);

                                accountingRecord = new InterrimAccountingRecord()
                                {
                                    UploadBytes = foundRecord.UploadBytes,
                                    DownloadBytes = foundRecord.DownloadBytes
                                };
                            }
                            // update existing accounting record
                            else
                            {
                                foundRecord = new RadiusAccounting()
                                {
                                    ID = accountingRecord.ID,
                                    UploadBytes = UploadedBytes,
                                    DownloadBytes = DownloadedBytes,
                                    SessionTime = long.Parse(Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime).Value),
                                    UpdateTime = DateTime.Now,
                                    StopTime = null,
                                    TerminateCause = null
                                };
                                db.RadiusAccountings.Attach(foundRecord);
                                db.Entry(foundRecord).Property(fr => fr.DownloadBytes).IsModified = true;
                                db.Entry(foundRecord).Property(fr => fr.UploadBytes).IsModified = true;
                                db.Entry(foundRecord).Property(fr => fr.StopTime).IsModified = true;
                                db.Entry(foundRecord).Property(fr => fr.UpdateTime).IsModified = true;
                                db.Entry(foundRecord).Property(fr => fr.SessionTime).IsModified = true;
                                db.Entry(foundRecord).Property(fr => fr.TerminateCause).IsModified = true;
                                db.Configuration.ValidateOnSaveEnabled = false;
                            }
                            // set prequisites
                            var today = DateTime.Now.Date;
                            var uploadedBytesChange = UploadedBytes - accountingRecord.UploadBytes;
                            var downloadedBytesChange = DownloadedBytes - accountingRecord.DownloadBytes;
                            // set daily accounting record
                            {
                                var _newRecord = false;
                                var dailyAccountingRecord = db.RadiusDailyAccountings.Where(dra => dra.Date == today && dra.SubscriptionID == dbSubscription.ID).FirstOrDefault();
                                if (dailyAccountingRecord == null)
                                {
                                    dailyAccountingRecord = new RadiusDailyAccounting()
                                    {
                                        SubscriptionID = dbSubscription.ID,
                                        Date = DateTime.Now,
                                        DownloadBytes = 0,
                                        UploadBytes = 0,
                                        Username = dbSubscription.Username
                                    };
                                    _newRecord = true;
                                }
                                dailyAccountingRecord.DownloadBytes += downloadedBytesChange;
                                dailyAccountingRecord.UploadBytes += uploadedBytesChange;
                                if (_newRecord)
                                    db.RadiusDailyAccountings.Add(dailyAccountingRecord);
                            }
                            // save to database
                            db.SaveChanges();
                            return;
                        }
                    default:
                        responsePacket = null;
                        return;
                }
            }
        }

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

        private bool IsValidRequest()
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

            toHashBytes.AddRange(Encoding.UTF8.GetBytes(_nasClientCredentials.Secret));
            var hashAlgorithm = MD5.Create();
            var hashedString = Encoding.UTF8.GetString(hashAlgorithm.ComputeHash(toHashBytes.ToArray()));
            var requestAuthenticatorString = Encoding.UTF8.GetString(RequestAuthenticator);
            return hashedString == requestAuthenticatorString;
        }

        private SubscriptionStateChange CheckSubscriptionChange(DbConnection connection, Subscription dbSubscription)
        {
            using (RadiusREntities db = new RadiusREntities(connection))
            {
                db.Database.Log = dbLogger.Trace;
                db.Configuration.AutoDetectChangesEnabled = false;
                // check if is active
                if (!dbSubscription.IsActive)
                    return new SubscriptionStateChange()
                    {
                        State = SubscriptionStateChange.SubscriptionState.Inactive,
                        SubscriberID = dbSubscription.ID
                    };
                // check if has debt (expired)
                if (!dbSubscription.LastAllowedDate.HasValue || dbSubscription.LastAllowedDate.Value.Add(ServerCache.DailyDisconnectionTime) < DateTime.Now)
                    return new SubscriptionStateChange()
                    {
                        State = SubscriptionStateChange.SubscriptionState.Expired,
                        SubscriberID = dbSubscription.ID
                    };
                // get base rate limit without quota consideration
                var rateLimit = dbSubscription.Service.CurrentRateLimit;

                // -------------- SHOULD DELETE ----------------------
                return new SubscriptionStateChange()
                {
                    State = SubscriptionStateChange.SubscriptionState.Active,
                    RateLimit = rateLimit,
                    SubscriberID = dbSubscription.ID
                };
                // ---------------------------------------------------
                // check for non-quota
                if (!dbSubscription.Service.QuotaType.HasValue)
                    return new SubscriptionStateChange()
                    {
                        State = SubscriptionStateChange.SubscriptionState.Active,
                        RateLimit = rateLimit,
                        SubscriberID = dbSubscription.ID
                    };
                var usageInfo = dbSubscription.GetQuotaAndUsageInfo();
                // add base quota
                var quota = usageInfo.PeriodQuota;
                var usage = usageInfo.PeriodUsage;
                var lastQuotaDate = usageInfo.LastQuotaChangeDate;
                var lastQuotaAmount = usageInfo.LastQuotaAmount;
                // if has no quota
                if (quota < usage)
                {
                    var quotaType = (QuotaType)dbSubscription.Service.QuotaType.Value;
                    if (quotaType == QuotaType.HardQuota)
                    {
                        return new SubscriptionStateChange()
                        {
                            State = SubscriptionStateChange.SubscriptionState.HardQuotaExpired,
                            SubscriberID = dbSubscription.ID,
                            LastQuotaDate = lastQuotaDate,
                            LastQuotaAmount = lastQuotaAmount
                        };
                    }
                    if (quotaType == QuotaType.SoftQuota)
                    {
                        return new SubscriptionStateChange()
                        {
                            State = SubscriptionStateChange.SubscriptionState.SoftQuotaExpired,
                            RateLimit = dbSubscription.Service.SoftQuotaRateLimit,
                            SubscriberID = dbSubscription.ID,
                            LastQuotaDate = lastQuotaDate,
                            LastQuotaAmount = lastQuotaAmount
                        };
                    }
                    if (quotaType == QuotaType.SmartQuota)
                    {
                        if (dbSubscription.Service.Price + ((usage - quota) * ServerCache.SmartQuotaPricePerByte) > dbSubscription.Service.SmartQuotaMaxPrice)
                        {
                            return new SubscriptionStateChange()
                            {
                                State = SubscriptionStateChange.SubscriptionState.SmartQuotaMaxPriceReached,
                                RateLimit = rateLimit,
                                SubscriberID = dbSubscription.ID,
                                LastQuotaDate = lastQuotaDate,
                                LastQuotaAmount = lastQuotaAmount
                            };
                        }
                        else
                        {
                            return new SubscriptionStateChange()
                            {
                                State = SubscriptionStateChange.SubscriptionState.SmartQuotaExpired,
                                RateLimit = rateLimit,
                                SubscriberID = dbSubscription.ID,
                                LastQuotaDate = lastQuotaDate,
                                LastQuotaAmount = lastQuotaAmount
                            };
                        }
                    }
                }
                // if used more than %80
                if (usage > Math.Floor(quota * 0.8m))
                {
                    return new SubscriptionStateChange()
                    {
                        State = SubscriptionStateChange.SubscriptionState.Used80PercentQuota,
                        RateLimit = rateLimit,
                        SubscriberID = dbSubscription.ID,
                        LastQuotaDate = lastQuotaDate,
                        LastQuotaAmount = lastQuotaAmount
                    };
                }
                // then it is below %80
                return new SubscriptionStateChange()
                {
                    State = SubscriptionStateChange.SubscriptionState.Active,
                    RateLimit = rateLimit,
                    SubscriberID = dbSubscription.ID,
                    LastQuotaDate = lastQuotaDate,
                    LastQuotaAmount = lastQuotaAmount
                };
            }
        }

        protected Subscription FindSubscriber(RadiusREntities db, string username)
        {
            if (db != null)
            {
                return db.Subscriptions.Where(s => s.Username == username).Include(s => s.Service.ServiceRateTimeTables).Include(s => s.RadiusSMS).FirstOrDefault();
            }
            else
            {
                using (db = new RadiusREntities())
                {
                    db.Database.Log = dbLogger.Trace;
                    db.Configuration.AutoDetectChangesEnabled = false;
                    return db.Subscriptions.Where(s => s.Username == username).Include(s => s.Service.ServiceRateTimeTables).Include(s => s.RadiusSMS).FirstOrDefault();
                }
            }
        }

        private InterrimAccountingRecord FindAccountingRecord(RadiusREntities db, long subscriptionId)
        {
            var result = db.RadiusAccountings.Where(ra => ra.SubscriptionID == subscriptionId).OrderByDescending(ra => ra.StartTime).Select(ra => new InterrimAccountingRecord()
            {
                ID = ra.ID,
                DownloadBytes = ra.DownloadBytes,
                UploadBytes = ra.UploadBytes,
                UpdateTime = ra.UpdateTime,
                StopTime = ra.StopTime,
                SessionTime = ra.SessionTime,
                UniqueID = ra.UniqueID
            }).FirstOrDefault();
            return (result != null && result.UniqueID == UniqueSessionId) ? result : null;
        }

        protected bool HasMaxSimultaneousUse(DbConnection connection, Subscription dbSubscription)
        {
            int dbOnlineCount;
            using (RadiusREntities db = new RadiusREntities(connection))
            {
                db.Database.Log = dbLogger.Trace;
                dbOnlineCount = db.RadiusAccountings.Where(ra => ra.SubscriptionID == dbSubscription.ID && !ra.StopTime.HasValue).Count();
            }
            return dbOnlineCount >= dbSubscription.SimultaneousUse;
        }

        public class SubscriptionStateChange
        {
            public SubscriptionState State { get; set; }

            public string RateLimit { get; set; }

            public long? SubscriberID { get; set; }

            public DateTime? LastQuotaDate { get; set; }

            public long? LastQuotaAmount { get; set; }

            public bool ExpiredPoolValid
            {
                get
                {
                    return State == SubscriptionState.Expired || State == SubscriptionState.HardQuotaExpired || State == SubscriptionState.Inactive;
                }
            }

            public enum SubscriptionState
            {
                Expired = 1,
                Inactive = 2,
                Active = 3,
                HardQuotaExpired = 4,
                SoftQuotaExpired = 5,
                Used80PercentQuota = 6,
                SmartQuotaExpired = 7,
                SmartQuotaMaxPriceReached = 8
            }
        }
    }
}
