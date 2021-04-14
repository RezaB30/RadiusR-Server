using NLog;
using RezaB.Radius.PacketStructure;
using RezaB.Radius.Server.Caching;
using RezaB.Threading;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Caching;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RezaB.Radius.Server
{
    public abstract class RadiusServerBase
    {
        // loggers
        protected static Logger mainLogger = LogManager.GetLogger("server-main");
        protected static Logger processingLogger = LogManager.GetLogger("server-processor");
        protected static Logger dbLogger = LogManager.GetLogger("server-db");
        // internal variables
        protected bool isStopped = false;
        protected UdpClient _server;
        private CustomThreadPool<RawIncomingPacket> _workPool;
        private Thread listeningThread;
        protected MemoryCache identifierHistory = new MemoryCache("identifiers");
        private IEnumerable<MessageTypes> AcceptableMessageTypes { get; set; }

        protected string ThreadNamePrefix { get; set; }

        protected SettingsCache ServerCache { get; set; }

        public virtual void Start(RadiusServerSettings settings, IEnumerable<MessageTypes> acceptableMessageTypes)
        {
            AcceptableMessageTypes = acceptableMessageTypes.ToArray();
            isStopped = false;
            try
            {
                if (ServerCache == null)
                    ServerCache = new SettingsCache(settings.ConnectionString);
            }
            catch (Exception ex)
            {
                mainLogger.Fatal(ex, "Error loading settings cache.");
                return;
            }

            // initialize thread pool
            mainLogger.Trace("Initializing thread pool...");
            _workPool = new CustomThreadPool<RawIncomingPacket>(settings.ThreadCount, ProcessPacket, ThreadNamePrefix, (i => i.ToString("000")), 100, settings.ItemDiscardThreshold, settings.PoolCapacity, settings.ConnectionString);
            mainLogger.Trace(string.Format("{0} threads initialized.", settings.ThreadCount));

            // start listening
            if (!string.IsNullOrWhiteSpace(settings.ServerLocalIP))
                _server = new UdpClient(new IPEndPoint(IPAddress.Parse(settings.ServerLocalIP), settings.Port));
            else
                _server = new UdpClient(settings.Port);
            listeningThread = new Thread(new ThreadStart(Listen));
            listeningThread.Start();
        }

        public void Stop()
        {
            try
            {
                mainLogger.Trace("Stopping server...");
                isStopped = true;
                mainLogger.Trace("Closing port...");
                _server.Close();
                mainLogger.Trace("Port closed.");
                listeningThread.Join();
                mainLogger.Trace("Server stopped.");
            }
            catch (Exception ex)
            {
                mainLogger.Error(ex, "Error in stopping server.");
            }
        }

        protected void Listen()
        {
            mainLogger.Trace("Listening to port " + ((IPEndPoint)_server.Client.LocalEndPoint).Port);
            while (!isStopped)
            {
                try
                {
                    var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = _server.Receive(ref remoteEndpoint);
                    mainLogger.Trace("Received data from " + remoteEndpoint);

                    if (ServerCache.NASListCache.GetCachedNAS(remoteEndpoint.Address) != null)
                    {
                        if (!_workPool.TryAddWorkItem(new RawIncomingPacket()
                        {
                            Data = data,
                            EndPoint = remoteEndpoint
                        }))
                        {
                            mainLogger.Warn("Threadpool is full, Ignored!");
                        }
                    }
                    else
                    {
                        mainLogger.Trace("Message from " + remoteEndpoint.Address + " ignored!");
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is SocketException) || ((SocketException)ex).ErrorCode != 10004)
                    {
                        mainLogger.Error(ex, "Error processing data");
                        //Thread.Sleep(200);
                    }
                }

                //break;
            }

            mainLogger.Trace("Disposing thread pool threads...");
            _workPool.Dispose();
            mainLogger.Trace("Thread pool threads disposed.");
        }

        private void ProcessPacket(ConnectableItem<RawIncomingPacket> rawDataItem)
        {
            try
            {
                // find NAS
                var foundNAS = ServerCache.NASListCache.GetCachedNAS(rawDataItem.Item.EndPoint.Address);
                if (foundNAS == null)
                {
                    processingLogger.Info("Invalid NAS IP. Ignored!");
                    return;
                }
                // parse packet
                RadiusPacket packet = null;
                try
                {
                    packet = new RadiusPacket(rawDataItem.Item.Data);
                }
                catch (Exception ex)
                {
                    processingLogger.Warn(ex, "Error in processing packet.");
                    return;
                }

                processingLogger.Trace(packet.GetLog());

                // check identifiers to ignore duplicate messages
                {
                    var previousIdentifier = identifierHistory[rawDataItem.Item.EndPoint.ToString()] as string;
                    if (previousIdentifier == packet.Identifier.ToString())
                    {
                        processingLogger.Trace($"Same Identifier {packet.Identifier}... Ignored!");
                        return;
                    }
                    identifierHistory.Set(rawDataItem.Item.EndPoint.ToString(), packet.Identifier.ToString(), DateTime.UtcNow.AddSeconds(5));
                }

                // check message code
                if (!AcceptableMessageTypes.Contains(packet.Code))
                {
                    processingLogger.Trace("Invalid message code. Ignored!");
                    return;
                }

                // opening db connection if closed
                if (rawDataItem.DbConnection.State != System.Data.ConnectionState.Open)
                    rawDataItem.DbConnection.Open();

                // create response packet
                processingLogger.Trace("Creating response packet...");
                RadiusPacket responsePacket = null;
                try
                {
                    responsePacket = CreateResponse(rawDataItem.DbConnection, packet, foundNAS, ServerCache.ServerSettingsCache.GetSettings());
                    if (responsePacket == null)
                    {
                        processingLogger.Warn("Bad request. Ignored!");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    processingLogger.Warn(ex, "Error creating response packet.");
                    return;
                }

                // sending response
                var toSendBytes = responsePacket.GetBytes(foundNAS.Secret);
                processingLogger.Trace(responsePacket.GetLog());
                processingLogger.Trace($"Sending response to {rawDataItem.Item.EndPoint} ...");
                try
                {
                    _server.Send(toSendBytes, toSendBytes.Length, rawDataItem.Item.EndPoint);
                }
                catch (Exception ex)
                {
                    processingLogger.Warn(ex, "Error sending response packet.");
                    return;
                }
                processingLogger.Trace("Response sent.");
            }
            catch (Exception ex)
            {
                processingLogger.Error(ex, "General error.");
                return;
            }
        }

        protected abstract RadiusPacket CreateResponse(DbConnection connection, RadiusPacket packet, CachedNAS cachedNAS, CachedServerDefaults cachedServerDefaults);
    }
}
