using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Threading
{
    public class CustomThreadPool<T> : IDisposable
    {
        private ConcurrentQueue<QueueItem<T>> workItemQueue;
        private Action<ConnectableItem<T>> method;
        private bool IsClosing = false;
        private int CheckIntervals;
        private int ItemDiscardDelay;
        private Random rnd;
        public int ThreadCount { get; private set; }
        public int QueueSize { get; private set; }

        public CustomThreadPool(int threadCount, Action<ConnectableItem<T>> method, string threadsPrefix, Func<int, string> formatInfo = null, int checkIntervals = 100, int itemDiscardDelay = 3000, int? queueSize = null, string connectionString = null)
        {
            formatInfo = formatInfo ?? (i => i.ToString("0000"));
            CheckIntervals = checkIntervals;
            ItemDiscardDelay = itemDiscardDelay;

            ThreadCount = threadCount;
            QueueSize = queueSize ?? threadCount;
            workItemQueue = new ConcurrentQueue<QueueItem<T>>();
            this.method = method;
            rnd = new Random();

            for (int i = 0; i < ThreadCount; i++)
            {
                Thread current = new Thread(new ThreadStart(() => workThread(connectionString)));
                current.IsBackground = true;
                current.Name = threadsPrefix + "_" + formatInfo(i);
                current.Start();
            }
        }

        private void workThread(string connectionString = null)
        {
            try
            {
                using (EntityConnection connection = connectionString != null ? new EntityConnection(connectionString) : null)
                {
                    // do until closing
                    while (!IsClosing)
                    {
                        // try to retrieve a work item
                        QueueItem<T> qItem;
                        while (workItemQueue.TryDequeue(out qItem))
                        {
                            if (qItem.TimeStamp.AddMilliseconds(ItemDiscardDelay) > DateTime.Now)
                            {
                                qItem.Item.DbConnection = connection;
                                method(qItem.Item);
                            }
                        }
                        // wait for the next cycle
                        //if (!Thread.Yield())
                        Thread.Sleep(rnd.Next(CheckIntervals / 2, CheckIntervals));
                    }
                }
            }
            catch { }
        }

        public bool TryAddWorkItem(T item)
        {
            if (workItemQueue.Count < QueueSize)
            {
                workItemQueue.Enqueue(new QueueItem<T>() { Item = new ConnectableItem<T>(item), TimeStamp = DateTime.Now });
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            IsClosing = true;
        }
    }
}
