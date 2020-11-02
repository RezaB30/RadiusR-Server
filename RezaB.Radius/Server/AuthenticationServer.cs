using RezaB.Radius.Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server
{
    public class AuthenticationServer : RadiusServer
    {
        public AuthenticationServer()
        {
            ThreadNamePrefix = "Auth";
        }

        protected override void ProcessPacket(RawRadiusPacket rawData)
        {
            try
            {
                var nasCredentials = NasList.Get(rawData.EndPoint.Address);
                if (nasCredentials == null)
                {
                    consoleLogger.Trace("Invalid NAS IP. Ignored!");
                    return;
                }
                RadiusPacket packet = null;
                try
                {
                    packet = new RadiusPacket(rawData.Data, nasCredentials);
                }
                catch (Exception ex)
                {
                    LogException(ex, "Can not process packet.");
                    return;
                }

                consoleLogger.Trace(packet.GetLog());

                {
                    var previousIdentifier = identifierHistory[rawData.EndPoint.ToString()] as string;
                    if (previousIdentifier == packet.Identifier.ToString())
                    {
                        consoleLogger.Trace(string.Format("Same Identifier {0}... Ignored!", packet.Identifier));
                        return;
                    }
                    identifierHistory.Set(rawData.EndPoint.ToString(), packet.Identifier.ToString(), DateTime.UtcNow.AddSeconds(5));
                }

                if (packet.Code != MessageTypes.AccessRequest)
                {
                    consoleLogger.Trace("Invalid message code. Ignored!");
                    return;
                }

                consoleLogger.Trace("Preparing sending packet...");
                RadiusPacket responsePacket = null;
                try
                {
                    responsePacket = packet.GetResponse(rawData.ProcessingOptions);
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

                consoleLogger.Trace("Sending response to " + rawData.EndPoint + " ...");
                // sending response
                try
                {
                    _server.Send(toSendBytes, toSendBytes.Length, rawData.EndPoint);
                }
                catch (Exception ex)
                {
                    LogException(ex, "Can not send packet.");
                    return;
                }
                consoleLogger.Trace("Response sent.");
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex, "General error");
            }
        }
    }
}
