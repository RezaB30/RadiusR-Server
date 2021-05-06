using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public class CachedServerDefaults
    {
        public int FramedProtocol { get; private set; }

        public int AccountingInterimInterval { get; private set; }

        public TimeSpan RadiusSettingsRefreshInterval { get; private set; }

        public TimeSpan NASListRefreshInterval { get; private set; }

        public bool CheckCLID { get; set; }

        public CachedServerDefaults(int framedProtocol, int accountingInterimInterval, TimeSpan radiusSettingsRefreshInterval, TimeSpan nasListRefreshInterval, bool checkCLID)
        {
            FramedProtocol = framedProtocol;
            AccountingInterimInterval = accountingInterimInterval;
            RadiusSettingsRefreshInterval = radiusSettingsRefreshInterval;
            NASListRefreshInterval = nasListRefreshInterval;
            CheckCLID = checkCLID;
        }

        public CachedServerDefaults(IEnumerable<RadiusR.DB.RadiusDefault> dbSettings)
        {
            var list = dbSettings.ToDictionary(s => s.Attribute, s => s.Value);
            FramedProtocol = Convert.ToInt32(list["FramedProtocol"]);
            AccountingInterimInterval = Convert.ToInt32(list["AccountingInterimInterval"]);
            RadiusSettingsRefreshInterval = TimeSpan.ParseExact(list["RadiusSettingsRefreshInterval"], "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            NASListRefreshInterval = TimeSpan.ParseExact(list["NASListRefreshInterval"], "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
            CheckCLID = Convert.ToBoolean(list["CheckCLID"]);
        }

        public CachedServerDefaults Clone()
        {
            return new CachedServerDefaults(FramedProtocol, AccountingInterimInterval, RadiusSettingsRefreshInterval, NASListRefreshInterval, CheckCLID);
        }
    }
}
