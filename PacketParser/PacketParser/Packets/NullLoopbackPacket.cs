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

    public class NullLoopbackPacket : AbstractPacket
    {
        private const int PACKET_LENGTH = 4;
        private uint protocolFamily;

        internal NullLoopbackPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Null/Loopback")
        {
            this.protocolFamily = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex, 4, true);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            AbstractPacket iteratorVariable0 = null;
            if (this.protocolFamily == 2)
            {
                iteratorVariable0 = new IPv4Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
            }
            else if (this.protocolFamily == 0x18)
            {
                iteratorVariable0 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
            }
            else if (this.protocolFamily == 0x1c)
            {
                iteratorVariable0 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
            }
            else if (this.protocolFamily == 30)
            {
                iteratorVariable0 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
            }
            if (iteratorVariable0 != null)
            {
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }


        public enum ProtocolFamily : uint
        {
            AF_INET = 2,
            AF_INET6_FreeBSD = 0x1c,
            AF_INET6_OpenBSD = 0x18,
            AF_INET6_OSX = 30
        }
    }
}

