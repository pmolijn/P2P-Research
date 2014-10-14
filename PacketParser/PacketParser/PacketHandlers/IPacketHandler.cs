using PacketParser.Packets;
using System;
using System.Collections.Generic;

namespace PacketParser.PacketHandlers
{

    internal interface IPacketHandler
    {
        void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList);
        void Reset();
    }
}

