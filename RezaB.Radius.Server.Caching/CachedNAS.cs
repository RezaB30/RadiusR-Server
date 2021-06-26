using RezaB.Networking.IP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Server.Caching
{
    public class CachedNAS
    {
        public IPAddress NASIP { get; protected set; }

        public string Secret { get; protected set; }

        public int IncomingPort { get; protected set; }

        public int NasTypeID { get; protected set; }

        public int NATType { get; protected set; }

        public CachedNAS Backbone { get; protected set; }

        public IEnumerable<NasNetmap> Netmaps { get; protected set; }

        public IEnumerable<NASVerticalPool> VerticalPools { get; protected set; }

        public NASVerticalDSLPool VerticalDSLPool { get; protected set; }

        public IEnumerable<NASExpiredPool> ExpiredPools { get; protected set; }

        public class NasNetmap
        {
            public string LocalIPSubnet { get; protected set; }

            public string RealIPSubnet { get; protected set; }

            public int PortCount { get; protected set; }

            public bool PreserveLastByte { get; protected set; }

            public NasNetmap(string localIPSubnet, string realIPSubnet, int portCount, bool preserveLastByte)
            {
                LocalIPSubnet = localIPSubnet;
                RealIPSubnet = realIPSubnet;
                PortCount = portCount;
                PreserveLastByte = preserveLastByte;
            }
        }

        public class NASVerticalPool
        {
            public string LocalIPStart { get; protected set; }

            public string LocalIPEnd { get; protected set; }

            public string RealIPStart { get; protected set; }

            public string RealIPEnd { get; protected set; }

            public int PortCount { get; protected set; }

            public NASVerticalPool(string localIPStart, string localIPEnd, string realIPStart, string realIPEnd, int portCount)
            {
                LocalIPStart = localIPStart;
                LocalIPEnd = localIPEnd;
                RealIPStart = realIPStart;
                RealIPEnd = realIPEnd;
                PortCount = portCount;
            }
        }

        public class NASVerticalDSLPool
        {
            public IEnumerable<IPSubnet> LocalIPSubnets { get; set; }

            public IEnumerable<uint> DSLIPs { get; set; }

            public NASVerticalDSLPool(string localIPSubnets, string dslIPs)
            {
                LocalIPSubnets = !string.IsNullOrWhiteSpace(localIPSubnets) ? localIPSubnets.Split('|').Select(subnet => IPTools.ParseIPSubnet(subnet)).ToArray() : new IPSubnet[0];
                DSLIPs = !string.IsNullOrWhiteSpace(dslIPs) ? dslIPs.Split('|').Select(ip => IPTools.GetUIntValue(ip)) : new uint[0];
            }

            public NASVerticalDSLPool(IEnumerable<IPSubnet> localIPSubnets, IEnumerable<uint> dslIPs)
            {
                LocalIPSubnets = localIPSubnets;
                DSLIPs = dslIPs;
            }
        }

        public class NASExpiredPool
        {
            public string PoolName { get; set; }

            public string LocalIPSubnet { get; set; }
        }

        private CachedNAS() { }

        public CachedNAS(RadiusR.DB.NAS nas)
        {
            NASIP = IPAddress.Parse(nas.IP);
            Secret = nas.Secret;
            IncomingPort = nas.RadiusIncomingPort;
            NasTypeID = nas.TypeID;
            NATType = nas.NATType;
            Netmaps = nas.NATType == (short)NASNATTypes.Horizontal && nas.NASNetmaps.Any() ? nas.NASNetmaps.Select(netmap => new NasNetmap(netmap.LocalIPSubnet, netmap.RealIPSubnet, netmap.PortCount, netmap.PreserveLastByte)) : null;
            VerticalPools = nas.NATType == (short)NASNATTypes.Vertical && nas.NASVerticalIPMaps.Any() ? nas.NASVerticalIPMaps.Select(ipMap => new NASVerticalPool(ipMap.LocalIPStart, ipMap.LocalIPEnd, ipMap.RealIPStart, ipMap.RealIPEnd, ipMap.PortCount)) : null;
            VerticalDSLPool = nas.NATType == (short)NASNATTypes.VerticalDSL && nas.NASVerticalDSLIPMap != null ? new NASVerticalDSLPool(nas.NASVerticalDSLIPMap.LocalIPPools, nas.NASVerticalDSLIPMap.DSLLineIPs) : null;
            Backbone = nas.BackboneNAS != null ? new CachedNAS(nas.BackboneNAS) : null;
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
                case (short)NASNATTypes.Horizontal:
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
                case (short)NASNATTypes.Vertical:
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
                case (short)NASNATTypes.VerticalDSL:
                    if (reference.VerticalDSLPool != null)
                    {
                        var currentRuleSet = IPTools.CreateVerticalNATRulesFromDSLRoutingTable(new VerticalDSLRoutingTable() { LocalIPSubnets = reference.VerticalDSLPool.LocalIPSubnets, DSLIPs = reference.VerticalDSLPool.DSLIPs });
                        var validEntry = currentRuleSet.FirstOrDefault(rule => rule.LocalIP == localIP);
                        if (validEntry != null)
                        {
                            return validEntry;
                        }
                    }
                    return null;
                default:
                    return null;
            }
        }

        public bool IsInExpiredPool(string localIP)
        {
            return ExpiredPools != null && ExpiredPools.Any(pool => IPTools.IsIPInSubnet(IPTools.ParseIPSubnet(pool.LocalIPSubnet), localIP));
        }

        public CachedNAS Clone()
        {
            return new CachedNAS()
            {
                NASIP = new IPAddress(NASIP.GetAddressBytes()),
                IncomingPort = IncomingPort,
                NasTypeID = NasTypeID,
                NATType = NATType,
                Secret = Secret,
                Backbone = Backbone != null ? Backbone.Clone() : null,
                Netmaps = Netmaps != null ? Netmaps.ToArray().Select(n => new NasNetmap(n.LocalIPSubnet, n.RealIPSubnet, n.PortCount, n.PreserveLastByte)) : null,
                VerticalPools = VerticalPools != null ? VerticalPools.ToArray().Select(v => new NASVerticalPool(v.LocalIPStart, v.LocalIPEnd, v.RealIPStart, v.RealIPEnd, v.PortCount)) : null,
                VerticalDSLPool = VerticalDSLPool != null ? new NASVerticalDSLPool(VerticalDSLPool.LocalIPSubnets.Select(subnet => new IPSubnet() { MinBound = subnet.MinBound, Count = subnet.Count }).ToArray(), VerticalDSLPool.DSLIPs.ToArray()) : null,
                ExpiredPools = ExpiredPools?.ToArray().Select(e => new NASExpiredPool()
                {
                    PoolName = e.PoolName,
                    LocalIPSubnet = e.LocalIPSubnet
                })
            };
        }
    }
}
