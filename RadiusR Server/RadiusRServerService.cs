using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RadiusR_Server
{
    public partial class RadiusRServerService : ServiceBase
    {
        public RadiusRServerService()
        {
            InitializeComponent();
            CanPauseAndContinue = false;
            CanShutdown = false;
        }

        protected override void OnStart(string[] args)
        {
            RadiusRServer.Start();
        }

        protected override void OnStop()
        {
            RadiusRServer.Stop();
        }
    }
}
