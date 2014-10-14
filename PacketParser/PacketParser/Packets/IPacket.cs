namespace PacketParser.Packets
{
    using PacketParser;
    using System;

    public interface IPacket
    {
        int PacketStartIndex { get; }

        Frame ParentFrame { get; }
    }
}

