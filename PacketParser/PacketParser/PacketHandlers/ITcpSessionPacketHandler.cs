namespace PacketParser.PacketHandlers
{
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal interface ITcpSessionPacketHandler
    {
        int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList);
        void Reset();

        ApplicationLayerProtocol HandledProtocol { get; }
    }
}

