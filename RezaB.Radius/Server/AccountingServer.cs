using RezaB.Radius.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using RezaB.Radius.Packet;
using RezaB.Threading;
using RadiusR.DB;
using RadiusR.DB.ModelExtentions;
using RadiusR.SMS;

namespace RezaB.Radius.Server
{
    public class AccountingServer : RadiusServer
    {
        private CustomThreadPool<RadiusPacket> _COAThreadPool;
        public AccountingServer()
        {
            ThreadNamePrefix = "Acc";
        }

        public override void Start(RadiusServerSettings serverSettings)
        {
            base.Start(serverSettings);
            consoleLogger.Trace("Initializing COA thread pool...");
            _COAThreadPool = new CustomThreadPool<RadiusPacket>(serverSettings.COAThreadCount ?? 32, SendCOAPacket, "COA", (i => i.ToString("000")), 100, 60000, serverSettings.COAPoolCapacity ?? 3000, serverSettings.ConnectionString);
            consoleLogger.Trace(string.Format("{0} threads initialized.", serverSettings.COAThreadCount));
        }

        protected override void ProcessPacket(ConnectableItem<RawRadiusPacket> rawDataItem)
        {
            try
            {
                var nasCredentials = NasList.Get(rawDataItem.Item.EndPoint.Address);
                if (nasCredentials == null)
                {
                    consoleLogger.Trace("Invalid NAS IP. Ignored!");
                    return;
                }
                RadiusPacket packet = null;
                try
                {
                    packet = new RadiusPacket(rawDataItem.Item.Data, nasCredentials);
                }
                catch (Exception ex)
                {
                    LogException(ex, "Can not process packet.");
                    return;
                }

                consoleLogger.Trace(packet.GetLog());

                {
                    var previousIdentifier = identifierHistory[rawDataItem.Item.EndPoint.ToString()] as string;
                    if (previousIdentifier == packet.Identifier.ToString())
                    {
                        consoleLogger.Trace(string.Format("Same Identifier {0}... Ignored!", packet.Identifier));
                        return;
                    }
                    identifierHistory.Set(rawDataItem.Item.EndPoint.ToString(), packet.Identifier.ToString(), DateTime.UtcNow.AddSeconds(5));
                }

                if (packet.Code != MessageTypes.AccountingRequest)
                {
                    consoleLogger.Trace("Invalid message code. Ignored!");
                    return;
                }

                consoleLogger.Trace("Preparing sending packet...");
                RadiusPacket responsePacket = null;
                try
                {
                    if (rawDataItem.DbConnection.State != System.Data.ConnectionState.Open)
                        rawDataItem.DbConnection.Open();
                    responsePacket = packet.GetResponse(rawDataItem.DbConnection, rawDataItem.Item.ProcessingOptions);
                }
                catch (Exception ex)
                {
                    LogException(ex, "Can not process response packet.");
                    return;
                }

                if (responsePacket == null)
                {
                    consoleLogger.Trace("Bad request. Ignored!");
                    return;
                }

                var toSendBytes = responsePacket.GetBytes(nasCredentials.Secret);

                consoleLogger.Trace(responsePacket.GetLog());

                consoleLogger.Trace("Sending response to " + rawDataItem.Item.EndPoint + " ...");
                // sending response
                try
                {
                    _server.Send(toSendBytes, toSendBytes.Length, rawDataItem.Item.EndPoint);
                }
                catch (Exception ex)
                {
                    LogException(ex, "Can not send packet.");
                    return;
                }
                consoleLogger.Trace("Response sent.");

                if (!AddToCOAQueue(packet))
                    consoleLogger.Trace("COA thread pool is full!");
                else
                    consoleLogger.Trace("Added packet to COA queue.");
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex, "General error");
            }
        }

