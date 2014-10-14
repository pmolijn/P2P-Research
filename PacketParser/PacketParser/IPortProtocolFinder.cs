namespace PacketParser
{
    using System;

    public interface IPortProtocolFinder
    {
        ApplicationLayerProtocol GetApplicationLayerProtocol(TransportLayerProtocol transport, ushort sourcePort, ushort destinationPort);
    }
}

