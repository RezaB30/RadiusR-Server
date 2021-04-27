using NLog;
using RadiusR.DB;
using RadiusR.DB.Enums;
using RadiusR.DB.ModelExtentions;
using RezaB.Radius.Server.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using RadiusR.SMS;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public class SmartQuotaChecks : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("smart-quota-checks");
        private static Logger dbLogger = LogManager.GetLogger("smart-quota-checks-DB");

        public SmartQuotaChecks(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

        public override bool Run()
        {
            logger.Trace("Task started.");
            using (RadiusREntities db = new RadiusREntities())
            {
                // prepare query
                db.Database.Log = dbLogger.Trace;
                var searchQuery = db.RadiusAuthorizations.Include(s => s.Subscription.RadiusSMS).OrderBy(s => s.SubscriptionID).Where(s => s.IsEnabled && s.Subscription.Service.QuotaType == (short)QuotaType.SmartQuota);
                long currentId = 0;
                // quota price per byte
                var quotaPricePerByte = QuotaSettings.QuotaUnitPrice / (decimal)QuotaSettings.QuotaUnit;
                using (var DAClient = new DAE.DynamicAuthorizationClient(DACPort, 3000, DACAddress))
                {
                    while (true)
                    {
                        if (_isAborted)
                        {
                            logger.Trace("Aborted!");
                            return false;
                        }
                        try
                        {
                            // fetch record
                            var currentAuthRecord = searchQuery.Where(s => s.SubscriptionID > currentId).FirstOrDefault();
                            if (currentAuthRecord == null)
                            {
                                logger.Trace("No more records found. returning");
                                return true;
                            }
                            currentId = currentAuthRecord.SubscriptionID;
                            // server from cache
                            CachedNAS nas = null;
                            if (!string.IsNullOrEmpty(currentAuthRecord.NASIP))
                            {
                                IPAddress currentNASIP;
                                if (IPAddress.TryParse(currentAuthRecord.NASIP, out currentNASIP))
                                {
                                    nas = DAServers.GetCachedNAS(currentNASIP);
                                }
                            }
                            // get quota usage
                            var usage = currentAuthRecord.Subscription.GetQuotaAndUsageInfo();
                            // quota expired
                            if (usage.PeriodQuota < usage.PeriodUsage)
                            {
                                // quota expired SMS
                                {
                                    var lastSMSDate = currentAuthRecord.Subscription.RadiusSMS.FirstOrDefault(rs => rs.SMSTypeID == (short)SMSType.SmartQuota100)?.Date;
                                    if (!lastSMSDate.HasValue || lastSMSDate < usage.PeriodStart)
                                    {
                                        // send SMS
                                        try
                                        {
                                            var parameters = new Dictionary<string, object>()
                                                {
                                                    { SMSParamaterRepository.SMSParameterNameCollection.SmartQuotaUnit, QuotaSettings.QuotaUnit },
                                                    { SMSParamaterRepository.SMSParameterNameCollection.SmartQuotaUnitPrice, QuotaSettings.QuotaUnitPrice },
                                                    { SMSParamaterRepository.SMSParameterNameCollection.LastQuotaTotal, usage.LastQuotaAmount }
                                                };

                                            SendSMS(currentAuthRecord, dbLogger, SMSType.SmartQuota100, parameters);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Warn(ex, $"Error sending SMS for [{currentAuthRecord.Username}].");
                                        }
                                    }
                                }
                                // max smart quota price reached
                                if (currentAuthRecord.Subscription.Service.Price + ((usage.PeriodUsage - usage.PeriodQuota) * quotaPricePerByte) > currentAuthRecord.Subscription.Service.SmartQuotaMaxPrice)
                                {
                                    var lastSMSDate = currentAuthRecord.Subscription.RadiusSMS.FirstOrDefault(rs => rs.SMSTypeID == (short)SMSType.SmartQuotaMax)?.Date;
                                    if (!lastSMSDate.HasValue || lastSMSDate < usage.PeriodStart)
                                    {
                                        // send SMS
                                        try
                                        {
                                            SendSMS(currentAuthRecord, dbLogger, SMSType.SmartQuotaMax, new Dictionary<string, object>()
                                                {
                                                    { SMSParamaterRepository.SMSParameterNameCollection.SmartQuotaMaxPrice, currentAuthRecord.Subscription.Service.SmartQuotaMaxPrice.Value }
                                                });
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.Warn(ex, $"Error sending SMS for [{currentAuthRecord.Username}].");
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex);
                            return false;
                        }
                    }
                }
            }
        }
    }
}
