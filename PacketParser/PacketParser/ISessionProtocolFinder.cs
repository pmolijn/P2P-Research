namespace PacketParser
{
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    public interface ISessionProtocolFinder
    {
        void AddPacket(TcpPacket tcpPacket, NetworkHost source, NetworkHost destination);
        IEnumerable<ApplicationLayerProtocol> GetProbableApplicationLayerProtocols();

        NetworkHost Client { get; }

        ushort ClientPort { get; }

        ApplicationLayerProtocol ConfirmedApplicationLayerProtocol { get; set; }

        NetworkHost Server { get; }

        ushort ServerPort { get; }

        PacketParser.TransportLayerProtocol TransportLayerProtocol { get; }
    }
}

