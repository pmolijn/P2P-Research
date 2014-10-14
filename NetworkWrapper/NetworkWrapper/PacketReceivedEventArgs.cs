namespace NetworkWrapper
{
    using System;

    public class PacketReceivedEventArgs : EventArgs
    {
        private byte[] data;
        private PacketTypes packetType;
        private DateTime timestamp;

        public PacketReceivedEventArgs(byte[] data, DateTime timestamp, PacketTypes packetType)
        {
            this.data = data;
            this.timestamp = timestamp;
            this.packetType = packetType;
        }

        public byte[] Data
        {
            get
            {
                return this.data;
            }
        }

        public PacketTypes PacketType
        {
            get
            {
                return this.packetType;
            }
        }

        public DateTime Timestamp
        {
            get
            {
                return this.timestamp;
            }
        }

        public enum PacketTypes
        {
            NullLoopback,
            Ethernet2Packet,
            IPv4Packet,
            IPv6Packet,
            IEEE_802_11Packet,
            IEEE_802_11RadiotapPacket,
            CiscoHDLC,
            LinuxCookedCapture,
            PrismCaptureHeader
        }
    }
}

