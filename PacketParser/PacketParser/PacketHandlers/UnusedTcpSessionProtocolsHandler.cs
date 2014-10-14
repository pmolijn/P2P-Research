namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class UnusedTcpSessionProtocolsHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        private List<Type> unusedPacketTypes;

        public UnusedTcpSessionProtocolsHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.unusedPacketTypes = new List<Type>();
            this.unusedPacketTypes.Add(typeof(NetBiosDatagramServicePacket));
            this.unusedPacketTypes.Add(typeof(NetBiosNameServicePacket));
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (this.unusedPacketTypes.Contains(packet.GetType()))
                {
                    return packet.ParentFrame.Data.Length;
                }
            }
            return 0;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.Unknown;
            }
        }
    }
}

