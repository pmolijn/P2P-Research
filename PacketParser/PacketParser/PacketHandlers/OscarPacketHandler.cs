namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class OscarPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public OscarPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            OscarPacket packet = null;
            TcpPacket packet2 = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(OscarPacket))
                {
                    packet = (OscarPacket) packet3;
                }
                else if (packet3.GetType() == typeof(TcpPacket))
                {
                    packet2 = (TcpPacket) packet3;
                }
            }
            if ((packet == null) || (packet2 == null))
            {
                return 0;
            }
            if (packet.ImText != null)
            {
                base.MainPacketHandler.OnMessageDetected(new MessageEventArgs(ApplicationLayerProtocol.Oscar, sourceHost, destinationHost, packet.ParentFrame.FrameNumber, packet.ParentFrame.Timestamp, packet.SourceLoginId, packet.DestinationLoginId, packet.ImText, packet.ImText, packet.Attributes));
            }
            return packet.BytesParsed;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.Oscar;
            }
        }
    }
}

