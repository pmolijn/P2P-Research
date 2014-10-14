namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class IPv6Packet : AbstractPacket, IIPPacket, IPacket
    {
        private IPAddress destinationIP;
        private byte hopLimit;
        private byte nextHeader;
        private ushort payloadLength;
        private IPAddress sourceIP;

        internal IPv6Packet(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "IPv6")
        {
            if (!base.ParentFrame.QuickParse && ((parentFrame.Data[packetStartIndex] >> 4) != 6))
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex, packetStartIndex, "IP Version!=6 (" + (parentFrame.Data[packetStartIndex] >> 4) + ")"));
            }
            this.payloadLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 4);
            this.nextHeader = parentFrame.Data[base.PacketStartIndex + 6];
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Next Header", "0x" + this.nextHeader.ToString("X2"));
            }
            this.hopLimit = parentFrame.Data[base.PacketStartIndex + 7];
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Hop Limit", this.hopLimit.ToString());
            }
            byte[] destinationArray = new byte[0x10];
            Array.Copy(parentFrame.Data, packetStartIndex + 8, destinationArray, 0, destinationArray.Length);
            this.sourceIP = new IPAddress(destinationArray);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Source IP", this.sourceIP.ToString());
            }
            byte[] buffer2 = new byte[0x10];
            Array.Copy(parentFrame.Data, packetStartIndex + 0x18, buffer2, 0, buffer2.Length);
            this.destinationIP = new IPAddress(buffer2);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Destination IP", this.destinationIP.ToString());
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 40) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0;
                try
                {
                    if (this.nextHeader == 6)
                    {
                        iteratorVariable0 = new TcpPacket(this.ParentFrame, this.PacketStartIndex + 40, this.PacketEndIndex);
                    }
                    else if (this.nextHeader == 0x11)
                    {
                        iteratorVariable0 = new UdpPacket(this.ParentFrame, this.PacketStartIndex + 40, this.PacketEndIndex);
                    }
                    else if (this.nextHeader == 0x2f)
                    {
                        iteratorVariable0 = new GrePacket(this.ParentFrame, this.PacketStartIndex + 40, this.PacketEndIndex);
                    }
                    else
                    {
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 40, this.PacketEndIndex);
                    }
                }
                catch (Exception)
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 40, this.PacketEndIndex);
                }
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

        public IPAddress DestinationIPAddress
        {
            get
            {
                return this.destinationIP;
            }
        }

        public byte HopLimit
        {
            get
            {
                return this.hopLimit;
            }
        }

        public int PayloadLength
        {
            get
            {
                return this.payloadLength;
            }
        }

        public IPAddress SourceIPAddress
        {
            get
            {
                return this.sourceIP;
            }
        }

    }
}

