namespace PacketParser.Fingerprints
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using PacketParser.Packets;

    internal interface ITtlDistanceCalculator
    {
        byte GetTtlDistance(byte ipTimeToLive);
        bool TryGetTtlDistance(out byte ttlDistance, IEnumerable<AbstractPacket> packetList);
    }
}

