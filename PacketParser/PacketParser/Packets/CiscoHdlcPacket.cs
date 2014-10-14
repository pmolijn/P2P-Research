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

    public class CiscoHdlcPacket : AbstractPacket
    {
        private ushort? protocolCode;

        internal CiscoHdlcPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Cisco HDLC")
        {
            if (((packetStartIndex + 4) <= parentFrame.Data.Length) && ((packetStartIndex + 3) <= packetEndIndex))
            {
                this.protocolCode = new ushort?(ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2));
            }
            else
            {
                this.protocolCode = null;
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 4) < this.PacketEndIndex)
            {
                ushort? protocolCode = this.protocolCode;
                int? nullable3 = protocolCode.HasValue ? new int?(protocolCode.GetValueOrDefault()) : null;
                if (nullable3.HasValue)
                {
                    AbstractPacket iteratorVariable0;
                    if (this.protocolCode.Value == 0x800)
                    {
                        iteratorVariable0 = new IPv4Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                    }
                    else if (this.protocolCode.Value == 0x86dd)
                    {
                        iteratorVariable0 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                    }
                    else if (this.protocolCode.Value == 0x806)
                    {
                        iteratorVariable0 = new ArpPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                    }
                    else if (this.protocolCode.Value == 0x8100)
                    {
                        iteratorVariable0 = new IEEE_802_1Q_VlanPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                    }
                    else if (this.protocolCode.Value == 0x8864)
                    {
                        iteratorVariable0 = new PointToPointOverEthernetPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                    }
                    else
                    {
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
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
}

