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

    public class LogicalLinkControlPacket : AbstractPacket
    {
        private byte control;
        private byte dsap;
        private ushort etherType;
        private uint organisationCode;
        private byte ssap;

        internal LogicalLinkControlPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Logical Link Control (LLC)")
        {
            this.dsap = parentFrame.Data[packetStartIndex];
            this.ssap = parentFrame.Data[packetStartIndex + 1];
            this.control = parentFrame.Data[packetStartIndex + 2];
            if (this.dsap == 170)
            {
                this.organisationCode = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 3) >> 8;
                this.etherType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("EtherType", "0x" + this.etherType.ToString("X2"));
                }
            }
            else if (this.dsap == 0xf8)
            {
                this.etherType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("EtherType", "0x" + this.etherType.ToString("X2"));
                }
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            AbstractPacket iteratorVariable0 = null;
            try
            {
                if (this.dsap == 170)
                {
                    if ((this.PacketStartIndex + 8) < this.PacketEndIndex)
                    {
                        if (this.etherType == 0x800)
                        {
                            iteratorVariable0 = new IPv4Packet(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                        }
                        else if (this.etherType == 0x806)
                        {
                            iteratorVariable0 = new ArpPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                        }
                        else
                        {
                            iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                        }
                    }
                }
                else if ((this.dsap == 0xf8) && (this.etherType == 0x623))
                {
                    if ((this.PacketStartIndex + 10) < this.PacketEndIndex)
                    {
                        iteratorVariable0 = new HpSwitchProtocolPacket(this.ParentFrame, (((this.PacketStartIndex + 3) + 3) + 2) + 2, this.PacketEndIndex);
                    }
                }
                else if ((this.PacketStartIndex + 3) < this.PacketEndIndex)
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 3, this.PacketEndIndex);
                }
            }
            catch (Exception)
            {
                iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 3, this.PacketEndIndex);
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


        public enum ServiceAccessPointType : byte
        {
            HpExtendedLLC = 0xf8,
            IbmNetBIOS = 240,
            ISONetworkLayerProtocol = 0xfe,
            NullLsap = 0,
            SpanningTree = 0x42,
            SubNetworkAccessProtocol = 170,
            X_25overIEEE802_2 = 0x7e
        }
    }
}

