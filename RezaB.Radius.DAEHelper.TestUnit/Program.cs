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
            while (true)
            {
                Console.WriteLine("Choose an option:");
                Console.WriteLine("0.Run orphan-session-cleanup.");
                Console.WriteLine("1.Run scheduler (full).");
                Console.WriteLine("2.state-updates Task.");
                Console.WriteLine("3.expiration-disconnects Task.");
                Console.WriteLine("4.expiration-reconnects Task.");
                Console.WriteLine("5.hard-quota-expiration-disconnects Task.");
                Console.WriteLine("6.soft-quota-rate-limit-updates Task.");
                Console.WriteLine("7.smart-quota-checks Task.");
                Console.WriteLine("8.quota-warnings Task.");
                Console.WriteLine("9.rate-limit-updates Task.");
                Console.WriteLine("a.hard-quota-flag-clears Task.");
                Console.WriteLine("ESC: Exit.");

                var key = Console.ReadKey();
                Console.WriteLine();
                AbortableTask task = null;
                Scheduler scheduler = null;
                if (key.Key != ConsoleKey.Escape)
                {
                    var serversCache = new Server.Caching.NASesCache(TimeSpan.FromMinutes(15), ConfigurationManager.ConnectionStrings["RadiusREntities"].ConnectionString);
                    switch (key.KeyChar)
                    {
                        case '0':
                            {
                                task = new Tasks.OrphanSessionCleanUp();
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        case '1':
                            scheduler = HelperScheduler.Create(TimeSpan.FromMinutes(1), "DAE Helper", new DACBindings()
                            {
                                BindingAddress = "10.180.0.2",
                                Port01 = 13300,
                                Port02 = 13301,
                                Port03 = 13302,
                                Port04 = 13303,
                                Port05 = 13304,
                                Port06 = 13305,
                                Port07 = 13306,
                                Port08 = 13307
                            });
                            scheduler.Start();
                            break;
                        case '2':
                            {
                                task = new Tasks.DATasks.StateUpdates(serversCache, 3799, "10.180.0.2");
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
                                task = new Tasks.DATasks.ExpirationReconnects(serversCache, 3799, "10.180.0.2");
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        case '5':
                            {
                                task = new Tasks.DATasks.HardQuotaExpirationDisconnects(serversCache, 3799, "10.180.0.2");
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        case '6':
                            {
                                task = new Tasks.DATasks.SoftQuotaRateLimitUpdates(serversCache, 3799, "10.180.0.2");
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        case '7':
                            {
                                task = new Tasks.DATasks.SmartQuotaChecks(serversCache, 3799, "10.180.0.2");
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        case '8':
                            {
                                task = new Tasks.DATasks.QuotaWarnings(serversCache, 3799, "10.180.0.2");
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        case '9':
                            {
                                task = new Tasks.DATasks.RateLimitUpdates(serversCache, 3799, "10.180.0.2");
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        case 'a':
                        case 'A':
                            {
                                task = new Tasks.HardQuotaFlagClears();
                                Thread temp = new Thread(new ThreadStart(() => task.Run()));
                                temp.Start();
                            }
                            break;
                        default:
                            break;
                    }
                    Console.ReadKey();
                    Console.WriteLine();
                    task?.Abort();
                    scheduler?.Stop();
                }
                else
                {
                    break;
                }
            }
        }
    }
}
