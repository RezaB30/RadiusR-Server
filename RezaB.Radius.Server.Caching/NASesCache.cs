using RadiusR.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;

namespace RezaB.Radius.Server.Caching
{
    public class NASesCache : UpdatableSettingsBase
    {
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();
        private IDictionary<IPAddress, CachedNAS> InternalDictionary { get; set; }

        public NASesCache(TimeSpan refreshRate, string connectionString) : base(refreshRate, connectionString)
        {
            Update();
        }

        public CachedNAS GetCachedNAS(IPAddress ip)
        {
            if (locker.TryEnterReadLock(2000))
            {
                try
                {
                    if (InternalDictionary.ContainsKey(ip))
                        return InternalDictionary[ip].Clone();
                    return null;
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

        public override void Update()
        {
            using (RadiusREntities db = new RadiusREntities(new EntityConnection(ConnectionString)))
            {
                Dictionary<IPAddress, CachedNAS> nasList;
                nasList = db.NAS.Where(nas => !nas.Disabled).Include(nas => nas.NASVerticalIPMaps).Include(nas => nas.NASNetmaps).Include(nas => nas.NASExpiredPools).ToArray().Select(nas => new CachedNAS(nas)).ToDictionary(c => c.NASIP, c => c);

                if (locker.TryEnterWriteLock(10000))
                {
                    try
                    {
                        InternalDictionary = nasList;
                        base.Update();
                    }
                    finally
                    {
                        locker.ExitWriteLock();
                    }
                }
            }
        }
    }
}
