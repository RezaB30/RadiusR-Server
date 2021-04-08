using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server
{
    public class RadiusServerSettings
    {
        public int Port { get; set; }

        public int ThreadCount { get; set; }

        public int PoolCapacity { get; set; }

        public int ItemDiscardThreshold { get; set; }

        public string ServerLocalIP { get; set; }

        public string ConnectionString { get; set; }
    }
}
