using NLog;
using RadiusR.DB;
using RezaB.Radius.Server.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Net;
using RadiusR.DB.Enums;
using System.Data.SqlClient;
using RezaB.Radius.PacketStructure;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public class RateLimitUpdates : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("rate-limit-updates");
        private static Logger dbLogger = LogManager.GetLogger("rate-limit-updates-DB");

        public RateLimitUpdates(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

        public override bool Run()
        {
            logger.Trace("Task started.");
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
                        using (RadiusREntities db = new RadiusREntities())
                        {
                            // prepare query
                            db.Database.Log = dbLogger.Trace;
                            var searchQuery = db.RadiusAuthorizations.Include(s => s.Subscription.Service.ServiceRateTimeTables).OrderBy(s => s.SubscriptionID).Where(s => s.IsEnabled && s.Subscription.Service.QuotaType != (short)QuotaType.SoftQuota);
                            // fetch record
                            var currentAuthRecord = searchQuery.Where(s => s.SubscriptionID > currentId).FirstOrDefault();
                            if (currentAuthRecord == null)
                            {
                                logger.Trace("No more records found. returning");
                                return true;
                            }
                            currentId = currentAuthRecord.SubscriptionID;
                            // server from cache
                            CachedNAS nas = null;
                            if (!string.IsNullOrEmpty(currentAuthRecord.NASIP))
                            {
                                IPAddress currentNASIP;
                                if (IPAddress.TryParse(currentAuthRecord.NASIP, out currentNASIP))
                                {
                                    nas = DAServers.GetCachedNAS(currentNASIP);
                                }
                            }
                            // No quoue tariffs
                            if (currentAuthRecord.Subscription.Service.NoQueue)
                            {
                                if (!string.IsNullOrWhiteSpace(currentAuthRecord.RateLimit))
                                {
                                    // nas update
                                    if (currentAuthRecord.LastInterimUpdate.HasValue && currentAuthRecord.LastInterimUpdate > (currentAuthRecord.LastLogout ?? DateTime.MinValue) && !currentAuthRecord.UsingExpiredPool)
                                    {
                                        try
                                        {
                                            DAClient.Send(new IPEndPoint(nas.NASIP, nas.IncomingPort), new DynamicAuthorizationExtentionPacket(MessageTypes.CoARequest, new[] { new RadiusAttribute(AttributeType.UserName, currentAuthRecord.Username), new PacketStructure.Vendors.MikrotikAttribute(PacketStructure.Vendors.MikrotikAttribute.Attributes.MikrotikRateLimit, "") }), nas.Secret);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Warn(ex, $"Could not send COA for [{currentAuthRecord.Username}] from [{currentAuthRecord.NASIP}].");
                                            continue;
                                        }
                                    }
                                    // update auth record
                                    try
                                    {
                                        db.Database.ExecuteSqlCommand("UPDATE RadiusAuthorization SET RateLimit = NULL WHERE SubscriptionID = @subId", new[] { new SqlParameter("@subId", currentAuthRecord.SubscriptionID) });
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Warn(ex, $"Could not update authorization record with subscription id [{currentAuthRecord.SubscriptionID}].");
                                        continue;
                                    }
                                }
                            }
                            // check rate limit
                            else if (currentAuthRecord.RateLimit != currentAuthRecord.Subscription.Service.CurrentRateLimit)
                            {
                                // nas update
                                if (currentAuthRecord.LastInterimUpdate.HasValue && currentAuthRecord.LastInterimUpdate > (currentAuthRecord.LastLogout ?? DateTime.MinValue) && !currentAuthRecord.UsingExpiredPool)
                                {
                                    try
                                    {
                                        DAClient.Send(new IPEndPoint(nas.NASIP, nas.IncomingPort), new DynamicAuthorizationExtentionPacket(MessageTypes.CoARequest, new[] { new RadiusAttribute(AttributeType.UserName, currentAuthRecord.Username), new PacketStructure.Vendors.MikrotikAttribute(PacketStructure.Vendors.MikrotikAttribute.Attributes.MikrotikRateLimit, currentAuthRecord.Subscription.Service.CurrentRateLimit) }), nas.Secret);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Warn(ex, $"Could not send COA for [{currentAuthRecord.Username}] from [{currentAuthRecord.NASIP}].");
                                        continue;
                                    }
                                }
                                // update auth record
                                try
                                {
                                    db.Database.ExecuteSqlCommand("UPDATE RadiusAuthorization SET RateLimit = @rateLimit WHERE SubscriptionID = @subId", new[] { new SqlParameter("@subId", currentAuthRecord.SubscriptionID), new SqlParameter("@rateLimit", currentAuthRecord.Subscription.Service.CurrentRateLimit) });
                                }
                                catch (Exception ex)
                                {
                                    logger.Warn(ex, $"Could not update authorization record with subscription id [{currentAuthRecord.SubscriptionID}].");
                                    continue;
                                }
                            }
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
