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

    internal class TftpPacket : AbstractPacket
    {
        private ushort blksize;
        private byte[] dataBlock;
        private ushort dataBlockNumber;
        internal const ushort DefaultUdpPortNumber = 0x45;
        private string filename;
        private Modes mode;
        private ushort opCode;
        private Dictionary<string, string> rfc2347OptionList;

        internal TftpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : this(parentFrame, packetStartIndex, packetEndIndex, 0x200)
        {
        }

        internal TftpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex, ushort blksize) : base(parentFrame, packetStartIndex, packetEndIndex, "TFTP")
        {
            this.blksize = blksize;
            this.rfc2347OptionList = new Dictionary<string, string>();
            this.opCode = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex);
            if ((this.opCode < 1) || (this.opCode > 6))
            {
                throw new Exception("Incorrect OPCODE (" + this.opCode + "), not correct TFTP pakcet");
            }
            if ((this.opCode == 1) || (this.opCode == 2))
            {
                int dataIndex = packetStartIndex + 2;
                this.filename = ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref dataIndex);
                string str = ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref dataIndex);
                if (str.ToLower() == "netascii")
                {
                    this.mode = Modes.netascii;
                }
                else if (str.ToLower() == "octet")
                {
                    this.mode = Modes.octet;
                }
                else if (str.ToLower() == "mail")
                {
                    this.mode = Modes.mail;
                }
                while (dataIndex < packetEndIndex)
                {
                    string str2 = ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref dataIndex);
                    string s = ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref dataIndex);
                    this.rfc2347OptionList[str2] = s;
                    if (str2.Equals("blksize", StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.blksize = ushort.Parse(s);
                    }
                }
            }
            else if (this.opCode == 3)
            {
                this.dataBlockNumber = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
                this.dataBlock = new byte[Math.Min((int) blksize, (packetEndIndex - packetStartIndex) - 3)];
                Array.Copy(parentFrame.Data, packetStartIndex + 4, this.dataBlock, 0, this.dataBlock.Length);
            }
            else if (this.opCode == 4)
            {
                this.dataBlockNumber = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
            }
            else if (this.opCode == 6)
            {
                int num2 = packetStartIndex + 2;
                while (num2 < packetEndIndex)
                {
                    string str4 = ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref num2);
                    string str5 = ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref num2);
                    this.rfc2347OptionList[str4] = str5;
                    if (str4.Equals("blksize", StringComparison.InvariantCultureIgnoreCase))
                    {
                        this.blksize = ushort.Parse(str5);
                    }
                }
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        internal ushort Blksize
        {
            get
            {
                return this.blksize;
            }
        }

        internal byte[] DataBlock
        {
            get
            {
                return this.dataBlock;
            }
        }

        internal bool DataBlockIsLast
        {
            get
            {
                return (this.dataBlock.Length < this.blksize);
            }
        }

        internal ushort DataBlockNumber
        {
            get
            {
                return this.dataBlockNumber;
            }
        }

        internal string Filename
        {
            get
            {
                return this.filename;
            }
        }

        internal Modes Mode
        {
            get
            {
                return this.mode;
            }
        }

        internal OpCodes OpCode
        {
            get
            {
                return (OpCodes) this.opCode;
            }
        }


        internal enum Modes
        {
            netascii,
            octet,
            mail
        }

        internal enum OpCodes : ushort
        {
            Acknowledgment = 4,
            Data = 3,
            Error = 5,
            OptionAcknowledgment = 6,
            ReadRequest = 1,
            WriteRequest = 2
        }
    }
}

