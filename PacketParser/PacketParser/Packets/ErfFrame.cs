namespace PacketParser.Packets
{
    using PacketParser;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class ErfFrame : AbstractPacket
    {
        private bool extensionHeadersPresent;
        private byte type;

        internal ErfFrame(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "ERF")
        {
            this.type = (byte) (parentFrame.Data[packetStartIndex + 8] & 0x7f);
            this.extensionHeadersPresent = (parentFrame.Data[packetStartIndex + 8] & 0x80) == 0x80;
            if (!base.ParentFrame.QuickParse)
            {
                if (Enum.IsDefined(typeof(RecordTypes), this.type))
                {
                    base.Attributes.Add("Type", ((RecordTypes) this.type).ToString().Substring(9));
                }
                else
                {
                    base.Attributes.Add("Type", this.type.ToString());
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
            int iteratorVariable1 = 0x10;
            if (this.extensionHeadersPresent)
            {
                iteratorVariable1 += 4;
            }
            if ((this.PacketStartIndex + 0x10) >= this.PacketEndIndex)
            {
                goto Label_02ED;
            }
            if (((this.type == 2) || (this.type == 11)) || (this.type == 0x10))
            {
                iteratorVariable0 = new Ethernet2Packet(this.ParentFrame, (this.PacketStartIndex + iteratorVariable1) + 2, this.PacketEndIndex);
            }
            else if (this.type == 0x16)
            {
                iteratorVariable0 = new IPv4Packet(this.ParentFrame, this.PacketStartIndex + iteratorVariable1, this.PacketEndIndex);
            }
            else if (this.type == 0x17)
            {
                iteratorVariable0 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + iteratorVariable1, this.PacketEndIndex);
            }
            else if (((this.type == 1) || (this.type == 10)) || ((this.type == 15) || (this.type == 0x11)))
            {
                switch (this.ParentFrame.Data[this.PacketStartIndex])
                {
                    case 15:
                    case 0x8f:
                        iteratorVariable0 = new CiscoHdlcPacket(this.ParentFrame, this.PacketStartIndex + iteratorVariable1, this.PacketEndIndex);
                        goto Label_0262;
                }
                iteratorVariable0 = new PointToPointPacket(this.ParentFrame, this.PacketStartIndex + iteratorVariable1, this.PacketEndIndex);
            }
        Label_0262:
            if (iteratorVariable0 != null)
            {
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable2 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable2;
                }
            }
        Label_02ED:
            yield break;
        }


        public enum RecordTypes : byte
        {
            ERF_TYPE_AAL2 = 0x12,
            ERF_TYPE_AAL5 = 4,
            ERF_TYPE_ATM = 3,
            ERF_TYPE_COLOR_ETH = 11,
            ERF_TYPE_COLOR_HDLC_POS = 10,
            ERF_TYPE_COLOR_MC_HDLC_POS = 0x11,
            ERF_TYPE_DSM_COLOR_ETH = 0x10,
            ERF_TYPE_DSM_COLOR_HDLC_POS = 15,
            ERF_TYPE_ETH = 2,
            ERF_TYPE_HDLC_POS = 1,
            ERF_TYPE_INFINIBAND = 0x15,
            ERF_TYPE_INFINIBAND_LINK = 0x19,
            ERF_TYPE_IP_COUNTER = 13,
            ERF_TYPE_IPV4 = 0x16,
            ERF_TYPE_IPV6 = 0x17,
            ERF_TYPE_LEGACY = 0,
            ERF_TYPE_MC_AAL2 = 12,
            ERF_TYPE_MC_AAL5 = 9,
            ERF_TYPE_MC_ATM = 7,
            ERF_TYPE_MC_HDLC = 5,
            ERF_TYPE_MC_RAW = 6,
            ERF_TYPE_MC_RAW_CHANNEL = 8,
            ERF_TYPE_RAW_LINK = 0x18,
            ERF_TYPE_TCP_FLOW_COUNTER = 14
        }
    }
}

