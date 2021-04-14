using RadiusR.DB;
using RezaB.Radius.PacketStructure;
using RezaB.Radius.PacketStructure.AttributeEnums;
using RezaB.Radius.Server.Caching;
using RezaB.Radius.Server.Implementations.IntermediateContainers;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Implementations
{
    public class AccountingServer : RadiusServerBase
    {
        public AccountingServer() : base()
        {
            ThreadNamePrefix = "ACC";
        }

        protected override RadiusPacket CreateResponse(DbConnection connection, RadiusPacket packet, CachedNAS cachedNAS, CachedServerDefaults cachedServerDefaults)
        {
            // check packet validation
            var AccountingStatusTypeAttribute = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctStatusType);
            var usernameAttribute = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName);
            var framedIPAddressAttribute = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedIPAddress);
            if (string.IsNullOrEmpty(packet.UniqueSessionId) || AccountingStatusTypeAttribute == null || usernameAttribute == null)
            {
                return null;
            }
            if (!packet.IsValidRequest(cachedNAS.Secret))
            {
                return null;
            }
            short accountingStatusTypeValue;
            if (!short.TryParse(AccountingStatusTypeAttribute.Value, out accountingStatusTypeValue))
            {
                return null;
            }
            var accountingStatusType = (AcctStatusType)accountingStatusTypeValue;
            var username = usernameAttribute.Value;

            // packet is valid
            var responsePacket = new RadiusPacket(packet, MessageTypes.AccountingResponse);

            using (RadiusREntities db = new RadiusREntities(connection))
            {
                db.Database.Log = dbLogger.Trace;
                // find user
                var foundUser = db.RadiusAuthorizations.FirstOrDefault(user => user.Username == username);
                if (foundUser == null)
                {
                    return null;
                }

                switch (accountingStatusType)
                {
                    case AcctStatusType.Start:
                        {
                            // try to find the accounting record first
                            var foundAccountingRecord = FindAccountingRecord(db, foundUser.SubscriptionID, packet.UniqueSessionId);
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
                                var newAccountingRecord = CreateAccountingRecord(foundUser, packet, cachedNAS);
                                db.RadiusAccountings.Add(newAccountingRecord);
                            }
                            // save
                            db.SaveChanges();
                            // return response
                            return responsePacket;
                        }
                    case AcctStatusType.Stop:
                        {
                            // check if relevant record exists in database
                            var interrimAccountingRecord = FindAccountingRecord(db, foundUser.SubscriptionID, packet.UniqueSessionId);
                            if (interrimAccountingRecord == null)
                            {
                                return null;
                            }
                            // set prequisites
                            var today = DateTime.Now.Date;
                            var uploadedBytesChange = packet.UploadedBytes - interrimAccountingRecord.UploadBytes;
                            var downloadedBytesChange = packet.DownloadedBytes - interrimAccountingRecord.DownloadBytes;
                            // set daily accounting record
                            {
                                var _newRecord = false;
                                var dailyAccountingRecord = db.RadiusDailyAccountings.Where(dra => dra.Date == today && dra.SubscriptionID == foundUser.SubscriptionID).FirstOrDefault();
                                if (dailyAccountingRecord == null)
                                {
                                    dailyAccountingRecord = new RadiusDailyAccounting()
                                    {
                                        SubscriptionID = foundUser.SubscriptionID,
                                        Date = DateTime.Now,
                                        DownloadBytes = 0,
                                        UploadBytes = 0,
                                        Username = foundUser.Username
                                    };
                                    _newRecord = true;
                                }
                                dailyAccountingRecord.DownloadBytes += downloadedBytesChange;
                                dailyAccountingRecord.UploadBytes += uploadedBytesChange;
                                if (_newRecord)
                                    db.RadiusDailyAccountings.Add(dailyAccountingRecord);
                            }
                            // set accounting record
                            foundUser.LastLogout = DateTime.Now;
                            interrimAccountingRecord.DownloadBytes = packet.DownloadedBytes;
                            interrimAccountingRecord.UploadBytes = packet.UploadedBytes;
                            interrimAccountingRecord.StopTime = DateTime.Now;
                            interrimAccountingRecord.SessionTime = long.Parse(packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime).Value);
                            var terminateCauseAttribute = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctTerminateCause);
                            if (terminateCauseAttribute == null)
                            {
                                return null;
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
                            // return response
                            return responsePacket;
                        }
                    case AcctStatusType.InterimUpdate:
                        {
                            // check if relevant record exists in database
                            var accountingRecord = FindAccountingRecord(db, foundUser.SubscriptionID, packet.UniqueSessionId);
                            RadiusAccounting foundRecord = null;
                            // add new record if not found
                            if (accountingRecord == null)
                            {
                                foundRecord = CreateAccountingRecord(foundUser, packet, cachedNAS);
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
                                foundUser.LastInterimUpdate = DateTime.Now;

                                foundRecord = new RadiusAccounting()
                                {
                                    ID = accountingRecord.ID,
                                    UploadBytes = packet.UploadedBytes,
                                    DownloadBytes = packet.DownloadedBytes,
                                    SessionTime = long.Parse(packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime).Value),
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
                            var uploadedBytesChange = packet.UploadedBytes - accountingRecord.UploadBytes;
                            var downloadedBytesChange = packet.DownloadedBytes - accountingRecord.DownloadBytes;
                            // set daily accounting record
                            {
                                var _newRecord = false;
                                var dailyAccountingRecord = db.RadiusDailyAccountings.Where(dra => dra.Date == today && dra.SubscriptionID == foundUser.SubscriptionID).FirstOrDefault();
                                if (dailyAccountingRecord == null)
                                {
                                    dailyAccountingRecord = new RadiusDailyAccounting()
                                    {
                                        SubscriptionID = foundUser.SubscriptionID,
                                        Date = DateTime.Now,
                                        DownloadBytes = 0,
                                        UploadBytes = 0,
                                        Username = foundUser.Username
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
                            // return response
                            return responsePacket;
                        }
                    case AcctStatusType.AccountingOff:
                    default:
                        return null;
                }
            }
        }

        private RadiusAccounting CreateAccountingRecord(RadiusAuthorization user, RadiusPacket packet, CachedNAS cachedNAS)
        {
            var packetUserIP = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedIPAddress).Value;
            var clientIPInfo = cachedNAS.GetClientIPInfo(packetUserIP);

            user.LastInterimUpdate = DateTime.Now;

            return new RadiusAccounting()
            {
                SubscriptionID = user.SubscriptionID,
                CalledStationID = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CalledStationId) != null ? packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CalledStationId).Value : null,
                CallingStationID = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CallingStationId) != null ? packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CallingStationId).Value : null,
                DownloadBytes = packet.DownloadedBytes,
                UploadBytes = packet.UploadedBytes,
                FramedIPAddress = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedIPAddress).Value,
                FramedProtocol = short.Parse(packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.FramedProtocol).Value),
                NASIP = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASIPAddress).Value,
                NASPort = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPort) != null ? packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPort).Value : null,
                NASportType = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPortType) != null ? packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.NASPortType).Value : null,
                ServiceType = short.Parse(packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.ServiceType).Value),
                SessionID = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionId).Value,
                SessionTime = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime) != null ? long.Parse(packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.AcctSessionTime).Value) : 0,
                StartTime = DateTime.Now,
                Username = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName).Value,
                UniqueID = packet.UniqueSessionId,
                RadiusAccountingIPInfo = !string.IsNullOrEmpty(user.StaticIP) ? new RadiusAccountingIPInfo()
                {
                    LocalIP = packetUserIP,
                    RealIP = packetUserIP,
                    PortRange = "0-65535"
                } : clientIPInfo != null ? new RadiusAccountingIPInfo()
                {
                    LocalIP = clientIPInfo.LocalIP,
                    RealIP = clientIPInfo.RealIP,
                    PortRange = clientIPInfo.PortRange
                } : null
            };
        }

        private InterrimAccountingRecord FindAccountingRecord(RadiusREntities db, long subscriptionId, string uniqueSessionId)
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
            return (result != null && result.UniqueID == uniqueSessionId) ? result : null;
        }
    }
}
