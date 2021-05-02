using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Service
{
    public partial class DAETaskSchedulerService : ServiceBase
    {
        public DAETaskSchedulerService()
        {
            InitializeComponent();
            CanPauseAndContinue = false;
            CanShutdown = false;
        }

        protected override void OnStart(string[] args)
        {
            DAETaskScheduler.Start();
        }

        protected override void OnStop()
        {
            DAETaskScheduler.Stop();
        }
    }
}
