using RezaB.Networking.IP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server
{
    public class NasClientCredentials
    {
        public IPEndPoint NasEndpoint { get; set; }

        public string Secret { get; set; }

        public int NasTypeID { get; set; }

        public int NATType { get; set; }

        public NasClientCredentials Backbone { get; set; }

        public IEnumerable<NasNetmap> Netmaps { get; set; }

        public IEnumerable<NASVerticalPool> VerticalPools { get; set; }

        public IEnumerable<NASExpiredPool> ExpiredPools { get; set; }

        public class NasNetmap
        {
            public string LocalIPSubnet { get; set; }

            public string RealIPSubnet { get; set; }

            public int PortCount { get; set; }

            public bool PreserveLastByte { get; set; }
        }

        public class NASVerticalPool
        {
            public string LocalIPStart { get; set; }

            public string LocalIPEnd { get; set; }

            public string RealIPStart { get; set; }

            public string RealIPEnd { get; set; }

            public int PortCount { get; set; }
        }

        public class NASExpiredPool
        {
            public string PoolName { get; set; }

            public string LocalIPSubnet { get; set; }
        }

        public class ClientIPInfo
        {
            public string LocalIP { get; set; }

            public string RealIP { get; set; }

            public string PortRange { get; set; }
        }

        public NasClientCredentials(RadiusR.DB.NAS nas)
        {
            NasEndpoint = new IPEndPoint(IPAddress.Parse(nas.IP), nas.RadiusIncomingPort);
            Secret = nas.Secret;
            NasTypeID = nas.TypeID;
            NATType = nas.NATType;
            Netmaps = nas.NATType == (short)Packet.NATType.Horizontal ? nas.NASNetmaps.Select(netmap => new NasNetmap()
            {
                LocalIPSubnet = netmap.LocalIPSubnet,
                RealIPSubnet = netmap.RealIPSubnet,
                PortCount = netmap.PortCount,
                PreserveLastByte = netmap.PreserveLastByte
            }) : null;
            VerticalPools = nas.NATType == (short)Packet.NATType.Vertical ? nas.NASVerticalIPMaps.Select(ipMap => new NASVerticalPool()
            {
                LocalIPStart = ipMap.LocalIPStart,
                LocalIPEnd = ipMap.LocalIPEnd,
                RealIPStart = ipMap.RealIPStart,
                RealIPEnd = ipMap.RealIPEnd,
                PortCount = ipMap.PortCount
            }) : null;
            Backbone = nas.BackboneNAS != null ? new NasClientCredentials(nas.BackboneNAS) : null;
            ExpiredPools = nas.NASExpiredPools.Select(pool => new NASExpiredPool()
            {
                PoolName = pool.PoolName,
                LocalIPSubnet = pool.LocalIPSubnet
            });
        }

        public IPPortInfo GetClientIPInfo(string localIP)
        {
            var reference = Backbone == null ? this : Backbone;
            switch (reference.NATType)
            {
                case (short)Packet.NATType.Horizontal:
                    var currentLocalIP = IPTools.GetUIntValue(localIP);
                    if (reference.Netmaps != null)
                        foreach (var netmap in reference.Netmaps)
                        {
                            var currentRuleSet = IPTools.CreateNetmapRulesFromCluster(netmap.LocalIPSubnet, netmap.RealIPSubnet, Convert.ToUInt16(netmap.PortCount), netmap.PreserveLastByte);
                            var validEntry = currentRuleSet.FindNetmapRealIP(localIP);
                            if (validEntry != null)
                            {
                                return validEntry;
                            }
                        }
                    return null;
                case (short)Packet.NATType.Vertical:
                    if (reference.VerticalPools != null)
                        foreach (var IPMap in reference.VerticalPools)
                        {
                            var currentRuleSet = IPTools.CreateVerticalNATRulesFromPools(new VerticalIPMapRule() { LocalIPStart = IPTools.GetUIntValue(IPMap.LocalIPStart), LocalIPEnd = IPTools.GetUIntValue(IPMap.LocalIPEnd), RealIPStart = IPTools.GetUIntValue(IPMap.RealIPStart), RealIPEnd = IPTools.GetUIntValue(IPMap.RealIPEnd), PortCount = Convert.ToUInt16(IPMap.PortCount) });
                            var validEntry = currentRuleSet.FirstOrDefault(rule => rule.LocalIP == localIP);
                            if (validEntry != null)
                            {
                                return validEntry;
                            }
                        }
                    return null;
                case (short)Packet.NATType.VerticalDSL:
                    return null;
                default:
                    return null;
            }
        }

        public bool IsInExpiredPool(string localIP)
        {
            return ExpiredPools != null && ExpiredPools.Any(pool => IPTools.IsIPInSubnet(IPTools.ParseIPSubnet(pool.LocalIPSubnet), localIP));
        }

        private NasClientCredentials() { }

        public NasClientCredentials Clone()
        {
            return new NasClientCredentials()
            {
                NasEndpoint = new IPEndPoint(NasEndpoint.Address, NasEndpoint.Port),
                NasTypeID = NasTypeID,
                NATType = NATType,
                Secret = Secret,
                Backbone = Backbone != null ? Backbone.Clone() : null,
                Netmaps = Netmaps != null ? Netmaps.ToArray().Select(n => new NasNetmap()
                {
                    LocalIPSubnet = n.LocalIPSubnet,
                    PortCount = n.PortCount,
                    PreserveLastByte = n.PreserveLastByte,
                    RealIPSubnet = n.RealIPSubnet
                }) : null,
                VerticalPools = VerticalPools != null ? VerticalPools.ToArray().Select(v => new NASVerticalPool()
                {
                    LocalIPEnd = v.LocalIPEnd,
                    LocalIPStart = v.LocalIPStart,
                    PortCount = v.PortCount,
                    RealIPEnd = v.RealIPEnd,
                    RealIPStart = v.RealIPStart
                }) : null,
                ExpiredPools = ExpiredPools != null ? ExpiredPools.ToArray().Select(e => new NASExpiredPool()
                {
                    PoolName = e.PoolName,
                    LocalIPSubnet = e.LocalIPSubnet
                }) : null
            };
        }
    }
}
