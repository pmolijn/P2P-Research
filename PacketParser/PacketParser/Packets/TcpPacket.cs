namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;

    public class TcpPacket : AbstractPacket, ITransportLayerPacket, IPacket
    {
        private uint acknowledgmentNumber;
        private ushort checksum;
        private byte dataOffsetByteCount;
        private ushort destinationPort;
        private Flags flags;
        private byte flagsRaw;
        private List<KeyValuePair<OptionKinds, byte[]>> optionList;
        private uint sequenceNumber;
        private ushort sourcePort;
        private ushort windowSize;

        internal TcpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "TCP")
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
            this.sequenceNumber = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 4);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Sequence Number", this.sequenceNumber.ToString("X2"));
            }
            this.acknowledgmentNumber = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 8);
            this.dataOffsetByteCount = (byte) (4 * (parentFrame.Data[packetStartIndex + 12] >> 4));
            if (!base.ParentFrame.QuickParse)
            {
                if (this.dataOffsetByteCount < 20)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 12, packetStartIndex + 12, "Too small defined TCP Data Offset : " + parentFrame.Data[packetStartIndex + 12]));
                }
                else if (this.dataOffsetByteCount > 60)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 12, packetStartIndex + 12, "Too large defined TCP Data Offset : " + parentFrame.Data[packetStartIndex + 12]));
                }
                else if (((base.PacketEndIndex - base.PacketStartIndex) + 1) < this.dataOffsetByteCount)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 12, packetStartIndex + 12, "TCP Data offset is outside frame"));
                }
            }
            this.flagsRaw = parentFrame.Data[packetStartIndex + 13];
            this.flags = new Flags(this.flagsRaw);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Flags", this.flags.ToString());
            }
            this.windowSize = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 14);
            this.checksum = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x10);
            if (this.dataOffsetByteCount > 20)
            {
                this.optionList = this.GetOptionList(packetStartIndex + 20);
            }
            else
            {
                this.optionList = null;
            }
        }

        private List<KeyValuePair<OptionKinds, byte[]>> GetOptionList(int startIndex)
        {
            List<KeyValuePair<OptionKinds, byte[]>> list = new List<KeyValuePair<OptionKinds, byte[]>>();
            int num = 0;
            while ((((startIndex + num) < (base.PacketStartIndex + this.dataOffsetByteCount)) && ((startIndex + num) < base.ParentFrame.Data.Length)) && (base.ParentFrame.Data[startIndex + num] != 0))
            {
                if (base.ParentFrame.Data[startIndex + num] > 8)
                {
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.ParentFrame.Errors.Add(new Frame.Error(base.ParentFrame, startIndex + num, startIndex + num, "TCP Option Kind is larger than 8 (it is:" + base.ParentFrame.Data[startIndex + num] + ")"));
                    }
                    return list;
                }
                OptionKinds key = (OptionKinds) base.ParentFrame.Data[startIndex + num];
                switch (key)
                {
                    case OptionKinds.EndOfOptionList:
                        list.Add(new KeyValuePair<OptionKinds, byte[]>(key, null));
                        num++;
                        return list;

                    case OptionKinds.NoOperation:
                    {
                        list.Add(new KeyValuePair<OptionKinds, byte[]>(key, null));
                        num++;
                        continue;
                    }
                }
                byte num2 = base.ParentFrame.Data[(startIndex + num) + 1];
                if (num2 < 2)
                {
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.ParentFrame.Errors.Add(new Frame.Error(base.ParentFrame, (startIndex + num) + 1, (startIndex + num) + 1, "TCP Option Length (" + num2 + ") is shorter than 2"));
                    }
                    num2 = 2;
                }
                else if (((startIndex + num) + num2) > (base.PacketStartIndex + this.dataOffsetByteCount))
                {
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.ParentFrame.Errors.Add(new Frame.Error(base.ParentFrame, (startIndex + num) + 1, (startIndex + num) + 1, string.Concat(new object[] { "TCP Option Length (", num2, ") makes option end outside TCP Data Offset (", this.dataOffsetByteCount, ")" })));
                    }
                    num2 = (byte) (((base.PacketStartIndex + this.dataOffsetByteCount) - startIndex) - num);
                }
                else if (num2 > 0x2c)
                {
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.ParentFrame.Errors.Add(new Frame.Error(base.ParentFrame, (startIndex + num) + 1, (startIndex + num) + 1, "TCP Option Length (" + num2 + ") is longer than 44"));
                    }
                    num2 = 0x2c;
                }
                byte[] destinationArray = new byte[num2 - 2];
                Array.Copy(base.ParentFrame.Data, (startIndex + num) + 2, destinationArray, 0, Math.Min((int) (((base.ParentFrame.Data.Length - startIndex) - num) - 2), (int) (num2 - 2)));
                list.Add(new KeyValuePair<OptionKinds, byte[]>(key, destinationArray));
                num += num2;
            }
            return list;
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + this.dataOffsetByteCount) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex);
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

        internal IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference, ISessionProtocolFinder protocolFinder, bool clientToServer)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + this.dataOffsetByteCount) >= this.PacketEndIndex)
            {
                goto Label_060D;
            }
            AbstractPacket result = null;
            foreach (ApplicationLayerProtocol protocol in protocolFinder.GetProbableApplicationLayerProtocols())
            {
                try
                {
                    switch (protocol)
                    {
                        case ApplicationLayerProtocol.Dns:
                            result = new DnsPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex);
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Dns;
                            goto Label_054D;

                        case ApplicationLayerProtocol.FtpControl:
                            if (!FtpPacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, clientToServer, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.FtpControl;
                            goto Label_054D;

                        case ApplicationLayerProtocol.Http:
                            if (!HttpPacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Http;
                            goto Label_054D;

                        case ApplicationLayerProtocol.Irc:
                            if (!IrcPacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Irc;
                            goto Label_054D;

                        case ApplicationLayerProtocol.IEC_104:
                            if (!IEC_60870_5_104Packet.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.IEC_104;
                            goto Label_054D;

                        case ApplicationLayerProtocol.NetBiosNameService:
                            result = new NetBiosNameServicePacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex);
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.NetBiosNameService;
                            goto Label_054D;

                        case ApplicationLayerProtocol.NetBiosSessionService:
                            if (!NetBiosSessionService.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.NetBiosSessionService;
                            goto Label_054D;

                        case ApplicationLayerProtocol.Oscar:
                            if (!OscarPacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Oscar;
                            goto Label_054D;

                        case ApplicationLayerProtocol.OscarFileTransfer:
                            if (!OscarFileTransferPacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.OscarFileTransfer;
                            goto Label_054D;

                        case ApplicationLayerProtocol.Smtp:
                            result = new SmtpPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, clientToServer);
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Smtp;
                            goto Label_054D;

                        case ApplicationLayerProtocol.SpotifyServerProtocol:
                            if (!SpotifyKeyExchangePacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, clientToServer, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.SpotifyServerProtocol;
                            goto Label_054D;

                        case ApplicationLayerProtocol.Ssh:
                            if (!SshPacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Ssh;
                            goto Label_054D;

                        case ApplicationLayerProtocol.Ssl:
                            if (!SslPacket.TryParse(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex, out result))
                            {
                                continue;
                            }
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Ssl;
                            goto Label_054D;

                        case ApplicationLayerProtocol.TabularDataStream:
                            result = new TabularDataStreamPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex);
                            protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.TabularDataStream;
                            goto Label_054D;
                    }
                }
                catch (Exception)
                {
                    result = null;
                }
            }
        Label_054D:
            if (result == null)
            {
                result = new RawPacket(this.ParentFrame, this.PacketStartIndex + this.dataOffsetByteCount, this.PacketEndIndex);
            }
            yield return result;
            foreach (AbstractPacket iteratorVariable1 in result.GetSubPackets(false))
            {
                yield return iteratorVariable1;
            }
        Label_060D:
            yield break;
        }

        internal byte[] GetTcpPacketPayloadData()
        {
            byte[] buffer = new byte[((base.PacketEndIndex - base.PacketStartIndex) - this.dataOffsetByteCount) + 1];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = base.ParentFrame.Data[(base.PacketStartIndex + this.dataOffsetByteCount) + i];
            }
            return buffer;
        }

        public uint AcknowledgmentNumber
        {
            get
            {
                return this.acknowledgmentNumber;
            }
        }

        public byte DataOffsetByteCount
        {
            get
            {
                return this.dataOffsetByteCount;
            }
        }

        public ushort DestinationPort
        {
            get
            {
                return this.destinationPort;
            }
        }

        public Flags FlagBits
        {
            get
            {
                return this.flags;
            }
        }

        public byte FlagsRaw
        {
            get
            {
                return this.flagsRaw;
            }
        }

        public List<KeyValuePair<OptionKinds, byte[]>> OptionList
        {
            get
            {
                if (this.optionList == null)
                {
                    this.optionList = new List<KeyValuePair<OptionKinds, byte[]>>();
                }
                return this.optionList;
            }
        }

        public int PayloadDataLength
        {
            get
            {
                return (((base.PacketEndIndex - base.PacketStartIndex) - this.dataOffsetByteCount) + 1);
            }
        }

        public uint SequenceNumber
        {
            get
            {
                return this.sequenceNumber;
            }
        }

        public ushort SourcePort
        {
            get
            {
                return this.sourcePort;
            }
        }

        public ushort WindowSize
        {
            get
            {
                return this.windowSize;
            }
        }



        public class Flags
        {
            private byte flagData;

            public Flags(byte data)
            {
                this.flagData = data;
            }

            public bool[] GetFlagArray()
            {
                return new bool[] { this.CongestionWindowReduced, this.ECNEcho, this.UrgentPointer, this.Acknowledgement, this.Push, this.Reset, this.Synchronize, this.Fin };
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                char ch = ' ';
                if (this.CongestionWindowReduced)
                {
                    builder.Append("C");
                }
                else
                {
                    builder.Append(ch);
                }
                if (this.ECNEcho)
                {
                    builder.Append("E");
                }
                else
                {
                    builder.Append(ch);
                }
                if (this.UrgentPointer)
                {
                    builder.Append("U");
                }
                else
                {
                    builder.Append(ch);
                }
                if (this.Acknowledgement)
                {
                    builder.Append("A");
                }
                else
                {
                    builder.Append(ch);
                }
                if (this.Push)
                {
                    builder.Append("P");
                }
                else
                {
                    builder.Append(ch);
                }
                if (this.Reset)
                {
                    builder.Append("R");
                }
                else
                {
                    builder.Append(ch);
                }
                if (this.Synchronize)
                {
                    builder.Append("S");
                }
                else
                {
                    builder.Append(ch);
                }
                if (this.Fin)
                {
                    builder.Append("F");
                }
                else
                {
                    builder.Append(ch);
                }
                return builder.ToString();
            }

            public bool Acknowledgement
            {
                get
                {
                    return ((this.flagData & 0x10) == 0x10);
                }
            }

            public bool CongestionWindowReduced
            {
                get
                {
                    return ((this.flagData & 0x80) == 0x80);
                }
            }

            public bool ECNEcho
            {
                get
                {
                    return ((this.flagData & 0x40) == 0x40);
                }
            }

            public bool Fin
            {
                get
                {
                    return ((this.flagData & 1) == 1);
                }
            }

            public bool Push
            {
                get
                {
                    return ((this.flagData & 8) == 8);
                }
            }

            public byte RawData
            {
                get
                {
                    return this.flagData;
                }
            }

            public bool Reset
            {
                get
                {
                    return ((this.flagData & 4) == 4);
                }
            }

            public bool Synchronize
            {
                get
                {
                    return ((this.flagData & 2) == 2);
                }
            }

            public bool UrgentPointer
            {
                get
                {
                    return ((this.flagData & 0x20) == 0x20);
                }
            }
        }

        public enum OptionKinds : byte
        {
            Echo = 6,
            EchoReply = 7,
            EndOfOptionList = 0,
            MaximumSegmentSize = 2,
            NoOperation = 1,
            Sack = 5,
            SackPermitted = 4,
            Timestamp = 8,
            WindowScaleFactor = 3
        }
    }
}

