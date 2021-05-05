using NLog;
using RadiusR.DB;
using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Tasks
{
    public class HardQuotaFlagClears : AbortableTask
    {

        private static Logger logger = LogManager.GetLogger("hard-quota-flag-clears");
        private static Logger dbLogger = LogManager.GetLogger("hard-quota-flag-clears-DB");

        public override bool Run()
        {
            logger.Trace("Task started.");
            try
            {
                using (RadiusREntities db = new RadiusREntities())
                {
                    db.Database.Log = dbLogger.Trace;
                    var temp = new RadiusAuthorization()
                    {
                        LastLogout = DateTime.Now,
                        LastInterimUpdate = DateTime.Now
                    };

                    var result = db.Database.ExecuteSqlCommand("UPDATE RA SET IsHardQuotaExpired = NULL FROM RadiusAuthorization RA INNER JOIN Subscription SUB ON RA.SubscriptionID = SUB.ID INNER JOIN [Service] SRV ON SRV.ID = SUB.ServiceID WHERE (SRV.QuotaType IS NULL OR SRV.QuotaType != @hardQType) AND RA.IsHardQuotaExpired IS NOT NULL;", new[] { new SqlParameter("@hardQType", (short)RadiusR.DB.Enums.QuotaType.HardQuota) });
                    logger.Trace($"{result} authorization records updated.");
                    logger.Trace("Task done.");
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return false;
            }

            return true;
        }
    }
}
