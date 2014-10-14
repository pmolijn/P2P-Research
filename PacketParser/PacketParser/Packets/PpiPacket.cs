namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using pcapFileIO;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class PpiPacket : AbstractPacket
    {
        private pcapFrame.DataLinkTypeEnum dataLinkType;
        private ushort ppiLength;

        internal PpiPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "PPI")
        {
            this.ppiLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2, true);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Length", this.ppiLength.ToString());
            }
            uint num = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 4, 4, true);
            this.dataLinkType = (pcapFrame.DataLinkTypeEnum) num;
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Data Link Type", this.dataLinkType.ToString());
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + this.ppiLength) <= this.PacketEndIndex)
            {
                AbstractPacket packet = null;
                if (PacketFactory.TryGetPacket(out packet, this.dataLinkType, this.ParentFrame, this.PacketStartIndex + this.ppiLength, this.PacketEndIndex))
                {
                    yield return packet;
                }
                else
                {
                    yield return new RawPacket(this.ParentFrame, this.PacketStartIndex + this.ppiLength, this.PacketEndIndex);
                }
            }
        }

    }
}

