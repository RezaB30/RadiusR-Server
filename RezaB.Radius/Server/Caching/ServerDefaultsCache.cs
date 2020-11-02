using RadiusR.DB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public class ServerDefaultsCache : CacheItemBase
    {
        private ServerDefaults ServerSettings { get; set; }

        public ServerDefaults GetSettings()
        {
            if (locker.TryEnterReadLock(2000))
            {
                try
                {
                    return ServerSettings.Clone();
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
                ServerDefaults newSettings;
                try
                {
                    var rawSettings = db.RadiusDefaults.ToArray();
                    var quotaSettings = db.AppSettings.ToArray();
                    newSettings = new ServerDefaults(rawSettings, quotaSettings);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Error loading server settings.");
                    IsLoaded = IsLoaded || false;
                    return;
                }

                if (locker.TryEnterWriteLock(10000))
                {
                    try
                    {
                        ServerSettings = newSettings;
                        IsLoaded = true;
                    }
                    finally
                    {
                        locker.ExitWriteLock();
                    }
                }
            }
        }

        public class ServerDefaults
        {
            public int FramedProtocol { get; private set; }

            public int AccountingInterimInterval { get; private set; }

            public TimeSpan DailyDisconnectionTime { get; private set; }

            public bool IncludeICMP { get; private set; }

            public TimeSpan RadiusSettingsRefreshInterval { get; private set; }

            public TimeSpan NASListRefreshInterval { get; private set; }

            public long QuotaUnit { get; private set; }

            public decimal QuotaUnitPrice { get; private set; }

            public decimal SmartQuotaPricePerByte
            {
                get
                {
                    return QuotaUnitPrice / (decimal)QuotaUnit;
                }
            }

            public ServerDefaults(IEnumerable<RadiusDefault> dbSettings, IEnumerable<AppSetting> quotaSettings)
            {
                var list = dbSettings.ToDictionary(s => s.Attribute, s => s.Value);
                var othersList = quotaSettings.ToDictionary(s => s.Key, s => s.Value);
                // general settings
                FramedProtocol = Convert.ToInt32(list["FramedProtocol"]);
                AccountingInterimInterval = Convert.ToInt32(list["AccountingInterimInterval"]);
                DailyDisconnectionTime = TimeSpan.ParseExact(list["DailyDisconnectionTime"], "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                IncludeICMP = Convert.ToBoolean(list["IncludeICMP"]);
                RadiusSettingsRefreshInterval = TimeSpan.ParseExact(list["RadiusSettingsRefreshInterval"], "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                NASListRefreshInterval = TimeSpan.ParseExact(list["NASListRefreshInterval"], "hh\\:mm\\:ss", CultureInfo.InvariantCulture);
                // quota settings
                QuotaUnitPrice = decimal.Parse(othersList["QuotaUnitPrice"], CultureInfo.InvariantCulture);
                QuotaUnit = long.Parse(othersList["QuotaUnit"], CultureInfo.InvariantCulture);
            }

            private ServerDefaults() { }

            public ServerDefaults Clone()
            {
                return new ServerDefaults()
                {
                    AccountingInterimInterval = AccountingInterimInterval,
                    DailyDisconnectionTime = DailyDisconnectionTime,
                    FramedProtocol = FramedProtocol,
                    IncludeICMP = IncludeICMP,
                    NASListRefreshInterval = NASListRefreshInterval,
                    RadiusSettingsRefreshInterval = RadiusSettingsRefreshInterval,
                    QuotaUnit = QuotaUnit,
                    QuotaUnitPrice = QuotaUnitPrice
                };
            }
        }
    }
}
