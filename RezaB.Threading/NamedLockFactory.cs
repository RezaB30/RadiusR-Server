using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Threading
{
    public class NamedLockFactory : IDisposable
    {
        private ConcurrentDictionary<string, object> InternalDictionary = new ConcurrentDictionary<string, object>();
        private bool IsDisposed = false;
        private Thread GarbageCleanerThread;

        public object GetNamedLock(string key)
        {
            return InternalDictionary.GetOrAdd(key, new object());
        }

        public NamedLockFactory(TimeSpan clearIntervals)
        {
            GarbageCleanerThread = new Thread(new ThreadStart(() => GarbageCleaner(clearIntervals)));
            GarbageCleanerThread.IsBackground = true;
            GarbageCleanerThread.Start();
        }

        private void GarbageCleaner(TimeSpan clearintervals)
        {
            var lastOperationTime = DateTime.Now;
            while (!IsDisposed)
            {
                var checkThreshold = lastOperationTime.Add(clearintervals);
                if (checkThreshold < DateTime.Now)
                {
                    var allKeys = InternalDictionary.Keys.ToList();
                    foreach (var key in allKeys)
                    {
                        var currentLock = GetNamedLock(key);
                        if (Monitor.TryEnter(currentLock, 0))
                        {
                            try
                            {
                                object temp;
                                InternalDictionary.TryRemove(key, out temp);
                            }
                            finally
                            {
                                Monitor.Exit(currentLock);
                            }
                        }
                    }
                    lastOperationTime = DateTime.Now;
                }
                Thread.Sleep(200);
            }
        }

        public void Dispose()
        {
            IsDisposed = true;
            GarbageCleanerThread.Join();
        }
    }
}
