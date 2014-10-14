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
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public class DnsPacket : AbstractPacket
    {
        private ushort additionalCount;
        private ushort answerCount;
        private ResourceRecord[] answerRecords;
        private HeaderFlags headerFlags;
        private ushort nameServerCount;
        private ushort questionClass;
        private ushort questionCount;
        private string[] questionNameDecoded;
        private int questionSectionByteCount;
        private ushort questionType;
        private ushort transactionID;

        internal DnsPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "DNS")
        {
            this.transactionID = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex);
            this.headerFlags = new HeaderFlags(ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2));
            if (!base.ParentFrame.QuickParse)
            {
                if (this.headerFlags.Response)
                {
                    base.Attributes.Add("Type", "Response");
                }
                else
                {
                    base.Attributes.Add("Type", "Request");
                }
                if (this.headerFlags.OperationCode == 0)
                {
                    base.Attributes.Add("Operation", "Standard Query");
                }
                else if (this.headerFlags.OperationCode == 1)
                {
                    base.Attributes.Add("Operation", "Inverse Query");
                }
            }
            if (this.headerFlags.OperationCode < 5)
            {
                this.questionCount = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 4);
                this.answerCount = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6);
                this.answerRecords = new ResourceRecord[this.answerCount];
                if (this.questionCount > 0)
                {
                    int num;
                    List<NameLabel> list = GetNameLabelList(parentFrame.Data, packetStartIndex, 12, out num);
                    this.questionSectionByteCount = num - 12;
                    this.questionNameDecoded = new string[list.Count];
                    for (int j = 0; j < list.Count; j++)
                    {
                        this.questionNameDecoded[j] = list[j].ToString();
                    }
                    this.questionType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + num);
                    this.questionSectionByteCount += 2;
                    this.questionClass = ByteConverter.ToUInt16(parentFrame.Data, (packetStartIndex + num) + 2);
                    this.questionSectionByteCount += 2;
                }
                else
                {
                    this.questionSectionByteCount = 0;
                    this.questionNameDecoded = null;
                }
                int startIndex = (packetStartIndex + 12) + this.questionSectionByteCount;
                for (int i = 0; i < this.answerRecords.Length; i++)
                {
                    this.answerRecords[i] = new ResourceRecord(this, startIndex);
                    startIndex += this.answerRecords[i].ByteCount;
                    if (!base.ParentFrame.QuickParse && (this.answerRecords[i].Type == 1))
                    {
                        if (this.answerRecords[i].IP != null)
                        {
                            base.Attributes.Add("IP", this.answerRecords[i].IP.ToString());
                        }
                        if (this.answerRecords[i].DNS != null)
                        {
                            base.Attributes.Add("DNS", this.answerRecords[i].DNS);
                        }
                    }
                }
            }
        }

        public static List<NameLabel> GetNameLabelList(byte[] data, int packetStartIndex, int labelStartOffset, out int typeStartOffset)
        {
            return GetNameLabelList(data, packetStartIndex, labelStartOffset, 20, out typeStartOffset);
        }

        public static List<NameLabel> GetNameLabelList(byte[] data, int packetStartIndex, int labelStartOffset, int ttl, out int typeStartOffset)
        {
            if (ttl <= 0)
            {
                throw new Exception("DNS Name Label contains a pointer that loops");
            }
            int num = 0;
            typeStartOffset = labelStartOffset;
            List<NameLabel> list = new List<NameLabel>();
            while (((data[(packetStartIndex + labelStartOffset) + num] != 0) && (data[(packetStartIndex + labelStartOffset) + num] < 0x40)) && (num <= 0xff))
            {
                NameLabel item = new NameLabel(data, (packetStartIndex + labelStartOffset) + num);
                if (item.LabelByteCount <= 0)
                {
                    break;
                }
                num += item.LabelByteCount + 1;
                list.Add(item);
                typeStartOffset = labelStartOffset + num;
            }
            if (data[(packetStartIndex + labelStartOffset) + num] == 0)
            {
                typeStartOffset++;
                return list;
            }
            if (data[(packetStartIndex + labelStartOffset) + num] >= 0xc0)
            {
                int num3;
                ushort num2 = (ushort) (ByteConverter.ToUInt16(data, (packetStartIndex + labelStartOffset) + num) & 0x3fff);
                list.AddRange(GetNameLabelList(data, packetStartIndex, num2, ttl - 1, out num3));
                typeStartOffset += 2;
            }
            return list;
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        public ResourceRecord[] AnswerRecords
        {
            get
            {
                return this.answerRecords;
            }
        }

        public HeaderFlags Flags
        {
            get
            {
                return this.headerFlags;
            }
        }

        public string QueriedDnsName
        {
            get
            {
                if (this.questionCount <= 0)
                {
                    return null;
                }
                if ((this.questionNameDecoded == null) || (this.questionNameDecoded.Length <= 0))
                {
                    return null;
                }
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < this.questionNameDecoded.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(".");
                    }
                    builder.Append(this.questionNameDecoded[i]);
                }
                return builder.ToString();
            }
        }

        public ushort TransactionId
        {
            get
            {
                return this.transactionID;
            }
        }


        public class HeaderFlags
        {
            private ushort headerData;

            internal HeaderFlags(ushort value)
            {
                this.headerData = value;
            }

            internal byte OperationCode
            {
                get
                {
                    return (byte) ((this.headerData >> 11) & 15);
                }
            }

            internal bool RecursionDesired
            {
                get
                {
                    return ((this.headerData >> 8) == 1);
                }
            }

            internal bool Response
            {
                get
                {
                    return ((this.headerData >> 15) == 1);
                }
            }

            internal byte ResultCode
            {
                get
                {
                    return (byte) (this.headerData & 15);
                }
            }

            internal bool Truncated
            {
                get
                {
                    return ((this.headerData >> 9) == 1);
                }
            }

            internal enum OperationCodes : byte
            {
                InverseQuery = 1,
                Query = 0,
                ServerStatusRequest = 2
            }

            internal enum ResultCodes : byte
            {
                FormatError = 1,
                NameError = 3,
                NoErrorCondition = 0,
                NotImplemented = 4,
                Refused = 5,
                ServerFailure = 2
            }
        }

        public class NameLabel
        {
            private StringBuilder decodedName;
            private byte labelByteCount;
            private int labelStartPosition;

            internal NameLabel(byte[] sourceData, int labelStartPosition)
            {
                this.labelStartPosition = labelStartPosition;
                this.decodedName = new StringBuilder();
                this.labelByteCount = sourceData[labelStartPosition];
                if (this.labelByteCount > 0x3f)
                {
                    throw new Exception(string.Concat(new object[] { "DNS Name label is larger than 63 : ", this.labelByteCount, " at position ", labelStartPosition }));
                }
                for (byte i = 0; i < this.labelByteCount; i = (byte) (i + 1))
                {
                    this.decodedName.Append((char) sourceData[(labelStartPosition + 1) + i]);
                }
            }

            public override string ToString()
            {
                return this.decodedName.ToString();
            }

            internal byte LabelByteCount
            {
                get
                {
                    return this.labelByteCount;
                }
            }
        }

        public class ResourceRecord
        {
            private ushort answerClass;
            private ushort answerDataLength;
            private string[] answerRepliedNameDecoded;
            private string[] answerRequestedNameDecoded;
            private uint answerTimeToLive;
            private ushort answerType;
            private DnsPacket parentPacket;
            private int recordByteCount;

            public ResourceRecord(DnsPacket parentPacket, int startIndex)
            {
                int num;
                this.parentPacket = parentPacket;
                List<DnsPacket.NameLabel> list = DnsPacket.GetNameLabelList(parentPacket.ParentFrame.Data, parentPacket.PacketStartIndex, startIndex - parentPacket.PacketStartIndex, out num);
                this.answerRequestedNameDecoded = new string[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    this.answerRequestedNameDecoded[i] = list[i].ToString();
                }
                this.answerType = ByteConverter.ToUInt16(parentPacket.ParentFrame.Data, parentPacket.PacketStartIndex + num);
                this.answerClass = ByteConverter.ToUInt16(parentPacket.ParentFrame.Data, (parentPacket.PacketStartIndex + num) + 2);
                this.answerTimeToLive = ByteConverter.ToUInt32(parentPacket.ParentFrame.Data, (parentPacket.PacketStartIndex + num) + 4);
                this.answerDataLength = ByteConverter.ToUInt16(parentPacket.ParentFrame.Data, (parentPacket.PacketStartIndex + num) + 8);
                this.recordByteCount = (((num - startIndex) + parentPacket.PacketStartIndex) + 10) + this.answerDataLength;
                if ((parentPacket.Flags.OperationCode == 0) && (this.answerType != 5))
                {
                    this.answerRepliedNameDecoded = new string[this.answerDataLength];
                    for (int j = 0; j < this.answerDataLength; j++)
                    {
                        this.answerRepliedNameDecoded[j] = parentPacket.ParentFrame.Data[(startIndex + 12) + j].ToString();
                    }
                }
                else if ((parentPacket.Flags.OperationCode == 0) && (this.answerType == 5))
                {
                    List<DnsPacket.NameLabel> list2 = DnsPacket.GetNameLabelList(parentPacket.ParentFrame.Data, parentPacket.PacketStartIndex, (startIndex + 12) - parentPacket.PacketStartIndex, out num);
                    this.answerRepliedNameDecoded = new string[list2.Count];
                    for (int k = 0; k < list2.Count; k++)
                    {
                        this.answerRepliedNameDecoded[k] = list2[k].ToString();
                    }
                }
                else if (parentPacket.Flags.OperationCode == 1)
                {
                    list = DnsPacket.GetNameLabelList(parentPacket.ParentFrame.Data, parentPacket.PacketStartIndex, (startIndex + 12) - parentPacket.PacketStartIndex, out num);
                    this.answerRepliedNameDecoded = new string[list.Count];
                    for (int m = 0; m < list.Count; m++)
                    {
                        this.answerRepliedNameDecoded[m] = list[m].ToString();
                    }
                }
            }

            public int ByteCount
            {
                get
                {
                    return this.recordByteCount;
                }
            }

            public string DNS
            {
                get
                {
                    if (this.parentPacket.headerFlags.OperationCode == 0)
                    {
                        if ((this.answerRequestedNameDecoded == null) || (this.answerRequestedNameDecoded.Length <= 0))
                        {
                            return null;
                        }
                        StringBuilder builder = new StringBuilder();
                        for (int j = 0; j < this.answerRequestedNameDecoded.Length; j++)
                        {
                            if (j > 0)
                            {
                                builder.Append(".");
                            }
                            builder.Append(this.answerRequestedNameDecoded[j]);
                        }
                        return builder.ToString();
                    }
                    if (this.parentPacket.headerFlags.OperationCode != 1)
                    {
                        return null;
                    }
                    if ((this.answerRepliedNameDecoded == null) || (this.answerRepliedNameDecoded.Length <= 0))
                    {
                        return null;
                    }
                    StringBuilder builder2 = new StringBuilder();
                    for (int i = 0; i < this.answerRepliedNameDecoded.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder2.Append(".");
                        }
                        builder2.Append(this.answerRepliedNameDecoded[i]);
                    }
                    return builder2.ToString();
                }
            }

            public IPAddress IP
            {
                get
                {
                    if ((this.parentPacket.headerFlags.OperationCode == 0) && (this.answerType == 1))
                    {
                        try
                        {
                            byte[] address = new byte[4];
                            for (int i = 0; i < 4; i++)
                            {
                                address[i] = Convert.ToByte(this.answerRepliedNameDecoded[i]);
                            }
                            return new IPAddress(address);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    if (this.parentPacket.headerFlags.OperationCode == 1)
                    {
                        try
                        {
                            byte[] buffer2 = new byte[4];
                            for (int j = 0; j < 4; j++)
                            {
                                buffer2[j] = Convert.ToByte(this.answerRequestedNameDecoded[j]);
                            }
                            return new IPAddress(buffer2);
                        }
                        catch
                        {
                            return null;
                        }
                    }
                    return null;
                }
            }

            public DnsPacket ParentPacket
            {
                get
                {
                    return this.parentPacket;
                }
            }

            public string PrimaryName
            {
                get
                {
                    if (this.answerType != 5)
                    {
                        return null;
                    }
                    if ((this.answerRepliedNameDecoded == null) || (this.answerRepliedNameDecoded.Length <= 0))
                    {
                        return null;
                    }
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < this.answerRepliedNameDecoded.Length; i++)
                    {
                        if (i > 0)
                        {
                            builder.Append(".");
                        }
                        builder.Append(this.answerRepliedNameDecoded[i]);
                    }
                    return builder.ToString();
                }
            }

            public TimeSpan TimeToLive
            {
                get
                {
                    return new TimeSpan(0, 0, (int) this.answerTimeToLive);
                }
            }

            public ushort Type
            {
                get
                {
                    return this.answerType;
                }
            }
        }

        public enum RRTypes : uint
        {
            CNAME = 5,
            DomainNamePointer = 12,
            HostAddress = 1,
            NB = 0x20,
            NBSTAT = 0x21
        }
    }
}

