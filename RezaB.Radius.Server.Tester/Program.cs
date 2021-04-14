using RezaB.Radius.PacketStructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace RezaB.Radius.Server.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var authServer = new Implementations.AuthenticationServer();
            var acctServer = new Implementations.AccountingServer();

            Console.WriteLine("Press any key to start the server or ESC to close...Press any key while server is running to stop the server.");
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                return;
            }

            authServer.Start(new RadiusServerSettings()
            {
                ConnectionString = Properties.Settings.Default.ConnectionString,
                ItemDiscardThreshold = Properties.Settings.Default.ItemDiscardThreshold,
                PoolCapacity = Properties.Settings.Default.AuthPoolCapacity,
                Port = Properties.Settings.Default.AuthPort,
                ServerLocalIP = Properties.Settings.Default.ServerLocalIP,
                ThreadCount = Properties.Settings.Default.AuthThreadCount
            }, new[] { MessageTypes.AccessRequest });

            acctServer.Start(new RadiusServerSettings()
            {
                ConnectionString = Properties.Settings.Default.ConnectionString,
                ItemDiscardThreshold = Properties.Settings.Default.ItemDiscardThreshold,
                PoolCapacity = Properties.Settings.Default.AccPoolCapacity,
                Port = Properties.Settings.Default.AccPort,
                ServerLocalIP = Properties.Settings.Default.ServerLocalIP,
                ThreadCount = Properties.Settings.Default.AccThreadCount
            }, new[] { MessageTypes.AccountingRequest });

            Console.ReadKey();
            Console.WriteLine("Stopping the server...");
            authServer.Stop();
            acctServer.Stop();
            Console.WriteLine("Server stopped. Press any key to close.");
            Console.ReadKey();
        }
    }
}
