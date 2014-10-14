namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class NetBiosDatagramServicePacketHandler : AbstractPacketHandler, IPacketHandler
    {
        public NetBiosDatagramServicePacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        private void ExtractData(NetBiosDatagramServicePacket netBiosDatagramServicePacket, NetworkHost sourceHost)
        {
            if (((netBiosDatagramServicePacket != null) && (netBiosDatagramServicePacket.SourceNetBiosName != null)) && (netBiosDatagramServicePacket.SourceNetBiosName.Length > 0))
            {
                sourceHost.AddHostName(netBiosDatagramServicePacket.SourceNetBiosName);
            }
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(NetBiosDatagramServicePacket))
                {
                    this.ExtractData((NetBiosDatagramServicePacket) packet, sourceHost);
                }
            }
        }

        public void Reset()
        {
        }
    }
}

