﻿using RezaB.Radius.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius.Packet
{
    public abstract class RadiusClientPacket : RadiusPacket
    {
        public NasClientCredentials NasClientCredentials
        {
            get
            {
                return _nasClientCredentials;
            }
        }

        public byte UniqueId
        {
            get
            {
                return Counter.Next();
            }
        }

        public RadiusClientPacket(RadiusPacket basePacket, IEnumerable<RadiusAttribute> additionalAttributes, NasClientCredentials credentials)
        {
            // initializing request
            _nasClientCredentials = credentials;
            //Code = MessageTypes.CoARequest;
            Identifier = UniqueId;
            var emptyBytes = new byte[16];
            emptyBytes.AsParallel().ForAll(b => b = 0);
            RequestAuthenticator = emptyBytes;
            // adding attributes
            Attributes = new List<RadiusAttribute>();
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.UserName));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.NASIPAddress));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.AcctSessionId));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.FramedIPAddress));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.NASPortType));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.NASPort));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.CalledStationId));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.CallingStationId));
            Attributes.Add(basePacket.Attributes.FirstOrDefault(acct => acct.Type == AttributeType.NASPortId));
            Attributes.AddRange(additionalAttributes ?? Enumerable.Empty<RadiusAttribute>());

            Attributes.RemoveAll(attr => attr == null);
        }
    }
}
