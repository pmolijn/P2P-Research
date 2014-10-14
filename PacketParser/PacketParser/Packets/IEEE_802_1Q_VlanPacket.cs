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

    public class IEEE_802_1Q_VlanPacket : AbstractPacket
    {
        private ushort etherType;
        private byte priorityTag;
        private ushort vlanID;

        internal IEEE_802_1Q_VlanPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "802.1Q VLAN")
        {
            this.priorityTag = (byte) (parentFrame.Data[packetEndIndex] >> 5);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Priority", this.priorityTag.ToString());
            }
            this.vlanID = (ushort) (ByteConverter.ToUInt16(parentFrame.Data, base.PacketStartIndex, false) & 0xfff);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("VLAN ID", this.vlanID.ToString());
            }
            this.etherType = ByteConverter.ToUInt16(parentFrame.Data, base.PacketStartIndex + 2, false);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 4) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0;
                if (this.etherType == 0x800)
                {
                    iteratorVariable0 = new IPv4Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                }
                else if (this.etherType == 0x86dd)
                {
                    iteratorVariable0 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                }
                else if (this.etherType == 0x806)
                {
                    iteratorVariable0 = new ArpPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                }
                else if (this.etherType == 0x8100)
                {
                    iteratorVariable0 = new IEEE_802_1Q_VlanPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                }
                else if (this.etherType == 0x8864)
                {
                    iteratorVariable0 = new PointToPointOverEthernetPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                }
                else if (this.etherType < 0x600)
                {
                    iteratorVariable0 = new LogicalLinkControlPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
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

