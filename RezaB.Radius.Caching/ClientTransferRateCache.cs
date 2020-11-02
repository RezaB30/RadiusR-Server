using NLog;
using RadiusR.DB;
using RezaB.Mikrotik.Extentions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Caching
{
    public static class ClientTransferRateCache
    {
        private static Logger logger = LogManager.GetLogger("RateLimitCache");
        private static int timeout = 10000;
        private static int updatePeriod = 900000;
        private static Dictionary<string, string> _internalList;
        private static bool ShouldDropDuplicates { get; set; }

        private static ReaderWriterLock cacheLock = new ReaderWriterLock();

        public static void Initialize(bool shouldDropDuplicates)
        {
            ShouldDropDuplicates = shouldDropDuplicates;
            _internalList = new Dictionary<string, string>();
            // start auto updater thread
            Thread updater = new Thread(new ThreadStart(() => _autoUpdater()));
            updater.Name = "RateLimitCacheUpdater";
            updater.Start();
        }

        public static string GetClientRateLimit(string username)
        {
            try
            {
                cacheLock.AcquireReaderLock(timeout);
                try
                {
                    string value;
                    if (_internalList.TryGetValue(username, out value))
                        return value;
                    return null;
                }
                finally
                {
                    cacheLock.ReleaseReaderLock();
                }
            }
            catch (ApplicationException)
            {
                return null;
            }
        }

        private static void _syncProc(MikrotikRouter router, ConcurrentBag<Dictionary<string, string>> results)
        {
            try
            {
                results.Add(router.GetCurrentRateLimits());
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return;
            }
        }

        private static void SyncWithRouters()
        {
            // getting mikrotik credentials
            List<MikrotikRouter> routers;
            using (RadiusREntities db = new RadiusREntities())
            {
                var credentials = db.NAS.ToList().Select(nas => new MikrotikApiCredentials(nas.IP, nas.ApiPort, nas.ApiUsername, nas.ApiPassword));
                routers = credentials.Select(credential => new MikrotikRouter(credential)).ToList();
            }
            var tasks = routers.Select(router => new Task<Dictionary<string, string>>(() => router.GetCurrentRateLimits(), TaskCreationOptions.LongRunning | TaskCreationOptions.PreferFairness)).ToList();

            try
            {
                cacheLock.AcquireWriterLock(timeout);
                try
                {
                    // create sync update threads
                    var results = new ConcurrentBag<Dictionary<string, string>>();
                    var threads = new List<Thread>();
                    var index = 0;
                    foreach (var router in routers)
                    {
                        var processThread = new Thread(new ThreadStart(() => _syncProc(router, results)));
                        threads.Add(processThread);
                        processThread.Start();
                        index++;
                    }
                    // wait for all to finish
                    foreach (var thread in threads)
                    {
                        thread.Join();
                    }
                    Dictionary<string, string> mergedResult = new Dictionary<string, string>();
                    var unorderedSet = results.Where(resultSet => resultSet != null).SelectMany(resultSet => resultSet);
                    var uniques = new HashSet<string>();
                    var duplicates = new HashSet<string>();
                    // check for duplicates and drop them if found
                    foreach (var item in unorderedSet)
                    {
                        if (!uniques.Add(item.Key))
                        {
                            duplicates.Add(item.Key);
                        }
                    }
                    if (duplicates.Any())
                    {
                        var duplicateList = string.Join(",", uniques);
                        logger.Warn("Found duplicate connections with the same usernames: {0}", duplicateList);
                        // disconnect duplicates
                        if (ShouldDropDuplicates)
                            routers.ForEach(router => new Task(() => router.DisconnectUser(duplicates.ToArray())).Start());
                    }
                    //logger.Trace(string.Join(Environment.NewLine, results.SelectMany(r => r).Select(r => r.Key.PadRight(30) + ":" + r.Value)));
                    mergedResult = unorderedSet.Where(resultsSet => !uniques.Contains(resultsSet.Key)).ToDictionary(item => item.Key, item => item.Value);

                    _internalList = mergedResult;
                    //logger.Trace(string.Join(Environment.NewLine, _internalList.Select(list => list.Key + ":" + list.Value)));
                }
                finally
                {
                    cacheLock.ReleaseWriterLock();
                }
            }
            catch (ApplicationException ex)
            {
                logger.Warn(ex, "Unable to acquire writer lock to refresh internal list.");
            }

        }

        private static void _autoUpdater()
        {
            while (true)
            {
                try
                {
                    logger.Trace("Syncing rate limit cache with routers...");
                    SyncWithRouters();
                    logger.Trace("Syncing done.");
                    Thread.Sleep(updatePeriod);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error syncing rate limit cache!");
                    Thread.Sleep(updatePeriod / 2);
                }
            }
        }

        public static void SetClientRateLimit(string username, string rateLimit)
        {
            var retries = 0;
            while (retries < 5)
            {
                try
                {
                    cacheLock.AcquireWriterLock(timeout);
                    try
                    {
                        _internalList[username] = rateLimit;
                    }
                    finally
                    {
                        cacheLock.ReleaseWriterLock();
                        retries = 5;
                    }
                }
                catch (ApplicationException ex)
                {
                    Thread.Sleep(500);
                    retries++;
                    if (retries >= 5)
                    {
                        logger.Warn(ex, string.Format("Unable to set value for {0}: {1}", username, rateLimit));
                    }
                }
            }
        }

        public static void SetClientRateLimitAsync(string username, string rateLimit)
        {
            return;
            Thread t = new Thread(new ThreadStart(() => SetClientRateLimit(username, rateLimit)));
            t.Name = "RateLimitCache";
            t.Start();
        }
    }
}
