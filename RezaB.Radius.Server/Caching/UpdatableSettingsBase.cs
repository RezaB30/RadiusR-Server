using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    abstract class UpdatableSettingsBase : IUpdatableSettings
    {
        public bool IsLoaded
        {
            get
            {
                return LastUpdate.HasValue;
            }
        }

        public UpdatableSettingsBase(TimeSpan refreshRate)
        {
            RefreshRate = refreshRate;
        }

        public virtual void Update()
        {
            LastUpdate = DateTime.Now;
        }

        public TimeSpan RefreshRate { get; set; }

        public DateTime? LastUpdate { get; protected set; }

    }
}
