using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using RadiusR.DB;
using System.Data.SqlClient;

namespace RezaB.Radius.DAEHelper.Tasks
{
    public class OrphanSessionCleanUp : AbortableTask
    {
        private static Logger logger = LogManager.GetLogger("orphan-session-cleanup");
        private static Logger dbLogger = LogManager.GetLogger("orphan-session-cleanup-DB");

        public override bool Run()
        {
            logger.Trace("Task started.");
            try
            {
                using (RadiusREntities db = new RadiusREntities())
                {
                    db.Database.Log = dbLogger.Trace;
                    var interimPeriod = TimeSpan.FromSeconds(Convert.ToInt32(db.RadiusDefaults.FirstOrDefault(rd => rd.Attribute == "AccountingInterimInterval").Value));
                    interimPeriod = interimPeriod.Add(TimeSpan.FromMinutes(2));
                    var temp = new RadiusAuthorization()
                    {
                        LastLogout = DateTime.Now,
                        LastInterimUpdate = DateTime.Now
                    };
                    var now = DateTime.Now;
                    var preTime = now - interimPeriod;
                    var result = db.Database.ExecuteSqlCommand("UPDATE RadiusAuthorization SET LastLogout = @currentTime WHERE LastInterimUpdate < @preTime AND (LastLogout IS NULL OR LastLogout < LastInterimUpdate);", new[] { new SqlParameter("@currentTime", now), new SqlParameter("@preTime", preTime) });
                    logger.Trace($"{result} authorization records updated.");
                    result = db.Database.ExecuteSqlCommand("UPDATE RadiusAccounting SET StopTime = @currentTime WHERE StopTime IS NULL AND (UpdateTime < @preTime OR (StartTime < @preTime AND UpdateTime IS NULL));", new[] { new SqlParameter("@currentTime", now), new SqlParameter("@preTime", preTime) });
                    logger.Trace($"{result} accounting records updated.");
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
