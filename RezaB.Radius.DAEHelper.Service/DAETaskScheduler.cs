using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Service
{
    static class DAETaskScheduler
    {
        private static Scheduler taskScheduler = null;

        public static void Start()
        {
            taskScheduler = taskScheduler ?? HelperScheduler.Create(Properties.Settings.Default.SchedulerCheckInterval, "DAE Task Scheduler", new DACBindings()
            {
                BindingAddress = Properties.Settings.Default.BindingAddress,
                Port01 = Properties.Settings.Default.Port01,
                Port02 = Properties.Settings.Default.Port02,
                Port03 = Properties.Settings.Default.Port03,
                Port04 = Properties.Settings.Default.Port04,
                Port05 = Properties.Settings.Default.Port05,
                Port06 = Properties.Settings.Default.Port06,
                Port07 = Properties.Settings.Default.Port07,
                Port08 = Properties.Settings.Default.Port08
            });

            taskScheduler.Start();
        }

        public static void Stop()
        {
            taskScheduler?.Stop();
        }
    }
}
