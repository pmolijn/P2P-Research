namespace PacketParser
{
    using System;

    public interface ISessionProtocolFinderFactory
    {
        ISessionProtocolFinder CreateProtocolFinder(NetworkHost client, NetworkHost server, ushort clientPort, ushort serverPort, bool tcpTransport, int startFrameNumber, DateTime startTimestamp);
    }
}

