namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class NetBiosNameServicePacketHandler : AbstractPacketHandler, IPacketHandler
    {
        public NetBiosNameServicePacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        private void ExtractData(NetBiosNameServicePacket netBiosNameServicePacket, NetworkHost sourceHost)
        {
            if (netBiosNameServicePacket.QueriedNetBiosName != null)
            {
                sourceHost.AddQueriedNetBiosName(netBiosNameServicePacket.QueriedNetBiosName);
            }
            if ((netBiosNameServicePacket.AnsweredNetBiosName != null) && base.MainPacketHandler.NetworkHostList.ContainsIP(netBiosNameServicePacket.AnsweredIpAddress))
            {
                base.MainPacketHandler.NetworkHostList.GetNetworkHost(netBiosNameServicePacket.AnsweredIpAddress).AddHostName(netBiosNameServicePacket.AnsweredNetBiosName);
            }
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(NetBiosNameServicePacket))
                {
                    this.ExtractData((NetBiosNameServicePacket) packet, sourceHost);
                }
            }
        }

        public void Reset()
        {
        }
    }
}

