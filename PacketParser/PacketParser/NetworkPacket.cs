namespace PacketParser
{
    using PacketParser.Packets;
    using System;

    public class NetworkPacket
    {
        private int cleartextBytes;
        private NetworkHost destinationHost;
        private ushort? destinationTcpPort;
        private ushort? destinationUdpPort;
        private int packetBytes;
        private int payloadBytes;
        private NetworkHost sourceHost;
        private ushort? sourceTcpPort;
        private ushort? sourceUdpPort;
        private int tcpPacketByteCount = 0;
        private bool tcpSynAckFlag = false;
        private bool tcpSynFlag = false;
        private DateTime timestamp;

        internal NetworkPacket(NetworkHost sourceHost, NetworkHost destinationHost, AbstractPacket ipPacket)
        {
            this.sourceHost = sourceHost;
            this.destinationHost = destinationHost;
            this.packetBytes = (ipPacket.PacketEndIndex - ipPacket.PacketStartIndex) + 1;
            this.timestamp = ipPacket.ParentFrame.Timestamp;
            this.payloadBytes = 0;
            this.cleartextBytes = 0;
        }

        internal void SetPayload(int payloadBytes, int cleartextBytes)
        {
            this.payloadBytes = payloadBytes;
            this.cleartextBytes = cleartextBytes;
        }

        internal void SetTcpData(TcpPacket tcpPacket)
        {
            this.sourceTcpPort = new ushort?(tcpPacket.SourcePort);
            this.destinationTcpPort = new ushort?(tcpPacket.DestinationPort);
            this.tcpPacketByteCount = tcpPacket.PacketByteCount;
            if (tcpPacket.FlagBits.Synchronize)
            {
                this.tcpSynFlag = true;
                if (tcpPacket.FlagBits.Acknowledgement)
                {
                    this.tcpSynAckFlag = true;
                    if (!this.sourceHost.TcpPortIsOpen(tcpPacket.SourcePort))
                    {
                        this.sourceHost.AddOpenTcpPort(tcpPacket.SourcePort);
                    }
                }
            }
        }

        internal void SetUdpData(UdpPacket udpPacket)
        {
            this.sourceUdpPort = new ushort?(udpPacket.SourcePort);
            this.destinationUdpPort = new ushort?(udpPacket.DestinationPort);
        }

        internal int CleartextBytes
        {
            get
            {
                return this.cleartextBytes;
            }
        }

        internal NetworkHost DestinationHost
        {
            get
            {
                return this.destinationHost;
            }
        }

        internal ushort? DestinationTcpPort
        {
            get
            {
                return this.destinationTcpPort;
            }
        }

        internal ushort? DestinationUdpPort
        {
            get
            {
                return this.destinationUdpPort;
            }
        }

        internal int PacketBytes
        {
            get
            {
                return this.packetBytes;
            }
        }

        internal int PayloadBytes
        {
            get
            {
                return this.payloadBytes;
            }
        }

        internal NetworkHost SourceHost
        {
            get
            {
                return this.sourceHost;
            }
        }

        internal ushort? SourceTcpPort
        {
            get
            {
                return this.sourceTcpPort;
            }
        }

        internal ushort? SourceUdpPort
        {
            get
            {
                return this.sourceUdpPort;
            }
        }

        internal int TcpPacketByteCount
        {
            get
            {
                return this.tcpPacketByteCount;
            }
        }

        internal bool TcpSynAckFlag
        {
            get
            {
                return this.tcpSynAckFlag;
            }
        }

        internal bool TcpSynFlag
        {
            get
            {
                return this.tcpSynFlag;
            }
        }

        internal DateTime Timestamp
        {
            get
            {
                return this.timestamp;
            }
        }
    }
}