        protected void SendCOAPacket(ConnectableItem<RadiusPacket> packetItem)
        {
            ChangeOfAccountingRequest COARequest = null;
            try
            {
                if (packetItem.DbConnection.State != System.Data.ConnectionState.Open)
                    packetItem.DbConnection.Open();
                
                COARequest = packetItem.Item.GetClientRequest(packetItem.DbConnection);
            }
            catch (Exception ex)
            {
                LogException(ex, "Error creating radius client package.");
                return;
            }
            if (COARequest != null)
            {
                try
                {
                    if(COARequest.Packet != null)
                    {
                        RadiusClient client = new RadiusClient(COARequest.Packet.NasClientCredentials);
                        client.SendPacket(COARequest.Packet);
                    }
                    try
                    {
                        // sending SMS
                        SendSMS(COARequest);
                    }
                    catch (Exception ex)
                    {
                        consoleLogger.Warn(ex, "Error sending state change SMS to client.");
                    }
                }
                catch (Exception ex)
                {
                    consoleLogger.Error(ex, "Error sending COA request.");
                }
            }
        }

        protected bool AddToCOAQueue(RadiusPacket clientPacket)
        {
            return _COAThreadPool.TryAddWorkItem(clientPacket);
        }

        protected void SendSMS(ChangeOfAccountingRequest COARequest)
        {
            if (COARequest.SMSType.HasValue && COARequest.SubscriptionState.SubscriberID.HasValue)
            {
                using (RadiusREntities db = new RadiusREntities())
                {
                    var SMSSubscriber = db.Subscriptions.PrepareForSMS().FirstOrDefault(subscription => subscription.ID == COARequest.SubscriptionState.SubscriberID.Value);
                    var serverCache = ServerCache.ServerSettings.GetSettings();
                    if (SMSSubscriber != null)
                    {
                        // prepare parameters
                        var additionalSMSparameters = new Dictionary<string, object>();
                        switch (COARequest.SMSType)
                        {
                            case RadiusR.DB.Enums.SMSType.SoftQuota100:
                                additionalSMSparameters.Add(SMSParamaterRepository.SMSParameterNameCollection.RateLimit, COARequest.SubscriptionState.RateLimit);
                                break;
                            case RadiusR.DB.Enums.SMSType.Quota80:
                                if (COARequest.SubscriptionState.LastQuotaAmount.HasValue)
                                    additionalSMSparameters.Add(SMSParamaterRepository.SMSParameterNameCollection.LastQuotaTotal, COARequest.SubscriptionState.LastQuotaAmount.Value);
                                break;
                            case RadiusR.DB.Enums.SMSType.SmartQuota100:
                                if (COARequest.SubscriptionState.LastQuotaAmount.HasValue)
                                    additionalSMSparameters.Add(SMSParamaterRepository.SMSParameterNameCollection.LastQuotaTotal, COARequest.SubscriptionState.LastQuotaAmount.Value);
                                additionalSMSparameters.Add(SMSParamaterRepository.SMSParameterNameCollection.SmartQuotaUnit, serverCache.QuotaUnit);
                                additionalSMSparameters.Add(SMSParamaterRepository.SMSParameterNameCollection.SmartQuotaUnitPrice, serverCache.QuotaUnitPrice);
                                break;
                            case RadiusR.DB.Enums.SMSType.SmartQuotaMax:
                                if (SMSSubscriber.Service.SmartQuotaMaxPrice.HasValue)
                                    additionalSMSparameters.Add(SMSParamaterRepository.SMSParameterNameCollection.SmartQuotaMaxPrice, SMSSubscriber.Service.SmartQuotaMaxPrice.Value);
                                break;
                            default:
                                break;
                        }
                        // send SMS
                        SMSService smsService = new SMSService();
                        var smsArchive = smsService.SendSubscriberSMS(SMSSubscriber, COARequest.SMSType, additionalSMSparameters);
                        // save SMS to database
                        db.SMSArchives.AddSafely(smsArchive);
                        var previousSMS = SMSSubscriber.RadiusSMS.FirstOrDefault(sms => sms.SMSTypeID == (short)COARequest.SMSType);
                        if (previousSMS != null)
                            previousSMS.Date = DateTime.Now;
                        else
                            db.RadiusSMS.Add(new RadiusSMS()
                            {
                                Date = DateTime.Now,
                                SMSTypeID = (short)COARequest.SMSType.Value,
                                SubscriptionID = SMSSubscriber.ID
                            });
                        db.SaveChanges();
                    }
                }
            }
        }
    }
}
