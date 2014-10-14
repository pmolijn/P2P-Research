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

    public class GrePacket : AbstractPacket
    {
        private ushort etherType;
        private const int PACKET_LENGTH = 4;

        internal GrePacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "GRE")
        {
            this.etherType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2, false);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            AbstractPacket iteratorVariable0 = Ethernet2Packet.GetPacketForType(this.etherType, this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
            yield return iteratorVariable0;
            foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
            {
                yield return iteratorVariable1;
            }
        }

    }
}

