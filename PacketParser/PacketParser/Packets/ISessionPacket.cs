namespace PacketParser.Packets
{
    using System;

    internal interface ISessionPacket
    {
        bool PacketHeaderIsComplete { get; }

        int ParsedBytesCount { get; }
    }
}

