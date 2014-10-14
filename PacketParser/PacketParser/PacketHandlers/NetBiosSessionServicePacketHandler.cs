namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class NetBiosSessionServicePacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public NetBiosSessionServicePacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            int num = 0;
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(NetBiosSessionService))
                {
                    num += ((NetBiosSessionService) packet).ParsedBytesCount;
                }
            }
            return num;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.NetBiosSessionService;
            }
        }
    }
}

