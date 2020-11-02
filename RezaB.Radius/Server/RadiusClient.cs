using NLog;
using RezaB.Radius.Caching;
using RezaB.Radius.Packet;
using RezaB.Radius.Vendors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server
{
    public class RadiusClient
    {
        private static Logger consoleLogger = LogManager.GetLogger("console");

        private UdpClient _radiusClient;
        private NasClientCredentials _nasCredentials;

        public RadiusClient(NasClientCredentials credentials, int timeout = 1500)
        {
            _radiusClient = new UdpClient();
            _radiusClient.Client.ReceiveTimeout = _radiusClient.Client.SendTimeout = timeout;
            _nasCredentials = credentials;
        }

        //public void SendDisconnect(RadiusPacket accountingRequest)
        //{
        //    DisconnectPacket disconnectPacket = null;
        //    try
        //    {
        //        disconnectPacket = new DisconnectPacket(accountingRequest, _nasCredentials);
        //    }
        //    catch (Exception ex)
        //    {
        //        RadiusServer.LogException(ex, "Error creating disconnect packet.");
        //        return;
        //    }
        //    Thread sendDisconnectProcess = new Thread(new ThreadStart(() => _sendPacket(disconnectPacket)));
        //    sendDisconnectProcess.Name = "Disconnect";
        //    sendDisconnectProcess.Start();
        //}

        //public void SendCoa(RadiusPacket accountingRequest, RadiusAttribute rateLimitAttribute)
        //{
        //    CoaPacket coaPacket = null;
        //    try
        //    {
        //        coaPacket = new CoaPacket(accountingRequest, rateLimitAttribute, _nasCredentials);
        //    }
        //    catch (Exception ex)
        //    {
        //        RadiusServer.LogException(ex, "Error creating coa packet.");
        //        return;
        //    }
        //    Thread sendCoaProcess = new Thread(new ThreadStart(() => _sendPacket(coaPacket)));
        //    sendCoaProcess.Name = "COA";
        //    sendCoaProcess.Start();
        //}

        public void SendPacket(RadiusClientPacket packet)
        {
            try
            {
                var toSendBytes = packet.GetBytes(_nasCredentials.Secret);
                consoleLogger.Trace("Sending to " + _nasCredentials.NasEndpoint.ToString() + Environment.NewLine + packet.GetLog());
                // send request
                try
                {
                    _radiusClient.Send(toSendBytes, toSendBytes.Length, _nasCredentials.NasEndpoint);
                }
                catch (Exception ex)
                {
                    RadiusServer.LogException(ex, "Error sending request to nas.");
                    return;
                }
                // receive data
                var server = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = null;
                try
                {
                    data = _radiusClient.Receive(ref server);
                }
                catch (Exception ex)
                {
                    RadiusServer.LogException(ex, "Did not receive any data from nas.");
                    return;
                }
                // process response
                RadiusPacket receivedPacket = null;
                try
                {
                    receivedPacket = new RadiusPacket(data, _nasCredentials);
                }
                catch (Exception ex)
                {
                    RadiusServer.LogException(ex, "Error reading nas response.");
                    return;
                }
                consoleLogger.Trace("Received response from " + server.ToString() + Environment.NewLine + receivedPacket.GetLog());
                //try
                //{
                //    var usernameAttribute = packet.Attributes.FirstOrDefault(attr => attr.Type == AttributeType.UserName);
                //    var rateLimitAttribute = packet.Attributes.Where(attr => attr is MikrotikAttribute).Select(attr => attr as MikrotikAttribute).FirstOrDefault(attr => attr.VendorType == MikrotikAttribute.Attributes.MikrotikRateLimit);
                //    if (usernameAttribute != null && rateLimitAttribute != null)
                //    {
                //        ClientTransferRateCache.SetClientRateLimit(usernameAttribute.Value, rateLimitAttribute.Value);
                //    }
                //}
                //catch (Exception ex)
                //{
                //    RadiusServer.LogException(ex, "Error setting client transfer rate cache");
                //    return;
                //}
            }
            catch (Exception ex)
            {
                consoleLogger.Error(ex, "General error in Radius Client.");
            }
        }
    }
}
