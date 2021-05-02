using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Service
{
    public partial class RadiusServerService : ServiceBase
    {
        public RadiusServerService()
        {
            InitializeComponent();
            CanPauseAndContinue = false;
            CanShutdown = false;
        }

        protected override void OnStart(string[] args)
        {
            RadiusServers.Start();
        }

        protected override void OnStop()
        {
            RadiusServers.Stop();
        }
    }
}
