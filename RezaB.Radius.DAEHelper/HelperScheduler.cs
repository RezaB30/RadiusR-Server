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
        public static Scheduler Create(TimeSpan checkInterval, string name, DACBindings dacBindings)
        {
            NASesCache cachedServers = new NASesCache(TimeSpan.FromMinutes(15), ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString);
            return new Scheduler(new SchedulerOperation[]
            {
                new SchedulerOperation("state-updates-tasks", new Tasks.DATasks.StateUpdates(cachedServers, dacBindings.Port01, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(5))), 3),
                new SchedulerOperation("expiration-disconnects-tasks", new Tasks.DATasks.ExpirationDisconnects(cachedServers, dacBindings.Port02, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(10))), 3),
                new SchedulerOperation("expiration-reconnects-tasks", new Tasks.DATasks.ExpirationReconnects(cachedServers, dacBindings.Port03, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(10))), 3),
                new SchedulerOperation("hard-quota-expiration-disconnects-tasks", new Tasks.DATasks.HardQuotaExpirationDisconnects(cachedServers, dacBindings.Port04, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(30))), 3),
                new SchedulerOperation("soft-quota-rate-limit-updates-tasks", new Tasks.DATasks.SoftQuotaRateLimitUpdates(cachedServers, dacBindings.Port05, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(30))), 3),
                new SchedulerOperation("smart-quota-checks-tasks", new Tasks.DATasks.SmartQuotaChecks(cachedServers, dacBindings.Port06, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(35))), 3),
                new SchedulerOperation("quota-warnings-tasks", new Tasks.DATasks.QuotaWarnings(cachedServers, dacBindings.Port07, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(35))), 3),
                new SchedulerOperation("rate-limit-updates-tasks", new Tasks.DATasks.RateLimitUpdates(cachedServers, dacBindings.Port08, dacBindings.BindingAddress), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(15))), 3),
                new SchedulerOperation("orphan-session-cleanup", new Tasks.OrphanSessionCleanUp(), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(1)))),
                new SchedulerOperation("hard-quota-flag-clears", new Tasks.HardQuotaFlagClears(), new Scheduling.StartParameters.SchedulerTimingOptions(new Scheduling.StartParameters.SchedulerIntervalTimeSpan(TimeSpan.FromMinutes(10))), 1)
            }, checkInterval, name);
        }
    }
}
