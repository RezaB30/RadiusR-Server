using NLog;
using RadiusR.DB;
using RezaB.Radius.PacketStructure;
using RezaB.Radius.Server.Caching;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public class StateUpdates : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("state-updates");
        private static Logger dbLogger = LogManager.GetLogger("state-updates-DB");

        private short[] _enabledStates = new short[]
        {
            (short)RadiusR.DB.Enums.CustomerState.Active,
            (short)RadiusR.DB.Enums.CustomerState.Reserved
        };

        public StateUpdates(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

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
                            var searchQuery = db.RadiusAuthorizations.OrderBy(s => s.SubscriptionID).Where(s => (s.IsEnabled && !_enabledStates.Contains(s.Subscription.State)) || (!s.IsEnabled && _enabledStates.Contains(s.Subscription.State)));
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
                            // decide the type
                            if (_enabledStates.Contains(currentAuthRecord.Subscription.State))
                            {
                                // update auth record
                                try
                                {
                                    db.Database.ExecuteSqlCommand("UPDATE RadiusAuthorization SET IsEnabled = 1 WHERE SubscriptionID = @subId;", new[] { new SqlParameter("@subId", currentAuthRecord.SubscriptionID) });
                                }
                                catch (Exception ex)
                                {
                                    logger.Warn(ex, $"Could not update authorization record with subscription id [{currentAuthRecord.SubscriptionID}].");
                                }
                                // nas update
                                if (currentAuthRecord.LastInterimUpdate.HasValue && currentAuthRecord.LastInterimUpdate > (currentAuthRecord.LastLogout ?? DateTime.MinValue))
                                {
                                    try
                                    {
                                        DAClient.Send(new IPEndPoint(nas.NASIP, nas.IncomingPort), new DynamicAuthorizationExtentionPacket(MessageTypes.DisconnectRequest, new[] { new RadiusAttribute(AttributeType.UserName, currentAuthRecord.Username) }), nas.Secret);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Warn(ex, $"Could not disconnect [{currentAuthRecord.Username}] from [{currentAuthRecord.NASIP}].");
                                    }
                                }
                            }
                            else
                            {
                                // update auth record
                                try
                                {
                                    db.Database.ExecuteSqlCommand("UPDATE RadiusAuthorization SET IsEnabled = 0 WHERE SubscriptionID = @subId;", new[] { new SqlParameter("@subId", currentAuthRecord.SubscriptionID) });
                                }
                                catch (Exception ex)
                                {
                                    logger.Warn(ex, $"Could not update authorization record with subscription id [{currentAuthRecord.SubscriptionID}].");
                                }
                                // nas update
                                if (currentAuthRecord.LastInterimUpdate.HasValue && currentAuthRecord.LastInterimUpdate > (currentAuthRecord.LastLogout ?? DateTime.MinValue))
                                {
                                    try
                                    {
                                        DAClient.Send(new IPEndPoint(nas.NASIP, nas.IncomingPort), new DynamicAuthorizationExtentionPacket(MessageTypes.DisconnectRequest, new[] { new RadiusAttribute(AttributeType.UserName, currentAuthRecord.Username) }), nas.Secret);
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Warn(ex, $"Could not disconnect [{currentAuthRecord.Username}] from [{currentAuthRecord.NASIP}].");
                                    }
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
