using NLog;
using RadiusR.DB;
using RadiusR.DB.Enums;
using RadiusR.DB.ModelExtentions;
using RadiusR.SMS;
using RezaB.Radius.Server.Caching;
using RezaB.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.DAEHelper.Tasks.DATasks
{
    public abstract class DynamicAccountingTask : AbortableTask
    {
        //protected DAE.DynamicAuthorizationClient DAEClient { get; set; }
        protected int DACPort { get; set; }
        protected IPAddress DACAddress { get; set; }
        protected NASesCache DAServers { get; set; }

        protected DynamicAccountingTask() { }

        public DynamicAccountingTask(NASesCache servers, int port, string IP = null)
        {
            DACPort = port;
            DACAddress = !string.IsNullOrWhiteSpace(IP) ? IPAddress.Parse(IP) : null;
            //DAEClient = !string.IsNullOrWhiteSpace(IP) ? new DAE.DynamicAuthorizationClient(port, 3000, IPAddress.Parse(IP)) : new DAE.DynamicAuthorizationClient(port, 3000);
            DAServers = servers;
        }

        protected void SendSMS(RadiusAuthorization currentAuthRecord, Logger dbLogger, SMSType smsType, IDictionary<string, object> smsParameters = null)
        {
            SMSService smsService = new SMSService();
            var sentSMS = smsService.SendSubscriberSMS(currentAuthRecord.Subscription, smsType, smsParameters);
            using (RadiusREntities smsDb = new RadiusREntities())
            {
                smsDb.Database.Log = dbLogger.Trace;
                smsDb.SMSArchives.AddSafely(sentSMS);
                var currentRadiusSMS = smsDb.RadiusSMS.FirstOrDefault(rs => rs.SubscriptionID == currentAuthRecord.SubscriptionID && rs.SMSTypeID == (short)smsType);
                if (currentRadiusSMS != null)
                {
                    currentRadiusSMS.Date = DateTime.Now;
                }
                else
                {
                    smsDb.RadiusSMS.Add(new RadiusSMS()
                    {
                        Date = DateTime.Now,
                        SMSTypeID = (short)smsType,
                        SubscriptionID = currentAuthRecord.SubscriptionID
                    });
                }
                smsDb.SaveChanges();
            }
        }
    }
}
