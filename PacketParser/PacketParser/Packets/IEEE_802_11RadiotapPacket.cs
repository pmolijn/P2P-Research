namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class IEEE_802_11RadiotapPacket : AbstractPacket
    {
        private BitVector32 fieldsPresentFlags;
        private ushort frequency;
        private ushort radiotapHeaderLength;
        private int signalStrength;

        internal IEEE_802_11RadiotapPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "IEEE 802.11 Radiotap")
        {
            this.radiotapHeaderLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2, true);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Header length", this.radiotapHeaderLength.ToString());
            }
            try
            {
                this.fieldsPresentFlags = new BitVector32((int) ByteConverter.ToUInt32(parentFrame.Data, 4, 4, true));
                int startIndex = packetStartIndex + 8;
                for (int i = 0; i < 8; i++)
                {
                    if (this.fieldsPresentFlags[((int) 1) << i])
                    {
                        if (i == 0)
                        {
                            startIndex += 8;
                        }
                        else if (i == 1)
                        {
                            startIndex++;
                        }
                        else if (i == 2)
                        {
                            startIndex++;
                        }
                        else if (i == 3)
                        {
                            this.frequency = ByteConverter.ToUInt16(parentFrame.Data, startIndex, true);
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add("Frequency", this.frequency + " MHz");
                            }
                            startIndex += 4;
                        }
                        else if (i == 4)
                        {
                            startIndex += 2;
                        }
                        else if (i == 5)
                        {
                            this.signalStrength = parentFrame.Data[startIndex];
                            while (this.signalStrength > 70)
                            {
                                this.signalStrength -= 0x100;
                            }
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add("Signal strength", string.Concat(new object[] { this.signalStrength, " dBm (", Math.Pow(10.0, ((double) this.signalStrength) / 10.0), " mW)" }));
                            }
                            startIndex++;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + this.radiotapHeaderLength) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0;
                try
                {
                    iteratorVariable0 = new IEEE_802_11Packet(this.ParentFrame, this.PacketStartIndex + this.radiotapHeaderLength, this.PacketEndIndex);
                }
                catch (Exception)
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.radiotapHeaderLength, this.PacketEndIndex);
                }
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

        public ushort Frequency
        {
            get
            {
                return this.frequency;
            }
        }

        public int SignalStrength
        {
            get
            {
                return this.signalStrength;
            }
        }

    }
}

