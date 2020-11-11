using System;
using System.Collections.Generic;
using System.Data.Entity.Core.EntityClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Threading
{
    public class ConnectableItem<TItem>
    {
        public TItem Item { get; private set; }

        public EntityConnection DbConnection { get; internal set; }

        public ConnectableItem(TItem item)
        {
            Item = item;
        }
    }
}
