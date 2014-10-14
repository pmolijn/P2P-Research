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

    public class IEEE_802_11Packet : AbstractPacket
    {
        private PhysicalAddress basicServiceSetMAC;
        private int dataOffsetByteCount;
        private PhysicalAddress destinationMAC;
        private ushort duration;
        private byte fragmentNibble;
        private FrameControl frameControl;
        private PhysicalAddress recipientMAC;
        private ushort sequenceNumber;
        private PhysicalAddress sourceMAC;
        private PhysicalAddress transmitterMAC;

        internal IEEE_802_11Packet(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "IEEE 802.11")
        {
            this.frameControl = new FrameControl(parentFrame.Data[packetStartIndex], parentFrame.Data[packetStartIndex + 1]);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Protocol version", this.frameControl.ProtocolVersion.ToString());
                base.Attributes.Add("Type", this.frameControl.Type.ToString());
                base.Attributes.Add("SubType", this.frameControl.SubType.ToString());
                base.Attributes.Add("WEP", this.frameControl.WEP.ToString());
            }
            this.duration = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2, true);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Duration", this.duration.ToString());
            }
            int sourceIndex = base.PacketStartIndex + 4;
            byte[][] bufferArray = new byte[4][];
            for (int i = 0; i < 4; i++)
            {
                bufferArray[i] = new byte[6];
                if (i < 3)
                {
                    if (sourceIndex < (packetEndIndex - 4))
                    {
                        Array.Copy(parentFrame.Data, sourceIndex, bufferArray[i], 0, bufferArray[i].Length);
                        sourceIndex += 6;
                    }
                    else
                    {
                        bufferArray[i] = null;
                    }
                }
            }
            if ((this.frameControl.Type == 0) || (this.frameControl.Type == 2))
            {
                this.fragmentNibble = (byte) (parentFrame.Data[sourceIndex] & 15);
                this.sequenceNumber = (ushort) (ByteConverter.ToUInt16(parentFrame.Data, sourceIndex, true) >> 4);
                sourceIndex += 2;
                if (this.frameControl.FromDistributionSystem && this.frameControl.ToDistributionSystem)
                {
                    Array.Copy(parentFrame.Data, sourceIndex, bufferArray[3], 0, bufferArray[3].Length);
                    sourceIndex += 6;
                }
            }
            if ((this.frameControl.Type == 2) && (this.frameControl.SubType >= 8))
            {
                sourceIndex += 2;
            }
            this.dataOffsetByteCount = sourceIndex - base.PacketStartIndex;
            if ((this.frameControl.Type == 0) || (this.frameControl.Type == 2))
            {
                if (!this.frameControl.ToDistributionSystem && !this.frameControl.FromDistributionSystem)
                {
                    this.destinationMAC = new PhysicalAddress(bufferArray[0]);
                    this.sourceMAC = new PhysicalAddress(bufferArray[1]);
                    this.basicServiceSetMAC = new PhysicalAddress(bufferArray[2]);
                    this.recipientMAC = null;
                    this.transmitterMAC = null;
                }
                else if (!this.frameControl.ToDistributionSystem && this.frameControl.FromDistributionSystem)
                {
                    this.destinationMAC = new PhysicalAddress(bufferArray[0]);
                    this.basicServiceSetMAC = new PhysicalAddress(bufferArray[1]);
                    this.sourceMAC = new PhysicalAddress(bufferArray[2]);
                    this.recipientMAC = null;
                    this.transmitterMAC = null;
                }
                else if (this.frameControl.ToDistributionSystem && !this.frameControl.FromDistributionSystem)
                {
                    this.basicServiceSetMAC = new PhysicalAddress(bufferArray[0]);
                    this.sourceMAC = new PhysicalAddress(bufferArray[1]);
                    this.destinationMAC = new PhysicalAddress(bufferArray[2]);
                    this.recipientMAC = null;
                    this.transmitterMAC = null;
                }
                else if (this.frameControl.ToDistributionSystem && !this.frameControl.FromDistributionSystem)
                {
                    this.recipientMAC = new PhysicalAddress(bufferArray[0]);
                    this.transmitterMAC = new PhysicalAddress(bufferArray[1]);
                    this.destinationMAC = new PhysicalAddress(bufferArray[2]);
                    this.sourceMAC = new PhysicalAddress(bufferArray[3]);
                    this.basicServiceSetMAC = null;
                }
            }
            else if (this.frameControl.Type == 1)
            {
                if (bufferArray[0] != null)
                {
                    this.recipientMAC = new PhysicalAddress(bufferArray[0]);
                }
                else
                {
                    this.recipientMAC = null;
                }
                if (bufferArray[1] != null)
                {
                    this.transmitterMAC = new PhysicalAddress(bufferArray[1]);
                }
                else
                {
                    this.transmitterMAC = null;
                }
                this.destinationMAC = null;
                this.sourceMAC = null;
                this.basicServiceSetMAC = null;
            }
            if (!base.ParentFrame.QuickParse)
            {
                if (this.sourceMAC != null)
                {
                    base.Attributes.Add("Source MAC", this.sourceMAC.ToString());
                }
                if (this.destinationMAC != null)
                {
                    base.Attributes.Add("Destination MAC", this.destinationMAC.ToString());
                }
                if (this.transmitterMAC != null)
                {
                    base.Attributes.Add("Transmitter MAC", this.transmitterMAC.ToString());
                }
                if (this.recipientMAC != null)
                {
                    base.Attributes.Add("Recipient MAC", this.recipientMAC.ToString());
                }
                if (this.basicServiceSetMAC != null)
                {
                    base.Attributes.Add("BSSID", this.basicServiceSetMAC.ToString());
                }
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            int iteratorVariable0 = 0;
            if ((this.PacketStartIndex + this.dataOffsetByteCount) < (this.PacketEndIndex - iteratorVariable0))
            {
                AbstractPacket iteratorVariable1;
                try
                {
                    if (this.frameControl.Type == 2)
                    {
                        iteratorVariable1 = new LogicalLinkControlPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex - iteratorVariable0);
                    }
                    else if (this.frameControl.Type == 0)
                    {
                        iteratorVariable1 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex - iteratorVariable0);
                    }
                    else
                    {
                        iteratorVariable1 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex - iteratorVariable0);
                    }
                }
                catch (Exception)
                {
                    iteratorVariable1 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex - iteratorVariable0);
                }
                yield return iteratorVariable1;
                foreach (AbstractPacket iteratorVariable2 in iteratorVariable1.GetSubPackets(false))
                {
                    yield return iteratorVariable2;
                }
            }
        }

        public PhysicalAddress DestinationMAC
        {
            get
            {
                return this.destinationMAC;
            }
        }

        public PhysicalAddress SourceMAC
        {
            get
            {
                return this.sourceMAC;
            }
        }


        internal class FrameControl
        {
            private byte flagData;
            private byte versionTypeSubtype;

            internal FrameControl(byte firstByte, byte secondByte)
            {
                this.versionTypeSubtype = firstByte;
                this.flagData = secondByte;
            }

            internal bool FromDistributionSystem
            {
                get
                {
                    return ((this.flagData & 2) == 2);
                }
            }

            internal bool MoreData
            {
                get
                {
                    return ((this.flagData & 0x20) == 0x20);
                }
            }

            internal bool MoreFragmentFlag
            {
                get
                {
                    return ((this.flagData & 4) == 4);
                }
            }

            internal bool Order
            {
                get
                {
                    return ((this.flagData & 0x80) == 0x80);
                }
            }

            internal bool PowerManagement
            {
                get
                {
                    return ((this.flagData & 0x10) == 0x10);
                }
            }

            internal byte ProtocolVersion
            {
                get
                {
                    return (byte) (this.versionTypeSubtype & 3);
                }
            }

            internal bool Retry
            {
                get
                {
                    return ((this.flagData & 8) == 8);
                }
            }

            internal byte SubType
            {
                get
                {
                    return (byte) ((this.versionTypeSubtype & 240) >> 4);
                }
            }

            internal bool ToDistributionSystem
            {
                get
                {
                    return ((this.flagData & 1) == 1);
                }
            }

            internal byte Type
            {
                get
                {
                    return (byte) ((this.versionTypeSubtype & 12) >> 2);
                }
            }

            internal bool WEP
            {
                get
                {
                    return ((this.flagData & 0x40) == 0x40);
                }
            }
        }
    }
}

