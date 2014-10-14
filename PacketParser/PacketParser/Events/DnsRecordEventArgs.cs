namespace PacketParser.Events
{
    using PacketParser;
    using PacketParser.Packets;
    using System;

    public class DnsRecordEventArgs : EventArgs
    {
        public NetworkHost DnsClient;
        public NetworkHost DnsServer;
        public IPv4Packet IpPakcet;
        public DnsPacket.ResourceRecord Record;
        public PacketParser.Packets.UdpPacket UdpPacket;

        public DnsRecordEventArgs(DnsPacket.ResourceRecord record, NetworkHost dnsServer, NetworkHost dnsClient, IPv4Packet ipPakcet, PacketParser.Packets.UdpPacket udpPacket)
        {
            this.Record = record;
            this.DnsServer = dnsServer;
            this.DnsClient = dnsClient;
            this.IpPakcet = ipPakcet;
            this.UdpPacket = udpPacket;
        }
    }
}

