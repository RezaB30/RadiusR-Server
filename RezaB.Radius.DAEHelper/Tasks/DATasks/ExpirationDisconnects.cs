using NLog;
using RadiusR.DB;
using RadiusR.DB.ModelExtentions;
using RadiusR.SMS;
using RezaB.Radius.PacketStructure;
using RezaB.Radius.Server.Caching;
using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public class ExpirationDisconnects : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("expiration-disconnects");
        private static Logger dbLogger = LogManager.GetLogger("expiration-disconnects-DB");

        public ExpirationDisconnects(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

        public override bool Run()
        {
            logger.Trace("Task started.");
            using (RadiusREntities db = new RadiusREntities())
            {
                // prepare query
                db.Database.Log = dbLogger.Trace;
                var searchQuery = db.RadiusAuthorizations.OrderBy(s => s.SubscriptionID).Where(s => s.IsEnabled && s.ExpirationDate <= DateTime.Now && ((s.LastInterimUpdate.HasValue && !s.LastLogout.HasValue) || (s.LastInterimUpdate > s.LastLogout)));
                long currentId = 0;
                using (var DAClient = new DAE.DynamicAuthorizationClient(DACPort, 3000, DACAddress))
                {

                    while (true)
                    {
                        if (_isAborted)
                        {
                            logger.Trace("Aborted!");
                            return false;
                        }
                        try
                        {
                            // fetch record
                            var currentAuthRecord = searchQuery.Where(s => s.SubscriptionID > currentId).FirstOrDefault();
                            if (currentAuthRecord == null)
                            {
                                logger.Trace("No more records found. returning");
                                return true;
                            }
                            currentId = currentAuthRecord.SubscriptionID;

                            if (!string.IsNullOrEmpty(currentAuthRecord.NASIP))
                            {
                                IPAddress currentNASIP;
                                CachedNAS nas = null;
                                if (IPAddress.TryParse(currentAuthRecord.NASIP, out currentNASIP))
                                {
                                    nas = DAServers.GetCachedNAS(currentNASIP);
                                    if (nas != null)
                                    {
                                        try
                                        {
                                            DAClient.Send(new IPEndPoint(nas.NASIP, nas.IncomingPort), new DynamicAuthorizationExtentionPacket(MessageTypes.DisconnectRequest, new[] { new RadiusAttribute(AttributeType.UserName, currentAuthRecord.Username) }), nas.Secret);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Warn(ex, $"Could not disconnect [{currentAuthRecord.Username}] from [{currentAuthRecord.NASIP}].");
                                            continue;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                try
                                {
                                    db.Database.ExecuteSqlCommand("UPDATE RadiusAuthorization SET LastLogout = @logoutTime WHERE SubscriptionID = @subId;", new[] { new SqlParameter("@subId", currentAuthRecord.SubscriptionID), new SqlParameter("@logoutTime", DateTime.Now) });
                                }
                                catch (Exception ex)
                                {
                                    logger.Warn(ex, $"Could not update authorization record with subscription id [{currentAuthRecord.SubscriptionID}].");
                                }
                            }
                            // send debt disconnect SMS
                            SMSService smsService = new SMSService();
                            var sentSMS = smsService.SendSubscriberSMS(currentAuthRecord.Subscription, RadiusR.DB.Enums.SMSType.DebtDisconnection);
                            using (RadiusREntities smsDb = new RadiusREntities())
                            {
                                smsDb.Database.Log = dbLogger.Trace;
                                smsDb.SMSArchives.AddSafely(sentSMS);
                                var currentRadiusSMS = smsDb.RadiusSMS.FirstOrDefault(rs => rs.SubscriptionID == currentAuthRecord.SubscriptionID && rs.SMSTypeID == (short)RadiusR.DB.Enums.SMSType.DebtDisconnection);
                                if (currentRadiusSMS != null)
                                {
                                    currentRadiusSMS.Date = DateTime.Now;
                                }
                                else
                                {
                                    smsDb.RadiusSMS.Add(new RadiusSMS()
                                    {
                                        Date = DateTime.Now,
                                        SMSTypeID = (short)RadiusR.DB.Enums.SMSType.DebtDisconnection,
                                        SubscriptionID = currentAuthRecord.SubscriptionID
                                    });
                                }
                                smsDb.SaveChanges();
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                            return false;
                        }
                    }
                }
            }
        }
    }
}
