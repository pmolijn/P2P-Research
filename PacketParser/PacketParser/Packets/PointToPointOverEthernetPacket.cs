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

    internal class PointToPointOverEthernetPacket : AbstractPacket
    {
        private byte code;
        private const int PAYLOAD_OFFSET = 6;
        private ushort payloadLength;

        public PointToPointOverEthernetPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Point-to-point protocol over Ethernet (PPPoE)")
        {
            if (parentFrame.Data[packetStartIndex] != 0x11)
            {
                throw new Exception("Invalid PPPoE Version or Type");
            }
            this.code = parentFrame.Data[packetStartIndex + 1];
            if (this.code == 0)
            {
                this.payloadLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 4);
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.payloadLength > 0) && (this.code == 0))
            {
                int packetEndIndex = ((this.PacketStartIndex + 6) + this.payloadLength) - 1;
                if (packetEndIndex <= this.PacketEndIndex)
                {
                    PointToPointPacket iteratorVariable1 = new PointToPointPacket(this.ParentFrame, this.PacketStartIndex + 6, packetEndIndex);
                    yield return iteratorVariable1;
                    foreach (AbstractPacket iteratorVariable2 in iteratorVariable1.GetSubPackets(false))
                    {
                        yield return iteratorVariable2;
                    }
                }
            }
        }


        private enum CODE : byte
        {
            ActiveDiscoveryInitiation = 9,
            ActiveDiscoveryOffer = 7,
            ActiveDiscoveryRequest = 0x19,
            ActiveDiscoverySessionConfirmation = 0x65,
            ActiveDiscoveryTerminate = 0xa7,
            SessionData = 0
        }
    }
}

