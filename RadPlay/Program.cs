using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using RezaB.Radius.Packet;
using RezaB.Radius.Server;
using RezaB.Radius.Caching;
using RadiusRServerTest.Properties;
using NLog;
using System.Threading;
using System.Configuration;

namespace RadiusRServerTest
{
    class Program
    {
        private static Logger consoleLogger = LogManager.GetLogger("console");
        static void Main(string[] args)
        {
            Thread.CurrentThread.Name = "Main";
            consoleLogger.Trace("RadiusR Server - v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

            //// start orphan session cleaner
            //OrphanSessionsCleaner.Start(Settings.Default.maxInterimUpdate);

            // starting authentication server
            AuthenticationServer AuthServer = new AuthenticationServer();
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
            AccountingServer AccServer = new AccountingServer();
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

            //Thread.Sleep(5000);

            //OrphanSessionsCleaner.Stop();
            //AuthServer.Stop();
            //AccServer.Stop();
        }
    }
}
