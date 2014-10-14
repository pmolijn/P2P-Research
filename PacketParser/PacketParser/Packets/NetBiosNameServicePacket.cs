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

    internal class NetBiosNameServicePacket : NetBiosPacket
    {
        private ushort additionalCount;
        private IPAddress answerAddress;
        private ushort answerClass;
        private ushort answerCount;
        private ushort answerDataLength;
        private ushort answerFlags;
        private string answerNameDecoded;
        private uint answerTTL;
        private ushort answerType;
        private ushort authorityCount;
        private HeaderFlags headerFlags;
        private ushort questionClass;
        private ushort questionCount;
        private string questionNameDecoded;
        private ushort questionType;
        private ushort transactionID;

        internal NetBiosNameServicePacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "NetBIOS Name Service")
        {
            this.transactionID = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex);
            this.headerFlags = new HeaderFlags(ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2));
            this.questionCount = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 4);
            this.answerCount = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6);
            this.authorityCount = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 8);
            this.additionalCount = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 10);
            int frameIndex = packetStartIndex + 12;
            this.questionNameDecoded = null;
            for (int i = 0; i < this.questionCount; i++)
            {
                this.questionNameDecoded = NetBiosPacket.DecodeNetBiosName(parentFrame, ref frameIndex);
                this.questionType = ByteConverter.ToUInt16(parentFrame.Data, frameIndex);
                frameIndex += 2;
                this.questionClass = ByteConverter.ToUInt16(parentFrame.Data, frameIndex);
                frameIndex += 2;
            }
            this.answerNameDecoded = null;
            this.answerAddress = new IPAddress(0L);
            for (int j = 0; j < this.answerCount; j++)
            {
                this.answerNameDecoded = NetBiosPacket.DecodeNetBiosName(parentFrame, ref frameIndex);
                this.answerType = ByteConverter.ToUInt16(parentFrame.Data, frameIndex);
                frameIndex += 2;
                this.answerClass = ByteConverter.ToUInt16(parentFrame.Data, frameIndex);
                frameIndex += 2;
                this.answerTTL = ByteConverter.ToUInt32(parentFrame.Data, frameIndex);
                frameIndex += 4;
                this.answerDataLength = ByteConverter.ToUInt16(parentFrame.Data, frameIndex);
                frameIndex += 2;
                this.answerFlags = ByteConverter.ToUInt16(parentFrame.Data, frameIndex);
                frameIndex += 2;
                byte[] destinationArray = new byte[4];
                Array.Copy(parentFrame.Data, frameIndex, destinationArray, 0, destinationArray.Length);
                this.answerAddress = new IPAddress(destinationArray);
                frameIndex += 4;
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.PacketStartIndex + 8) < this.PacketEndIndex)
            {
                RawPacket iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 8, this.PacketEndIndex);
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

        internal IPAddress AnsweredIpAddress
        {
            get
            {
                return this.answerAddress;
            }
        }

        internal string AnsweredNetBiosName
        {
            get
            {
                return this.answerNameDecoded;
            }
        }

        internal string QueriedNetBiosName
        {
            get
            {
                return this.questionNameDecoded;
            }
        }


        internal class HeaderFlags
        {
            private ushort headerData;

            internal HeaderFlags(ushort value)
            {
                this.headerData = value;
            }

            internal byte NmFlags
            {
                get
                {
                    return (byte) ((this.headerData >> 4) & 0x7f);
                }
            }

            internal byte OperationCode
            {
                get
                {
                    return (byte) ((this.headerData >> 11) & 15);
                }
            }

            internal bool Response
            {
                get
                {
                    return ((this.headerData & 0x8000) == 0x8000);
                }
            }

            internal byte ResultCode
            {
                get
                {
                    return (byte) (this.headerData & 15);
                }
            }

            internal enum OperationCodes : byte
            {
                query = 0,
                refresh = 8,
                registration = 5,
                release = 6,
                WACK = 7
            }
        }
    }
}

