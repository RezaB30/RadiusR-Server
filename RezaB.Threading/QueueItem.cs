using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Threading
{
    class QueueItem<TItem>
    {
        public ConnectableItem<TItem> Item { get; set; }

        public DateTime TimeStamp { get; set; }

    }
}
