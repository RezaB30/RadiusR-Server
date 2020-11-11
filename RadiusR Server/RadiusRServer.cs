using NLog;
using RadiusR_Server.Properties;
using RezaB.Radius.Server;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RadiusR_Server
{
    static class RadiusRServer
    {
        private static Logger consoleLogger = LogManager.GetLogger("console");
        private static AuthenticationServer AuthServer = new AuthenticationServer();
        private static AccountingServer AccServer = new AccountingServer();

        public static void Start()
        {
            Thread.CurrentThread.Name = "Main";
            consoleLogger.Trace("RadiusR Server - v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

            // initializing clients transfer rate cache
            //ClientTransferRateCache.Initialize(Settings.Default.DropDuplicates);
            // start orphan session cleaner
            OrphanSessionsCleaner.Start(Settings.Default.maxInterimUpdate);

            // starting authentication server
            var settings = new RadiusServerSettings()
            {
                Port = Settings.Default.AuthenticationServerPort,
                ServerLocalIP = Settings.Default.ServerLocalIP,
                ThreadCount = Settings.Default.AuthenticationServerThreadCount,
                PoolCapacity = Settings.Default.AuthenticationPoolCapacity,
                ItemDiscardThreshold = Settings.Default.AuthenticationItemDiscardThreshold,
                ConnectionString = ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString
            };
            AuthServer.Start(settings);

            // starting accounting server
            settings = new RadiusServerSettings()
            {
                Port = Settings.Default.AccountingServerPort,
                ServerLocalIP = Settings.Default.ServerLocalIP,
                ThreadCount = Settings.Default.AccountingServerThreadCount,
                COAThreadCount = Settings.Default.COAThreadCount,
                COAPoolCapacity = Settings.Default.COAPoolCapacity,
                PoolCapacity = Settings.Default.AccountingPoolCapacity,
                ItemDiscardThreshold = Settings.Default.AccountingItemDiscardThreshold,
                ConnectionString = ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString
            };
            AccServer.Start(settings);
        }

        public static void Stop()
        {
            OrphanSessionsCleaner.Stop();
            AuthServer.Stop();
            AccServer.Stop();
        }
    }
}
