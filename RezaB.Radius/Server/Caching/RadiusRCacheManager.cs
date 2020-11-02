using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public class RadiusRCacheManager : IDisposable
    {
        private Logger logger = LogManager.GetLogger("CacheManager");
        // reference to cache items for refreshing
        private static IEnumerable<UpdatableCacheItem> CacheItems { set; get; }
        // Specific cache items
        public NASListCache NASList { get; private set; }
        public ServerDefaultsCache ServerSettings { get; private set; }


        public bool IsPreLoaded { get; private set; }
        // updater props
        private Thread updaterThread;
        private bool isDisposing;

        public RadiusRCacheManager()
        {
            // create cache items
            ServerSettings = new ServerDefaultsCache();
            NASList = new NASListCache();
            // create cache containers
            CacheItems = new[]
            {
                new UpdatableCacheItem()
                {
                    Updatable = ServerSettings,
                    LastUpdate = null,
                    RefreshRate = TimeSpan.FromMinutes(15)
                },
                new UpdatableCacheItem()
                {
                    Updatable = NASList,
                    LastUpdate = null,
                    RefreshRate = TimeSpan.FromMinutes(15)
                }
            };
            if (!UpdateItems())
            {
                logger.Fatal("Unable to load server settings.");
                IsPreLoaded = false;
                return;
            }
            IsPreLoaded = true;
            // start updater
            updaterThread = new Thread(new ThreadStart(Updater));
            updaterThread.IsBackground = true;
            updaterThread.Start();
        }

        private void UpdateRefreshRates()
        {
            var serverSettings = ServerSettings.GetSettings();
            foreach (var item in CacheItems)
            {
                if (item.Updatable is ServerDefaultsCache)
                    item.RefreshRate = serverSettings.RadiusSettingsRefreshInterval;
                else if (item.Updatable is NASListCache)
                    item.RefreshRate = serverSettings.NASListRefreshInterval;
            }
        }

        private void Updater()
        {
            while (!isDisposing)
            {
                UpdateItems();

                Thread.Sleep(1000);
            }
        }

        private bool UpdateItems()
        {
            foreach (var item in CacheItems)
            {
                if (!item.LastUpdate.HasValue || item.LastUpdate + item.RefreshRate < DateTime.Now)
                {
                    item.Updatable.Update();
                    item.LastUpdate = DateTime.Now;
                    if (!item.Updatable.IsLoaded)
                    {
                        // throw exception
                        logger.Error("One of the cache items is not loaded: {0}", item.Updatable.GetType().FullName);
                        return false;
                    }
                    if (item.Updatable is ServerDefaultsCache)
                        UpdateRefreshRates();
                }
            }

            return true;
        }

        public void Dispose()
        {
            isDisposing = true;
            updaterThread.Join();
        }
    }
}
