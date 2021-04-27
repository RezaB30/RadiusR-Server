using NLog;
using RadiusR.DB;
using RezaB.Radius.PacketStructure;
using RezaB.Radius.Server.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public class ExpirationReconnects : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("expiration-reconnects");
        private static Logger dbLogger = LogManager.GetLogger("expiration-reconnects-DB");

        public ExpirationReconnects(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

        public override bool Run()
        {
            logger.Trace("Task started.");
            using (RadiusREntities db = new RadiusREntities())
            {
                // prepare query
                db.Database.Log = dbLogger.Trace;
                var searchQuery = db.RadiusAuthorizations.OrderBy(s => s.SubscriptionID).Where(s => s.IsEnabled && s.ExpirationDate > DateTime.Now && s.UsingExpiredPool && s.Subscription.Service.QuotaType != (short)RadiusR.DB.Enums.QuotaType.HardQuota && ((s.LastInterimUpdate.HasValue && !s.LastLogout.HasValue) || (s.LastInterimUpdate > s.LastLogout)));
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
                            // nas update
                            try
                            {
                                DAClient.Send(new IPEndPoint(nas.NASIP, nas.IncomingPort), new DynamicAuthorizationExtentionPacket(MessageTypes.DisconnectRequest, new[] { new RadiusAttribute(AttributeType.UserName, currentAuthRecord.Username) }), nas.Secret);
                            }
                            catch (Exception ex)
                            {
                                logger.Warn(ex, $"Could not disconnect [{currentAuthRecord.Username}] from [{currentAuthRecord.NASIP}].");
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
