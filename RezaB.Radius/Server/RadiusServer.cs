using NLog;
using RadiusR.DB;
using RezaB.Radius.Packet;
using RezaB.Radius.Server.Caching;
using RezaB.Threading;
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
    public abstract class RadiusServer
    {
        private static Logger logger = LogManager.GetLogger("main");
        protected static Logger consoleLogger = LogManager.GetLogger("console");

        protected bool IsStopped = false;

        public static RadiusRCacheManager ServerCache { get; private set; }
        protected UdpClient _server;
        private Thread listeningThread;
        protected static NASListCache NasList = new NASListCache();

        protected string ThreadNamePrefix { get; set; }
        private CustomThreadPool<RawRadiusPacket> _workPool;

        protected MemoryCache identifierHistory = new MemoryCache("identifiers");

        public virtual void Start(RadiusServerSettings serverSettings)
        {
            IsStopped = false;
            // load settings
            if (ServerCache == null)
                ServerCache = new RadiusRCacheManager();
            if (!ServerCache.IsPreLoaded)
            {
                logger.Fatal("Error loading server cache...shutting down.");
                return;
            }
            // initialize thread pool
            consoleLogger.Trace("Initializing thread pool...");
            _workPool = new CustomThreadPool<RawRadiusPacket>(serverSettings.ThreadCount, ProcessPacket, ThreadNamePrefix, (i => i.ToString("000")), 100, serverSettings.ItemDiscardThreshold, serverSettings.PoolCapacity, serverSettings.ConnectionString);
            consoleLogger.Trace(string.Format("{0} threads initialized.", serverSettings.ThreadCount));
            // start listening
            if (!string.IsNullOrWhiteSpace(serverSettings.ServerLocalIP))
                _server = new UdpClient(new IPEndPoint(IPAddress.Parse(serverSettings.ServerLocalIP), serverSettings.Port));
            else
                _server = new UdpClient(serverSettings.Port);
            listeningThread = new Thread(new ThreadStart(Listen));
            listeningThread.Start();
        }

        public void Stop()
        {
            try
            {
                logger.Trace("Stopping server...");
                IsStopped = true;
                logger.Trace("Closing port...");
                _server.Close();
                logger.Trace("Port closed.");
                listeningThread.Join();
                logger.Trace("Server stopped.");
            }
            catch (Exception ex)
            {
                LogException(ex, "Error in stopping server.");
            }
        }

        protected void Listen()
        {
            Thread.CurrentThread.Name = ThreadNamePrefix + "_Main";
            //ThreadPool.SetMinThreads(64, 64);
            consoleLogger.Trace("Listening to port " + ((IPEndPoint)_server.Client.LocalEndPoint).Port);
            while (!IsStopped)
            {
                try
                {
                    var remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
                    var data = _server.Receive(ref remoteEndpoint);
                    consoleLogger.Trace("Received data from " + remoteEndpoint);

                    if (NasList.Get(remoteEndpoint.Address) != null)
                    {
                        if (!_workPool.TryAddWorkItem(new RawRadiusPacket()
                        {
                            Data = data,
                            EndPoint = remoteEndpoint,
                            ProcessingOptions = new PacketProcessingOptions()
                            {
                                ShouldSetICMP = ServerCache.ServerSettings.GetSettings().IncludeICMP
                            }
                        }))
                        {
                            consoleLogger.Warn("Threadpool is full, Ignored!");
                        }
                    }
                    else
                    {
                        consoleLogger.Trace("Message from " + remoteEndpoint.Address + " ignored!");
                    }
                }
                catch (Exception ex)
                {
                    if (!(ex is SocketException) || ((SocketException)ex).ErrorCode != 10004)
                    {
                        consoleLogger.Error(ex, "Error processing data");
                        //Thread.Sleep(200);
                    }
                }

                //break;
            }

            logger.Trace("Disposing thread pool threads...");
            _workPool.Dispose();
            logger.Trace("Thread pool threads disposed.");
        }

        protected abstract void ProcessPacket(ConnectableItem<RawRadiusPacket> rawDataItem);

        public static void LogException(Exception ex, string message)
        {
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }
            StringBuilder logText = new StringBuilder(message);
            logText.AppendLine("EXEPTION: " + ex.Message);
            consoleLogger.Trace(logText);
            logger.Error(ex, message);
        }
    }
}
