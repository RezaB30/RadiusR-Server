using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public abstract class UpdatableSettingsBase : IUpdatableSettings
    {
        public bool IsLoaded
        {
            get
            {
                return LastUpdate.HasValue;
            }
        }

        public UpdatableSettingsBase(TimeSpan refreshRate, string connectionString)
        {
            ConnectionString = connectionString;
            RefreshRate = refreshRate;
        }

        public virtual void Update()
        {
            LastUpdate = DateTime.Now;
        }

        public TimeSpan RefreshRate { get; set; }

        public DateTime? LastUpdate { get; protected set; }

        public string ConnectionString { get; protected set; }

        public bool HasExpired
        {
            get
            {
                return !IsLoaded || DateTime.Now - LastUpdate > RefreshRate;
            }
        }

    }
}
