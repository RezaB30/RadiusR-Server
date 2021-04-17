using NLog;
using RadiusR.DB;
using RezaB.Radius.Server.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public class HardQuotaTasks : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("hard-quota-tasks");
        private static Logger dbLogger = LogManager.GetLogger("hard-quota-tasks-DB");

        public HardQuotaTasks(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

        public override bool Run()
        {
            logger.Trace("Task started.");
            using (RadiusREntities db = new RadiusREntities())
            {
                // prepare query
                db.Database.Log = dbLogger.Trace;
                var searchQuery = db.RadiusAuthorizations.Include(s => s.Subscription).OrderBy(s => s.SubscriptionID).Where(s => s.IsEnabled && s.Subscription.Service.QuotaType == (short)RadiusR.DB.Enums.QuotaType.HardQuota);
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
