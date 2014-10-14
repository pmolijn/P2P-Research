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

    public class PrismCaptureHeaderPacket : AbstractPacket
    {
        private uint channel;
        private string device;
        private uint messageLength;

        internal PrismCaptureHeaderPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Prism capture header")
        {
            this.messageLength = ByteConverter.ToUInt32(parentFrame.Data, base.PacketStartIndex + 4, 4, true);
            this.device = ByteConverter.ReadString(parentFrame.Data, (int) (base.PacketStartIndex + 8), 0x10, false, false);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Device", this.device);
            }
            this.channel = ByteConverter.ToUInt32(parentFrame.Data, base.PacketStartIndex + 0x38, 4, true);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Channel", this.channel.ToString());
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 0x90) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0;
                try
                {
                    iteratorVariable0 = new IEEE_802_11Packet(this.ParentFrame, this.PacketStartIndex + 0x90, this.PacketEndIndex);
                }
                catch (Exception)
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 0x90, this.PacketEndIndex);
                }
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

        public uint Channel
        {
            get
            {
                return this.channel;
            }
        }

        public string Device
        {
            get
            {
                return this.device;
            }
        }

        public uint MessageLength
        {
            get
            {
                return this.messageLength;
            }
        }

    }
}

