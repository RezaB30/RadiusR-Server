using RadiusR.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    class ServerDefaultsCache : UpdatableSettingsBase
    {
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private CachedServerDefaults ServerSettings { get; set; }

        public ServerDefaultsCache(TimeSpan refreshRate) : base(refreshRate)
        {
            Update();
        }

        public override void Update()
        {
            using (RadiusREntities db = new RadiusREntities())
            {
                CachedServerDefaults newSettings;
                var rawSettings = db.RadiusDefaults.ToArray();
                newSettings = new CachedServerDefaults(rawSettings);

                if (locker.TryEnterWriteLock(10000))
                {
                    try
                    {
                        ServerSettings = newSettings;
                        base.Update();
                    }
                    finally
                    {
                        locker.ExitWriteLock();
                    }
                }
            }
        }

        public CachedServerDefaults GetSettings()
        {
            if (locker.TryEnterReadLock(2000))
            {
                try
                {
                    return ServerSettings.Clone();
                }
                finally
                {
                    locker.ExitReadLock();
                }
            }
            else
            {
                return null;
            }
        }
    }
}
