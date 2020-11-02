using RadiusR.DB;
using System.Data.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public class NASListCache : CacheItemBase
    {
        private IDictionary<string, NasClientCredentials> InternalDictionary { get; set; }

        public NASListCache()
        {
            Update();
        }

        public NasClientCredentials Get(IPAddress ip)
        {
            var ipString = ip.ToString();
            if (locker.TryEnterReadLock(2000))
            {
                try
                {
                    if (InternalDictionary.ContainsKey(ipString))
                        return InternalDictionary[ipString].Clone();
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
            using (RadiusREntities db = new RadiusREntities())
            {
                Dictionary<string, NasClientCredentials> nasList;
                try
                {
                    nasList = db.NAS.Include(nas => nas.NASVerticalIPMaps).Include(nas => nas.NASNetmaps).Include(nas => nas.NASExpiredPools).ToArray().Select(nas => new NasClientCredentials(nas)).ToDictionary(c => c.NasEndpoint.Address.ToString(), c => c);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error loading NAS list.");
                    IsLoaded = IsLoaded || false;
                    return;
                }
                if (locker.TryEnterWriteLock(10000))
                {
                    try
                    {
                        InternalDictionary = nasList;
                        IsLoaded = true;
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
