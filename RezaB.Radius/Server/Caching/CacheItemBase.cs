using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public abstract class CacheItemBase : IUpdatable
    {
        protected Logger logger = LogManager.GetLogger("CacheItem");
        protected ReaderWriterLockSlim locker = new ReaderWriterLockSlim();

        public bool IsLoaded { get; protected set; }

        public abstract void Update();
    }
}
