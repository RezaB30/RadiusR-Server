using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    class UpdatableCacheItem
    {
        public CacheItemBase Updatable { get; set; }

        public TimeSpan RefreshRate { get; set; }

        public DateTime? LastUpdate { get; set; }
    }
}
