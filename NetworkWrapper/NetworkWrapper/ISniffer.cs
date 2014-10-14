namespace NetworkWrapper
{
    using System;

    public interface ISniffer
    {
        void StartSniffing();
        void StopSniffing();

        PacketReceivedEventArgs.PacketTypes BasePacketType { get; }
    }
}

