using RadiusR.DB;
using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public class UserProfilesCache : UpdatableSettingsBase
    {
        private ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        private IDictionary<int, string> InternalDictionary { get; set; }

        public UserProfilesCache(TimeSpan refreshRate, string connectionString) : base(refreshRate, connectionString)
        {
            Update();
        }

        public string GetCachedPoolName(int id)
        {
            if (locker.TryEnterReadLock(2000))
            {
                try
                {
                    if (InternalDictionary.ContainsKey(id))
                        return InternalDictionary[id];
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
                Dictionary<int, string> profileList;
                profileList = db.RadiusProfiles.ToArray().Select(rp => new { ID = rp.ID, PoolName = rp.PoolName }).ToDictionary(rp => rp.ID, rp => rp.PoolName);

                if (locker.TryEnterWriteLock(10000))
                {
                    try
                    {
                        InternalDictionary = profileList;
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
