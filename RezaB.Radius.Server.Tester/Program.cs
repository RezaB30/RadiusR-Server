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
            var server = new Implementations.AuthenticationServer();

            Console.WriteLine("Press any key to start the server or ESC to close...Press any key while server is running to stop the server.");
            var key = Console.ReadKey();
            if (key.Key == ConsoleKey.Escape)
            {
                return;
            }

            server.Start(new RadiusServerSettings()
            {
                ConnectionString = Properties.Settings.Default.ConnectionString,
                ItemDiscardThreshold = Properties.Settings.Default.ItemDiscardThreshold,
                PoolCapacity = Properties.Settings.Default.PoolCapacity,
                Port = Properties.Settings.Default.Port,
                ServerLocalIP = Properties.Settings.Default.ServerLocalIP,
                ThreadCount = Properties.Settings.Default.ThreadCount
            });

            Console.ReadKey();
            Console.WriteLine("Stopping the server...");
            server.Stop();
            Console.WriteLine("Server stopped. Press any key to close.");
            Console.ReadKey();
        }
    }
}
