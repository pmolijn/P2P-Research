namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class NetBiosDatagramServicePacket : NetBiosPacket
    {
        private ushort datagramID;
        private ushort datagramLength;
        private string destinationName;
        private Flags flags;
        private byte messageType;
        private ushort packetOffset;
        private uint sourceIP;
        private string sourceName;
        private ushort sourcePort;

        internal NetBiosDatagramServicePacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "NetBIOS Datagram Service")
        {
            this.messageType = parentFrame.Data[packetStartIndex];
            this.flags = new Flags(parentFrame.Data[packetStartIndex + 1]);
            this.datagramID = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
            this.sourceIP = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 4);
            this.sourcePort = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 8);
            if (((this.messageType == 0x10) || (this.messageType == 0x11)) || (this.messageType == 0x12))
            {
                this.datagramLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 10);
                this.packetOffset = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 12);
                int frameIndex = packetStartIndex + 14;
                this.sourceName = NetBiosPacket.DecodeNetBiosName(parentFrame, ref frameIndex);
                this.destinationName = NetBiosPacket.DecodeNetBiosName(parentFrame, ref frameIndex);
            }
            else if ((this.messageType != 0x13) && (((this.messageType == 20) || (this.messageType == 0x15)) || (this.messageType == 0x16)))
            {
                int num2 = packetStartIndex + 10;
                this.destinationName = NetBiosPacket.DecodeNetBiosName(parentFrame, ref num2);
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        internal string SourceNetBiosName
        {
            get
            {
                return this.sourceName;
            }
        }


        internal class Flags
        {
            private byte flagData;

            internal Flags(byte value)
            {
                this.flagData = value;
            }

            internal bool MoreDatagramFragmentsFollow
            {
                get
                {
                    return ((this.flagData & 1) == 1);
                }
            }

            internal SourceEndNodeTypeEnum SourceEndNodeType
            {
                get
                {
                    return (SourceEndNodeTypeEnum) ((byte) ((this.flagData >> 2) & 3));
                }
            }

            internal bool ThisIsFirstFragment
            {
                get
                {
                    return ((this.flagData & 2) == 2);
                }
            }

            internal enum SourceEndNodeTypeEnum : byte
            {
                B = 0,
                M = 2,
                NBDD = 3,
                P = 1
            }
        }

        internal enum MessageType : byte
        {
            BroadcastDatagram = 0x12,
            DatagramError = 0x13,
            DatagramNegativeQueryResponse = 0x16,
            DatagramPositiveQueryResponse = 0x15,
            DatagramQueryRequest = 20,
            DirectGroupDatagram = 0x11,
            DirectUniqueDatagram = 0x10
        }
    }
}

