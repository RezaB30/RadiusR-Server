using RezaB.Threading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RezaB.Radius.PacketStructure;
using System.Data.Common;
using RadiusR.DB;
using System.Data.Entity;

namespace RezaB.Radius.Server.Implementations
{
    public class AuthenticationServer : RadiusServerBase
    {
        public AuthenticationServer() : base()
        {
            ThreadNamePrefix = "AUTH";
        }

        //protected override void ProcessPacket(ConnectableItem<RawIncomingPacket> rawDataItem)
        //{
        //    try
        //    {
        //        // find NAS
        //        var foundNAS = ServerCache.NASListCache.GetCachedNAS(rawDataItem.Item.EndPoint.Address);
        //        if (foundNAS == null)
        //        {
        //            processingLogger.Info("Invalid NAS IP. Ignored!");
        //            return;
        //        }
        //        // parse packet
        //        RadiusPacket packet = null;
        //        try
        //        {
        //            packet = new RadiusPacket(rawDataItem.Item.Data);
        //        }
        //        catch (Exception ex)
        //        {
        //            processingLogger.Warn(ex, "Error in processing packet.");
        //            return;
        //        }

        //        processingLogger.Trace(packet.GetLog());

        //        // check identifiers to ignore duplicate messages
        //        {
        //            var previousIdentifier = identifierHistory[rawDataItem.Item.EndPoint.ToString()] as string;
        //            if (previousIdentifier == packet.Identifier.ToString())
        //            {
        //                processingLogger.Trace($"Same Identifier {packet.Identifier}... Ignored!");
        //                return;
        //            }
        //            identifierHistory.Set(rawDataItem.Item.EndPoint.ToString(), packet.Identifier.ToString(), DateTime.UtcNow.AddSeconds(5));
        //        }

        //        // check message code
        //        if (packet.Code != MessageTypes.AccessRequest)
        //        {
        //            processingLogger.Trace("Invalid message code. Ignored!");
        //            return;
        //        }

        //        // create response packet
        //        processingLogger.Trace("Creating response packet...");
        //        RadiusPacket responsePacket = null;
        //        try
        //        {
        //            responsePacket = CreateResponse(rawDataItem.DbConnection, packet, foundNAS.Secret);
        //            if (responsePacket == null)
        //            {
        //                processingLogger.Warn("Bad request. Ignored!");
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            processingLogger.Warn(ex, "Error creating response packet.");
        //            return;
        //        }

        //        // sending response
        //        processingLogger.Trace(responsePacket.GetLog());
        //        var toSendBytes = responsePacket.GetBytes(foundNAS.Secret);
        //        processingLogger.Trace($"Sending response to {rawDataItem.Item.EndPoint} ...");
        //        try
        //        {
        //            _server.Send(toSendBytes, toSendBytes.Length, rawDataItem.Item.EndPoint);
        //        }
        //        catch (Exception ex)
        //        {
        //            processingLogger.Warn(ex, "Error sending response packet.");
        //            return;
        //        }
        //        processingLogger.Trace("Response sent.");
        //    }
        //    catch (Exception ex)
        //    {
        //        processingLogger.Error(ex, "General error.");
        //        return;
        //    }
        //}

        protected override RadiusPacket CreateResponse(DbConnection connection, RadiusPacket packet, string secret)
        {
            RadiusPacket responsePacket = new RadiusPacket(packet);
            using (RadiusREntities db = new RadiusREntities(connection))
            {
                db.Database.Log = dbLogger.Trace;
                // check user password
                processingLogger.Trace("Checking password.");
                var username = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName).Value;
                var dbSubscription = FindSubscriber(db, username);
                if (dbSubscription == null)
                {
                    responsePacket.Code = MessageTypes.AccessReject;
                    return;
                }
                if (!HasValidPassword(dbSubscription, _nasClientCredentials.Secret))
                {
                    responsePacket.AccessReject();
                    return;
                }
            }

                throw new NotImplementedException();
        }

        private Subscription FindSubscriber(RadiusREntities db, string username)
        {
            if (db != null)
            {
                return db.Subscriptions.Where(s => s.Username == username).Include(s => s.Service.ServiceRateTimeTables).Include(s => s.RadiusSMS).FirstOrDefault();
            }
            else
            {
                using (db = new RadiusREntities())
                {
                    db.Database.Log = dbLogger.Trace;
                    db.Configuration.AutoDetectChangesEnabled = false;
                    return db.Subscriptions.Where(s => s.Username == username).Include(s => s.Service.ServiceRateTimeTables).Include(s => s.RadiusSMS).FirstOrDefault();
                }
            }
        }
    }
}
