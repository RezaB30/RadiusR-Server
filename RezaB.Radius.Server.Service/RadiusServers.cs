using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RezaB.Radius.Server.Implementations;

namespace RezaB.Radius.Server.Service
{
    static class RadiusServers
    {
        private static AuthenticationServer authServer = new AuthenticationServer();
        private static AccountingServer accServer = new AccountingServer();

        public static void Start()
        {
            authServer.Start(new RadiusServerSettings()
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString,
                ItemDiscardThreshold = Properties.Settings.Default.AuthItemDiscardThreshold,
                PoolCapacity = Properties.Settings.Default.AuthPoolCapacity,
                Port = Properties.Settings.Default.AuthPort,
                ServerLocalIP = Properties.Settings.Default.AuthServerLocalIP,
                ThreadCount = Properties.Settings.Default.AuthThreadCount
            }, new[] { PacketStructure.MessageTypes.AccessRequest });

            accServer.Start(new RadiusServerSettings()
            {
                ConnectionString = ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString,
                ItemDiscardThreshold = Properties.Settings.Default.AccItemDiscardThreshold,
                PoolCapacity = Properties.Settings.Default.AccPoolCapacity,
                Port = Properties.Settings.Default.AccPort,
                ServerLocalIP = Properties.Settings.Default.AccServerLocalIP,
                ThreadCount = Properties.Settings.Default.AccThreadCount
            }, new[] { PacketStructure.MessageTypes.AccountingRequest });
        }

        public static void Stop()
        {
            authServer.Stop();
            accServer.Stop();
        }
    }
}
