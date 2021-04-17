using RezaB.Radius.Server.Caching;
using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper
{
    public static class HelperScheduler
    {
        public static Scheduler Create(TimeSpan checkInterval, string name)
        {
            NASesCache cachedServers = new NASesCache(TimeSpan.FromMinutes(15), ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString);
            return new Scheduler(new SchedulerOperation[]
            {
                new SchedulerOperation("dynamic-authorization-tasks", new Tasks.DATasks.StateChangeDisconnects(cachedServers, 31100, "IP"), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(30))), 3),
                new SchedulerOperation("orphan-session-cleanup", new Tasks.OrphanSessionCleanUp(), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(3))))
            }, checkInterval, name);
        }
    }
}
