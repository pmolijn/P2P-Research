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
    using System.Text;
    using System.Threading;

    public class Ethernet2Packet : AbstractPacket
    {
        private PhysicalAddress destinationMAC;
        private ushort etherType;
        private PhysicalAddress sourceMAC;

        internal Ethernet2Packet(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Ethernet2")
        {
            this.etherType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 12);
            byte[] destinationArray = new byte[6];
            Array.Copy(parentFrame.Data, packetStartIndex, destinationArray, 0, destinationArray.Length);
            this.destinationMAC = new PhysicalAddress(destinationArray);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Destination MAC", this.destinationMAC.ToString());
            }
            byte[] buffer2 = new byte[6];
            Array.Copy(parentFrame.Data, packetStartIndex + 6, buffer2, 0, buffer2.Length);
            this.sourceMAC = new PhysicalAddress(buffer2);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Source MAC", this.sourceMAC.ToString());
            }
        }

        private string ConvertToHexString(byte[] data)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < (data.Length - 1); i++)
            {
                builder.Append(data[i].ToString("X2") + "-");
            }
            builder.Append(data[data.Length - 1].ToString("X2"));
            return builder.ToString();
        }

        internal static AbstractPacket GetPacketForType(ushort etherType, Frame parentFrame, int newPacketStartIndex, int newPacketEndIndex)
        {
            try
            {
                if (etherType == 0x800)
                {
                    return new IPv4Packet(parentFrame, newPacketStartIndex, newPacketEndIndex);
                }
                if (etherType == 0x86dd)
                {
                    return new IPv6Packet(parentFrame, newPacketStartIndex, newPacketEndIndex);
                }
                if (etherType == 0x806)
                {
                    return new ArpPacket(parentFrame, newPacketStartIndex, newPacketEndIndex);
                }
                if (etherType == 0x8100)
                {
                    return new IEEE_802_1Q_VlanPacket(parentFrame, newPacketStartIndex, newPacketEndIndex);
                }
                if (etherType == 0x8864)
                {
                    return new PointToPointOverEthernetPacket(parentFrame, newPacketStartIndex, newPacketEndIndex);
                }
                if (etherType < 0x600)
                {
                    return new LogicalLinkControlPacket(parentFrame, newPacketStartIndex, newPacketEndIndex);
                }
                return new RawPacket(parentFrame, newPacketStartIndex, newPacketEndIndex);
            }
            catch (Exception)
            {
                return new RawPacket(parentFrame, newPacketStartIndex, newPacketEndIndex);
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 14) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0 = GetPacketForType(this.etherType, this.ParentFrame, this.PacketStartIndex + 14, this.PacketEndIndex);
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

        public PhysicalAddress DestinationMACAddress
        {
            get
            {
                return this.destinationMAC;
            }
        }

        public PhysicalAddress SourceMACAddress
        {
            get
            {
                return this.sourceMAC;
            }
        }


        internal enum EtherTypes : ushort
        {
            ARP = 0x806,
            HPSW = 0x623,
            IEEE802_1Q = 0x8100,
            IEEE802_3_Max = 0x600,
            IPv4 = 0x800,
            IPv6 = 0x86dd,
            PPPoE = 0x8864
        }
    }
}

