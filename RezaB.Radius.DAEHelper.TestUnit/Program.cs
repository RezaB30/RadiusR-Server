using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.TestUnit
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Choose an option:");
            Console.WriteLine("1.(to be added).");
            Console.WriteLine("2.state-change-disconnect Task.");
            Console.WriteLine("3.expired-disconnect Task.");
            Console.WriteLine("4.quota-tasks Task.");

            var key = Console.ReadKey();
            AbortableTask task = null;
            if (key.Key != ConsoleKey.Escape)
            {
                var serversCache = new Server.Caching.NASesCache(TimeSpan.FromMinutes(15), ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString);
                switch (key.KeyChar)
                {
                    case '2':
                        {
                            task = new Tasks.DATasks.StateChangeDisconnects(serversCache, 3799, "10.180.0.2");
                            Thread temp = new Thread(new ThreadStart(() => task.Run()));
                            temp.Start();
                        }
                        break;
                    case '3':
                        {
                            task = new Tasks.DATasks.ExpirationDisconnects(serversCache, 3799, "10.180.0.2");
                            Thread temp = new Thread(new ThreadStart(() => task.Run()));
                            temp.Start();
                        }
                        break;
                    case '4':
                        {
                            task = new Tasks.DATasks.QuotaTasks(serversCache, 3799, "10.180.0.2");
                            Thread temp = new Thread(new ThreadStart(() => task.Run()));
                            temp.Start();
                        }
                        break;
                    default:
                        break;
                }
                Console.ReadKey();
                task?.Abort();
            }
        }
    }
}
