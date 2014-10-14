namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net.NetworkInformation;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class LinuxCookedCapture : AbstractPacket
    {
        private ushort addressLength;
        private ushort addressType;
        private ushort packetType;
        private ushort protocol;
        private PhysicalAddress sourceMacAddress;

        public LinuxCookedCapture(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Linux cooked capture (SLL)")
        {
            this.packetType = ByteConverter.ToUInt16(parentFrame.Data, base.PacketStartIndex);
            if (!base.ParentFrame.QuickParse)
            {
                if (this.packetType == 0)
                {
                    base.Attributes.Add("Packet Type", "Unicast to us (HOST)");
                }
                else if (this.packetType == 1)
                {
                    base.Attributes.Add("Packet Type", "Broadcast");
                }
                else if (this.packetType == 2)
                {
                    base.Attributes.Add("Packet Type", "Multicast");
                }
                else if (this.packetType == 3)
                {
                    base.Attributes.Add("Packet Type", "Unicast to another host (OTHERHOST)");
                }
                else if (this.packetType == 4)
                {
                    base.Attributes.Add("Packet Type", "Sent by us (OUTGOING)");
                }
            }
            this.addressType = ByteConverter.ToUInt16(parentFrame.Data, base.PacketStartIndex + 2);
            this.addressLength = ByteConverter.ToUInt16(parentFrame.Data, base.PacketStartIndex + 4);
            if (this.addressLength == 6)
            {
                byte[] destinationArray = new byte[6];
                Array.Copy(parentFrame.Data, packetStartIndex + 6, destinationArray, 0, destinationArray.Length);
                this.sourceMacAddress = new PhysicalAddress(destinationArray);
            }
            else
            {
                this.sourceMacAddress = PhysicalAddress.None;
            }
            this.protocol = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 14);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 0x10) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0;
                if (this.protocol == 0x800)
                {
                    iteratorVariable0 = new IPv4Packet(this.ParentFrame, this.PacketStartIndex + 0x10, this.PacketEndIndex);
                }
                else if (this.protocol == 0x86dd)
                {
                    iteratorVariable0 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + 0x10, this.PacketEndIndex);
                }
                else if (this.protocol == 0x806)
                {
                    iteratorVariable0 = new ArpPacket(this.ParentFrame, this.PacketStartIndex + 0x10, this.PacketEndIndex);
                }
                else if (this.protocol == 0x8100)
                {
                    iteratorVariable0 = new IEEE_802_1Q_VlanPacket(this.ParentFrame, this.PacketStartIndex + 0x10, this.PacketEndIndex);
                }
                else if (this.protocol == 0x8864)
                {
                    iteratorVariable0 = new PointToPointOverEthernetPacket(this.ParentFrame, this.PacketStartIndex + 0x10, this.PacketEndIndex);
                }
                else if (this.protocol < 0x600)
                {
                    iteratorVariable0 = new LogicalLinkControlPacket(this.ParentFrame, this.PacketStartIndex + 0x10, this.PacketEndIndex);
                }
                else
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 0x10, this.PacketEndIndex);
                }
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }


        internal enum PacketTypes : ushort
        {
            LINUX_SLL_BROADCAST = 1,
            LINUX_SLL_HOST = 0,
            LINUX_SLL_MULTICAST = 2,
            LINUX_SLL_OTHERHOST = 3,
            LINUX_SLL_OUTGOING = 4
        }
    }
}

