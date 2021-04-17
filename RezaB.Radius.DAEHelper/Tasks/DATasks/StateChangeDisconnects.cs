using NLog;
using RadiusR.DB;
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
    public class StateChangeDisconnects : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("state-change-disconnect");
        private static Logger dbLogger = LogManager.GetLogger("state-change-disconnect-DB");

        private short[] _disconnectStates = new short[]
        {
            (short)RadiusR.DB.Enums.CustomerState.Cancelled,
            (short)RadiusR.DB.Enums.CustomerState.Disabled
        };

        public StateChangeDisconnects(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

        public override bool Run()
        {
            logger.Trace("Task started.");
            using (RadiusREntities db = new RadiusREntities())
            {
                // prepare query
                db.Database.Log = dbLogger.Trace;
                var searchQuery = db.RadiusAuthorizations.OrderBy(s => s.SubscriptionID).Where(s => _disconnectStates.Contains(s.Subscription.State) && ((s.LastInterimUpdate.HasValue && !s.LastLogout.HasValue) || (s.LastInterimUpdate > s.LastLogout) || s.IsEnabled));
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

                            if (!string.IsNullOrEmpty(currentAuthRecord.NASIP) && (currentAuthRecord.LastInterimUpdate.HasValue && !currentAuthRecord.LastLogout.HasValue) || (currentAuthRecord.LastInterimUpdate > currentAuthRecord.LastLogout))
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
                            try
                            {
                                db.Database.ExecuteSqlCommand("UPDATE RadiusAuthorization SET IsEnabled = 0 WHERE SubscriptionID = @subId;", new[] { new SqlParameter("@subId", currentAuthRecord.SubscriptionID) });
                            }
                            catch (Exception ex)
                            {
                                logger.Warn(ex, $"Could not update authorization record with subscription id [{currentAuthRecord.SubscriptionID}].");
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
