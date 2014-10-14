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

    public class IPv4Packet : AbstractPacket, IIPPacket, IPacket
    {
        private IPAddress destinationIP;
        private bool dontFragmentFlag;
        private const ushort FRAGMENT_OFFSET_MASK = 0x1fff;
        private ushort fragmentOffset;
        private byte headerLength;
        private ushort identification;
        private bool moreFragmentsFlag;
        private byte protocol;
        private IPAddress sourceIP;
        private byte timeToLive;
        private ushort totalLength;

        internal IPv4Packet(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "IPv4")
        {
            if (((parentFrame.Data[packetStartIndex] >> 4) != 4) && !base.ParentFrame.QuickParse)
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex, packetStartIndex, "IP Version!=4 (" + (parentFrame.Data[packetStartIndex] >> 4) + ")"));
            }
            this.headerLength = (byte) (4 * (parentFrame.Data[packetStartIndex] & 15));
            if (!base.ParentFrame.QuickParse)
            {
                if (this.headerLength < 20)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex, packetStartIndex, "Too short defined IPv4 field HeaderLength"));
                }
                else if ((packetStartIndex + this.headerLength) > (packetEndIndex + 1))
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex, packetStartIndex, "Too long defined IPv4 field HeaderLength"));
                }
            }
            this.totalLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Total Length", this.totalLength.ToString());
            }
            if ((this.totalLength != ((packetEndIndex - packetStartIndex) + 1)) && (this.totalLength < ((packetEndIndex - packetStartIndex) + 1)))
            {
                base.PacketEndIndex = (packetStartIndex + this.totalLength) - 1;
            }
            this.identification = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 4, false);
            this.dontFragmentFlag = (parentFrame.Data[packetStartIndex + 6] & 0x40) == 0x40;
            this.moreFragmentsFlag = (parentFrame.Data[packetStartIndex + 6] & 0x20) == 0x20;
            this.fragmentOffset = (ushort) ((ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6, false) & 0x1fff) << 3);
            this.timeToLive = parentFrame.Data[packetStartIndex + 8];
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("TTL", this.timeToLive.ToString());
            }
            this.protocol = parentFrame.Data[packetStartIndex + 9];
            byte[] destinationArray = new byte[4];
            Array.Copy(parentFrame.Data, packetStartIndex + 12, destinationArray, 0, destinationArray.Length);
            this.sourceIP = new IPAddress(destinationArray);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Source IP", this.sourceIP.ToString());
            }
            byte[] buffer2 = new byte[4];
            Array.Copy(parentFrame.Data, packetStartIndex + 0x10, buffer2, 0, buffer2.Length);
            this.destinationIP = new IPAddress(buffer2);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Destination IP", this.destinationIP.ToString());
            }
        }

        internal string GetFragmentIdentifier()
        {
            return (this.sourceIP.ToString() + "\t" + this.destinationIP.ToString() + "\t" + this.identification.ToString());
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.fragmentOffset != 0) || this.moreFragmentsFlag)
            {
                if (!this.ParentFrame.QuickParse)
                {
                    byte[] destinationArray = null;
                    string fragmentIdentifier = this.GetFragmentIdentifier();
                    lock (PacketHandler.Ipv4Fragments)
                    {
                        List<IPv4Packet> list;
                        if (!PacketHandler.Ipv4Fragments.ContainsKey(fragmentIdentifier))
                        {
                            list = new List<IPv4Packet>();
                            PacketHandler.Ipv4Fragments.Add(fragmentIdentifier, list);
                        }
                        else
                        {
                            list = PacketHandler.Ipv4Fragments[fragmentIdentifier];
                        }
                        list.Add(this);
                        bool flag = true;
                        int num = 0;
                        foreach (IPv4Packet packet in list)
                        {
                            num += packet.PayloadLength;
                            if (!packet.moreFragmentsFlag)
                            {
                                flag = false;
                            }
                        }
                        if (!flag)
                        {
                            destinationArray = new byte[this.HeaderLength + num];
                            if (destinationArray.Length > 0xffff)
                            {
                                PacketHandler.Ipv4Fragments.Remove(fragmentIdentifier);
                                goto Label_061A;
                            }
                            foreach (IPv4Packet packet2 in list)
                            {
                                if (((packet2.fragmentOffset + this.HeaderLength) + packet2.PayloadLength) > destinationArray.Length)
                                {
                                    goto Label_061A;
                                }
                                Array.Copy(packet2.ParentFrame.Data, packet2.PacketStartIndex + packet2.HeaderLength, destinationArray, packet2.fragmentOffset + this.HeaderLength, packet2.PayloadLength);
                            }
                            PacketHandler.Ipv4Fragments.Remove(fragmentIdentifier);
                        }
                    }
                    if ((destinationArray != null) && (destinationArray.Length > this.HeaderLength))
                    {
                        Array.Copy(this.ParentFrame.Data, this.PacketStartIndex, destinationArray, 0, this.headerLength);
                        ByteConverter.ToByteArray((ushort) destinationArray.Length, destinationArray, 2);
                        destinationArray[6] = 0;
                        destinationArray[7] = 0;
                        Frame parentFrame = new Frame(this.ParentFrame.Timestamp, destinationArray, this.ParentFrame.FrameNumber);
                        IPv4Packet iteratorVariable3 = new IPv4Packet(parentFrame, 0, parentFrame.Data.Length - 1) {
                            fragmentOffset = 0,
                            moreFragmentsFlag = false,
                            totalLength = (ushort) destinationArray.Length
                        };
                        foreach (AbstractPacket iteratorVariable4 in iteratorVariable3.GetSubPackets(false))
                        {
                            yield return iteratorVariable4;
                        }
                    }
                }
            }
            else if (((this.PacketStartIndex + this.headerLength) < this.PacketEndIndex) && (this.fragmentOffset == 0))
            {
                AbstractPacket iteratorVariable5;
                try
                {
                    if (this.protocol == 6)
                    {
                        iteratorVariable5 = new TcpPacket(this.ParentFrame, this.PacketStartIndex + this.headerLength, this.PacketEndIndex);
                    }
                    else if (this.protocol == 0x11)
                    {
                        iteratorVariable5 = new UdpPacket(this.ParentFrame, this.PacketStartIndex + this.headerLength, this.PacketEndIndex);
                    }
                    else if (this.protocol == 0x29)
                    {
                        iteratorVariable5 = new IPv6Packet(this.ParentFrame, this.PacketStartIndex + this.headerLength, this.PacketEndIndex);
                    }
                    else if (this.protocol == 0x2f)
                    {
                        iteratorVariable5 = new GrePacket(this.ParentFrame, this.PacketStartIndex + this.headerLength, this.PacketEndIndex);
                    }
                    else
                    {
                        iteratorVariable5 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.headerLength, this.PacketEndIndex);
                    }
                }
                catch (Exception)
                {
                    iteratorVariable5 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.headerLength, this.PacketEndIndex);
                }
                yield return iteratorVariable5;
                foreach (AbstractPacket iteratorVariable6 in iteratorVariable5.GetSubPackets(false))
                {
                    yield return iteratorVariable6;
                }
            }
        Label_061A:
            yield break;
        }

        public IPAddress DestinationIPAddress
        {
            get
            {
                return this.destinationIP;
            }
        }

        public bool DontFragmentFlag
        {
            get
            {
                return this.dontFragmentFlag;
            }
        }

        public byte HeaderLength
        {
            get
            {
                return this.headerLength;
            }
        }

        public byte HopLimit
        {
            get
            {
                return this.TimeToLive;
            }
        }

        public int PayloadLength
        {
            get
            {
                return (this.totalLength - this.headerLength);
            }
        }

        public IPAddress SourceIPAddress
        {
            get
            {
                return this.sourceIP;
            }
        }

        public byte TimeToLive
        {
            get
            {
                return this.timeToLive;
            }
        }

        public ushort TotalLength
        {
            get
            {
                return this.totalLength;
            }
        }


        internal enum RFC1700Protocols : byte
        {
            GRE = 0x2f,
            ICMP = 1,
            IGMP = 2,
            IPv6 = 0x29,
            OSPF = 0x59,
            SCTP = 0x84,
            TCP = 6,
            UDP = 0x11
        }
    }
}

