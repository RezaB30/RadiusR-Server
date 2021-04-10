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
using RezaB.Radius.PacketStructure.Vendors;
using RezaB.Radius.Server.Caching;

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

        protected override RadiusPacket CreateResponse(DbConnection connection, RadiusPacket packet, CachedNAS cachedNAS, CachedServerDefaults cachedServerDefaults)
        {
            using (RadiusREntities db = new RadiusREntities(connection))
            {
                db.Database.Log = dbLogger.Trace;
                // find user
                processingLogger.Trace("Finding user.");
                var username = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName).Value;
                var radiusUser = db.RadiusAuthorizations.FirstOrDefault(user => user.Username == username);
                if (radiusUser == null)
                {
                    return new RadiusPacket(packet, MessageTypes.AccessReject);
                }
                // check active
                processingLogger.Trace("Checking if user is active.");
                if (!radiusUser.IsEnabled)
                {
                    return new RadiusPacket(packet, MessageTypes.AccessReject);
                }
                // check user password
                processingLogger.Trace("Checking password.");
                if (!packet.HasValidPassword(radiusUser.Password, cachedNAS.Secret))
                {
                    return new RadiusPacket(packet, MessageTypes.AccessReject);
                }
                // check simultaneous connections
                processingLogger.Trace("Checking simultaneous use.");
                if (radiusUser.LastLogin.HasValue && (!radiusUser.LastLogout.HasValue || radiusUser.LastLogin < radiusUser.LastLogout))
                {
                    return new RadiusPacket(packet, MessageTypes.AccessReject);
                }
                // initialize response packet
                RadiusPacket responsePacket = null;
                bool usesExpiredPool = false;
                // check expiration
                processingLogger.Trace("Checking expiration date.");
                if (!radiusUser.ExpirationDate.HasValue || radiusUser.ExpirationDate.Value.Add(ServerCache.ServerSettingsCache.GetSettings().DailyDisconnectionTime) < DateTime.Now)
                {
                    // set expiration pool if available
                    if (cachedNAS.ExpiredPools?.Any() == true)
                    {
                        processingLogger.Trace("Found expiration pool");
                        var rnd = new Random();
                        var expiredPoolName = cachedNAS.ExpiredPools.ToArray()[rnd.Next(cachedNAS.ExpiredPools.Count())].PoolName;
                        responsePacket = new RadiusPacket(packet, MessageTypes.AccessAccept);
                        responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedPool, expiredPoolName));
                        usesExpiredPool = true;
                    }
                    // else reject
                    else
                    {
                        processingLogger.Trace("No expiration pool");
                        return new RadiusPacket(packet, MessageTypes.AccessReject);
                    }
                }


                // checks passed so it is accepted
                processingLogger.Trace("All checks passed, preparing response.");
                responsePacket = responsePacket ?? new RadiusPacket(packet, MessageTypes.AccessAccept);

                // set rate limit if available (currently only Mikrotik)
                if (!string.IsNullOrWhiteSpace(radiusUser.RateLimit))
                    responsePacket.Attributes.Add(new MikrotikAttribute(MikrotikAttribute.Attributes.MikrotikRateLimit, radiusUser.RateLimit));

                // set static IP if available and not expired
                if (!string.IsNullOrEmpty(radiusUser.StaticIP) && !usesExpiredPool)
                    responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedIPAddress, radiusUser.StaticIP));
                else
                    responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedIPAddress, "255.255.255.255"));

                // set default values
                responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedProtocol, ServerCache.ServerSettingsCache.GetSettings().FramedProtocol));
                responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.AcctInterimInterval, ServerCache.ServerSettingsCache.GetSettings().AccountingInterimInterval));

                // set last login
                radiusUser.LastLogin = DateTime.Now;
                db.SaveChanges();

                // return response
                return responsePacket;
            }
        }
    }
}
