using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public class SettingsCache : IDisposable
    {
        private static Logger cacheLogger = LogManager.GetLogger("server-cache");
        private bool _isDisposed = false;

        public NASesCache NASListCache { get;  private set; }

        public ServerDefaultsCache ServerSettingsCache { get; private set; }

        public UserProfilesCache UserProfilesCache { get; private set; }

        private Thread RefresherThread { get; set; }

        public SettingsCache(string connectionString)
        {
            ServerSettingsCache = new ServerDefaultsCache(TimeSpan.FromMinutes(5), connectionString);
            ServerSettingsCache.RefreshRate = ServerSettingsCache.GetSettings().RadiusSettingsRefreshInterval;
            NASListCache = new NASesCache(ServerSettingsCache.GetSettings().NASListRefreshInterval, connectionString);
            UserProfilesCache = new UserProfilesCache(ServerSettingsCache.RefreshRate, connectionString);
            // refresher
            RefresherThread = new Thread(new ThreadStart(Refresh));
            RefresherThread.IsBackground = true;
            RefresherThread.Start();
        }

        private void Refresh()
        {
            while (!_isDisposed)
            {
                try
                {
                    if (ServerSettingsCache.HasExpired)
                    {
                        ServerSettingsCache.Update();
                        ServerSettingsCache.RefreshRate = ServerSettingsCache.GetSettings().RadiusSettingsRefreshInterval;
                    }
                    if (NASListCache.HasExpired)
                    {
                        NASListCache.Update();
                        NASListCache.RefreshRate = ServerSettingsCache.GetSettings().NASListRefreshInterval;
                    }
                    if (UserProfilesCache.HasExpired)
                    {
                        UserProfilesCache.Update();
                        UserProfilesCache.RefreshRate = ServerSettingsCache.RefreshRate;
                    }
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    cacheLogger.Error(ex, "Error updating cache.");
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                }
            }
        }

        public void Dispose()
        {
            _isDisposed = true;
        }
    }
}
