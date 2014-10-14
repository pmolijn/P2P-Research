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

    public class UdpPacket : AbstractPacket, ITransportLayerPacket, IPacket
    {
        private ushort checksum;
        private ushort destinationPort;
        private ushort length;
        private ushort sourcePort;

        internal UdpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "UDP")
        {
            this.sourcePort = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Source Port", this.sourcePort.ToString());
            }
            this.destinationPort = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Destination Port", this.destinationPort.ToString());
            }
            this.length = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 4);
            if (this.length != ((packetEndIndex - packetStartIndex) + 1))
            {
                if (!base.ParentFrame.QuickParse)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 4, base.PacketStartIndex + 5, string.Concat(new object[] { "UDP defined length (", this.length, ") differs from actual length (", (packetEndIndex - packetStartIndex) + 1, ")" })));
                }
                if (packetEndIndex > ((packetStartIndex + this.length) - 1))
                {
                    packetEndIndex = (packetStartIndex + this.length) - 1;
                    base.PacketEndIndex = packetEndIndex;
                }
            }
            this.checksum = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 8) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0;
                ApplicationLayerProtocol iteratorVariable1 = UdpPortProtocolFinder.Instance.GetApplicationLayerProtocol(TransportLayerProtocol.UDP, this.sourcePort, this.destinationPort);
                if (iteratorVariable1 == ApplicationLayerProtocol.Dns)
                {
                    try
                    {
                        iteratorVariable0 = new DnsPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse DNS packet (" + exception.Message + ")"));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else if (iteratorVariable1 == ApplicationLayerProtocol.Dhcp)
                {
                    try
                    {
                        iteratorVariable0 = new DhcpPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception2)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse DHCP (or BOOTP) protocol: " + exception2.Message));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else if (iteratorVariable1 == ApplicationLayerProtocol.Tftp)
                {
                    try
                    {
                        iteratorVariable0 = new TftpPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception3)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse NetBiosNameServicePacket packet (" + exception3.Message + ")"));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else if (iteratorVariable1 == ApplicationLayerProtocol.NetBiosNameService)
                {
                    try
                    {
                        iteratorVariable0 = new NetBiosNameServicePacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception4)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse NetBiosNameServicePacket packet (" + exception4.Message + ")"));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else if (iteratorVariable1 == ApplicationLayerProtocol.NetBiosDatagramService)
                {
                    try
                    {
                        iteratorVariable0 = new NetBiosDatagramServicePacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception5)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse NetBiosDatagramServicePacket packet (" + exception5.Message + ")"));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else if (iteratorVariable1 == ApplicationLayerProtocol.Syslog)
                {
                    try
                    {
                        iteratorVariable0 = new SyslogPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception6)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse Syslog packet (" + exception6.Message + ")"));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else if (iteratorVariable1 == ApplicationLayerProtocol.Upnp)
                {
                    try
                    {
                        iteratorVariable0 = new UpnpPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception7)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse UPnP packet (" + exception7.Message + ")"));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else if (iteratorVariable1 == ApplicationLayerProtocol.Sip)
                {
                    try
                    {
                        iteratorVariable0 = new SipPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                    catch (Exception exception8)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex, "Cannot parse SIP packet (" + exception8.Message + ")"));
                        }
                        iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                    }
                }
                else
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                }
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable2 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable2;
                }
            }
        }

        public byte DataOffsetByteCount
        {
            get
            {
                return 8;
            }
        }

        public ushort DestinationPort
        {
            get
            {
                return this.destinationPort;
            }
        }

        public byte FlagsRaw
        {
            get
            {
                return 0;
            }
        }

        public ushort SourcePort
        {
            get
            {
                return this.sourcePort;
            }
        }

    }
}

