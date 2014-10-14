namespace PacketParser.Packets
{
    using System;
    using System.Net;

    public interface IIPPacket : IPacket
    {
        IPAddress DestinationIPAddress { get; }

        byte HopLimit { get; }

        int PayloadLength { get; }

        IPAddress SourceIPAddress { get; }
    }
}

