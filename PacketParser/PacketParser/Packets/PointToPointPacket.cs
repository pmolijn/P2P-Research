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

    public class PointToPointPacket : AbstractPacket
    {
        private const byte ALL_STATIONS_ADDRESS = 0xff;
        private const ushort IP_PROTOCOL_ID = 0x21;
        private ushort protocol;
        private int protocolStartOffset;
        private const byte UNNUMBERED_INFORMATION_COMMAND = 3;

        public PointToPointPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Point-to-Point Protocol (PPP)")
        {
            this.protocolStartOffset = 4;
            if (parentFrame.Data[packetStartIndex] == 0xff)
            {
                if ((packetStartIndex + 3) > packetEndIndex)
                {
                    throw new Exception("Too short PPP header");
                }
                byte num = parentFrame.Data[packetStartIndex];
                byte num2 = parentFrame.Data[packetStartIndex + 1];
                if ((num != 0xff) || (num2 != 3))
                {
                    throw new Exception("Invalid PPP HDLC framing");
                }
                this.protocol = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2, false);
                this.protocolStartOffset = 4;
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Encapsulated protocol", "0x" + this.protocol.ToString("X4"));
                }
            }
            else
            {
                if ((packetStartIndex + 1) > packetEndIndex)
                {
                    throw new Exception("Too short PPP header");
                }
                this.protocol = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex, false);
                this.protocolStartOffset = 2;
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + this.protocolStartOffset) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0;
                if (this.protocol == 0x21)
                {
                    iteratorVariable0 = new IPv4Packet(this.ParentFrame, this.PacketStartIndex + this.protocolStartOffset, this.PacketEndIndex);
                }
                else
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.protocolStartOffset, this.PacketEndIndex);
                }
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

    }
}

