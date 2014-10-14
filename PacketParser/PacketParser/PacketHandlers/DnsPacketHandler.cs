namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    internal class DnsPacketHandler : AbstractPacketHandler, IPacketHandler
    {
        public DnsPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            DnsPacket packet = null;
            IPv4Packet ipPakcet = null;
            UdpPacket udpPacket = null;
            foreach (AbstractPacket packet4 in packetList)
            {
                if (packet4.GetType() == typeof(DnsPacket))
                {
                    packet = (DnsPacket) packet4;
                }
                else if (packet4.GetType() == typeof(IPv4Packet))
                {
                    ipPakcet = (IPv4Packet) packet4;
                }
                else if (packet4.GetType() == typeof(UdpPacket))
                {
                    udpPacket = (UdpPacket) packet4;
                }
            }
            if (packet != null)
            {
                if (packet.Flags.Response)
                {
                    NameValueCollection values = new NameValueCollection();
                    if (packet.AnswerRecords != null)
                    {
                        foreach (DnsPacket.ResourceRecord record in packet.AnswerRecords)
                        {
                            if (record.IP != null)
                            {
                                if (!base.MainPacketHandler.NetworkHostList.ContainsIP(record.IP))
                                {
                                    NetworkHost host = new NetworkHost(record.IP);
                                    host.AddHostName(record.DNS);
                                    base.MainPacketHandler.NetworkHostList.Add(host);
                                    base.MainPacketHandler.OnNetworkHostDetected(new NetworkHostEventArgs(host));
                                }
                                else
                                {
                                    base.MainPacketHandler.NetworkHostList.GetNetworkHost(record.IP).AddHostName(record.DNS);
                                }
                                if (values[record.DNS] != null)
                                {
                                    base.MainPacketHandler.NetworkHostList.GetNetworkHost(record.IP).AddHostName(values[record.DNS]);
                                }
                            }
                            else if (record.Type == 5)
                            {
                                values.Add(record.PrimaryName, record.DNS);
                            }
                            if (ipPakcet != null)
                            {
                                base.MainPacketHandler.OnDnsRecordDetected(new DnsRecordEventArgs(record, sourceHost, destinationHost, ipPakcet, udpPacket));
                            }
                        }
                    }
                }
                else if ((packet.QueriedDnsName != null) && (packet.QueriedDnsName.Length > 0))
                {
                    sourceHost.AddQueriedDnsName(packet.QueriedDnsName);
                }
            }
        }

        public void Reset()
        {
        }
    }
}

