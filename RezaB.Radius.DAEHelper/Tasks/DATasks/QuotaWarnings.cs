using NLog;
using RadiusR.DB;
using RezaB.Radius.Server.Caching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Net;
using RadiusR.SMS;
using RadiusR.DB.Enums;
using RadiusR.DB.ModelExtentions;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public class QuotaWarnings : DynamicAccountingTask
    {
        private static Logger logger = LogManager.GetLogger("quota-warnings");
        private static Logger dbLogger = LogManager.GetLogger("quota-warnings-DB");

        public QuotaWarnings(NASesCache servers, int port, string IP = null) : base(servers, port, IP) { }

        public override bool Run()
        {
            logger.Trace("Task started.");
            using (RadiusREntities db = new RadiusREntities())
            {
                // prepare query
                db.Database.Log = dbLogger.Trace;
                var searchQuery = db.RadiusAuthorizations.Include(s => s.Subscription.RadiusSMS).OrderBy(s => s.SubscriptionID).Where(s => s.IsEnabled && s.Subscription.Service.QuotaType.HasValue);
                long currentId = 0;
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
                            // if used more than %80
                            if (usage.LastQuotaUsage > Math.Floor(usage.LastQuotaAmount * 0.8m))
                            {
                                var lastSMSDate = currentAuthRecord.Subscription.RadiusSMS.FirstOrDefault(rs => rs.SMSTypeID == (short)SMSType.Quota80)?.Date;
                                if (!lastSMSDate.HasValue || lastSMSDate < usage.LastQuotaChangeDate)
                                {
                                    // send SMS
                                    try
                                    {
                                        SendSMS(currentAuthRecord, dbLogger, SMSType.Quota80, new Dictionary<string, object>()
                                        {
                                            { SMSParamaterRepository.SMSParameterNameCollection.LastQuotaTotal, usage.LastQuotaAmount }
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Warn(ex, $"Error sending SMS for [{currentAuthRecord.Username}].");
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
