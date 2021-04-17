using RezaB.Radius.Server.Caching;
using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public abstract class DynamicAccountingTask : AbortableTask
    {
        //protected DAE.DynamicAuthorizationClient DAEClient { get; set; }
        protected int DACPort { get; set; }
        protected IPAddress DACAddress { get; set; }
        protected NASesCache DAServers { get; set; }

        protected DynamicAccountingTask() { }

        public DynamicAccountingTask(NASesCache servers, int port, string IP = null)
        {
            DACPort = port;
            DACAddress = !string.IsNullOrWhiteSpace(IP) ? IPAddress.Parse(IP) : null;
            //DAEClient = !string.IsNullOrWhiteSpace(IP) ? new DAE.DynamicAuthorizationClient(port, 3000, IPAddress.Parse(IP)) : new DAE.DynamicAuthorizationClient(port, 3000);
            DAServers = servers;
        }
    }
}
