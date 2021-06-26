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
                    var responseReject = new RadiusPacket(packet, MessageTypes.AccessReject);
                    responseReject.Attributes.Add(new RadiusAttribute(AttributeType.ReplyMessage, "User not found."));
                    return responseReject;
                }
                // check active
                processingLogger.Trace("Checking if user is active.");
                if (!radiusUser.IsEnabled)
                {
                    var responseReject = new RadiusPacket(packet, MessageTypes.AccessReject);
                    responseReject.Attributes.Add(new RadiusAttribute(AttributeType.ReplyMessage, "User not active."));
                    return responseReject;
                }
                // check user password
                processingLogger.Trace("Checking password.");
                if (!packet.HasValidPassword(radiusUser.Password, cachedNAS.Secret))
                {
                    var responseReject = new RadiusPacket(packet, MessageTypes.AccessReject);
                    responseReject.Attributes.Add(new RadiusAttribute(AttributeType.ReplyMessage, "User password invalid."));
                    return responseReject;
                }
                // load settings
                var serverSettingsCache = ServerCache.ServerSettingsCache.GetSettings();
                // check CLID
                if (serverSettingsCache.CheckCLID)
                {
                    var CLID = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.CallingStationId);
                    if (CLID != null && !string.IsNullOrEmpty(radiusUser.CLID) && CLID.Value != radiusUser.CLID)
                    {
                        var responseReject = new RadiusPacket(packet, MessageTypes.AccessReject);
                        responseReject.Attributes.Add(new RadiusAttribute(AttributeType.ReplyMessage, "CLID invalid."));
                        return responseReject;
                    }
                }
                // check simultaneous connections
                processingLogger.Trace("Checking simultaneous use.");
                if (radiusUser.LastInterimUpdate.HasValue && (!radiusUser.LastLogout.HasValue || radiusUser.LastInterimUpdate > radiusUser.LastLogout))
                {
                    var responseReject = new RadiusPacket(packet, MessageTypes.AccessReject);
                    responseReject.Attributes.Add(new RadiusAttribute(AttributeType.ReplyMessage, "User currently online."));
                    return responseReject;
                }
                // initialize response packet
                RadiusPacket responsePacket = null;
                bool usesExpiredPool = false;
                // check expiration
                processingLogger.Trace("Checking expiration date.");
                if (!radiusUser.ExpirationDate.HasValue || radiusUser.ExpirationDate.Value < DateTime.Now || radiusUser.IsHardQuotaExpired == true)
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
                        var responseReject = new RadiusPacket(packet, MessageTypes.AccessReject);
                        responseReject.Attributes.Add(new RadiusAttribute(AttributeType.ReplyMessage, "User expired."));
                        return responseReject;
                    }
                }

                // checks passed so it is accepted
                processingLogger.Trace("All checks passed, preparing response.");
                responsePacket = responsePacket ?? new RadiusPacket(packet, MessageTypes.AccessAccept);

                // set profile pool if not expired
                if (!usesExpiredPool)
                {
                    var poolName = ServerCache.UserProfilesCache.GetCachedPoolName(cachedServerDefaults.DefaultProfile);

                    if (radiusUser.ProfileID.HasValue)
                        poolName = ServerCache.UserProfilesCache.GetCachedPoolName(radiusUser.ProfileID.Value);
                        
                    if (poolName != null)
                        responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedPool, poolName));
                }

                // set rate limit if available (currently only Mikrotik)
                if (!string.IsNullOrWhiteSpace(radiusUser.RateLimit))
                    responsePacket.Attributes.Add(new MikrotikAttribute(MikrotikAttribute.Attributes.MikrotikRateLimit, radiusUser.RateLimit));

                // set static IP if available and not expired
                if (!string.IsNullOrEmpty(radiusUser.StaticIP) && !usesExpiredPool)
                    responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedIPAddress, radiusUser.StaticIP));
                else
                    responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedIPAddress, "255.255.255.255"));

                // set default values
                responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.FramedProtocol, serverSettingsCache.FramedProtocol));
                responsePacket.Attributes.Add(new RadiusAttribute(AttributeType.AcctInterimInterval, serverSettingsCache.AccountingInterimInterval));

                // set last login
                radiusUser.LastInterimUpdate = DateTime.Now;
                radiusUser.NASIP = cachedNAS.NASIP.ToString();
                radiusUser.UsingExpiredPool = usesExpiredPool;
                db.SaveChanges();

                // return response
                return responsePacket;
            }
        }
    }
}
