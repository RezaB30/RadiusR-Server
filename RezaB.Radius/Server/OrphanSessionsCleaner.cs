using NLog;
using RadiusR.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server
{
    public static class OrphanSessionsCleaner
    {
        private static Logger logger = LogManager.GetLogger("OrphanSessionsCleaner");
        private static readonly TimeSpan schedulerPeriod = TimeSpan.FromMinutes(5);
        private static TimeSpan maxInterimUpdate;
        private static bool IsStopped = false;
        private static Thread scheduler;
        public static void Start(TimeSpan maxInterimUpdate)
        {
            IsStopped = false;
            OrphanSessionsCleaner.maxInterimUpdate = maxInterimUpdate;
            scheduler = new Thread(new ThreadStart(_scheduler));
            scheduler.Name = "Orphan Session Cleaner";
            scheduler.IsBackground = true;
            scheduler.Start();
            logger.Trace("Orphan session cleaner started.");
        }

        public static void Stop()
        {
            logger.Trace("Stopping session cleaner.");
            IsStopped = true;
            scheduler.Join();
            logger.Trace("Session cleaner stopped.");
        }

        private static void _scheduler()
        {
            DateTime lastOperationTime = DateTime.Now;
            while (!IsStopped)
            {
                var maxAllowedTime = DateTime.Now.Subtract(maxInterimUpdate).Subtract(TimeSpan.FromSeconds(10));
                try
                {
                    using (RadiusREntities db = new RadiusREntities())
                    {
                        logger.Trace("Checking for orphan sessions...");
                        // ------- to check for entity changes (name changes)
                        new RadiusAccounting()
                        {
                            StopTime = new DateTime(),
                            StartTime = new DateTime(),
                            UpdateTime = new DateTime(),
                            TerminateCause = (short)Packet.AttributeEnums.AcctTerminateCause.AdminReset
                        };
                        // --------------------------------------------------
                        var updateCount = db.Database.ExecuteSqlCommand("UPDATE [RadiusAccounting] SET [StopTime] = @currentTime, [TerminateCause] = @terminateCause WHERE [StopTime] IS NULL AND ([UpdateTime] < @timeLimit OR ([UpdateTime] IS NULL AND [StartTime] < @timeLimit));",
                            new System.Data.SqlClient.SqlParameter("@currentTime", DateTime.Now),
                            new System.Data.SqlClient.SqlParameter("@timeLimit", maxAllowedTime),
                            new System.Data.SqlClient.SqlParameter("@terminateCause", (short)Packet.AttributeEnums.AcctTerminateCause.AdminReset)
                            );
                        //var orphanSessions = db.RadiusAccountings.Where(ra => !ra.StopTime.HasValue).Where(ra => ra.UpdateTime < maxAllowedTime || (!ra.UpdateTime.HasValue && ra.StartTime < maxAllowedTime));
                        //orphanSessions.ToList().ForEach(ra =>
                        //{
                        //    ra.StopTime = DateTime.Now;
                        //    ra.TerminateCause = (short)Packet.AttributeEnums.AcctTerminateCause.AdminReset;
                        //});
                        //var updateCount = db.SaveChanges();
                        logger.Trace("{0} orphan sessions closed.", updateCount);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error in orphan session cleaner");
                }

                lastOperationTime = DateTime.Now;
                while (lastOperationTime.Add(schedulerPeriod) > DateTime.Now && !IsStopped)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
