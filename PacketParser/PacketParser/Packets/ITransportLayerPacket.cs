namespace PacketParser.Packets
{
    using System;

    public interface ITransportLayerPacket : IPacket
    {
        byte DataOffsetByteCount { get; }

        ushort DestinationPort { get; }

        byte FlagsRaw { get; }

        ushort SourcePort { get; }
    }
}

