namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal class OscarFileTransferPacket : AbstractPacket, ISessionPacket
    {
        private ushort commandType;
        private string fileName;
        private uint totalFileSize;

        internal OscarFileTransferPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "OSCAR File Transfer")
        {
            ByteConverter.ReadString(parentFrame.Data, packetStartIndex, 4);
            this.commandType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Command Type", "0x" + this.commandType.ToString("X2"));
            }
            ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x10);
            ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x12);
            ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 20);
            ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x16);
            ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x18);
            ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x1a);
            this.totalFileSize = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 0x1c);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Total File Size", this.totalFileSize.ToString());
            }
            int dataIndex = packetStartIndex + 0x44;
            ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref dataIndex);
            dataIndex = packetStartIndex + 0xc0;
            this.fileName = ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref dataIndex);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Filename", this.fileName);
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

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            try
            {
                if ((packetEndIndex - packetStartIndex) < 0xff)
                {
                    return false;
                }
                if (!ByteConverter.ReadString(parentFrame.Data, packetStartIndex, 4).Equals("OFT2"))
                {
                    return false;
                }
                if (!Enum.IsDefined(typeof(CommandType), ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6)))
                {
                    return false;
                }
                result = new OscarFileTransferPacket(parentFrame, packetStartIndex, packetEndIndex);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public string FileName
        {
            get
            {
                return this.fileName;
            }
        }

        public bool PacketHeaderIsComplete
        {
            get
            {
                return true;
            }
        }

        public int ParsedBytesCount
        {
            get
            {
                return 0x100;
            }
        }

        public uint TotalFileSize
        {
            get
            {
                return this.totalFileSize;
            }
        }

        public CommandType Type
        {
            get
            {
                return (CommandType) this.commandType;
            }
        }

        public enum CommandType : ushort
        {
            ReceiveAccept = 0x202,
            SendRequest = 0x101,
            TransferComplete = 0x204
        }
    }
}

