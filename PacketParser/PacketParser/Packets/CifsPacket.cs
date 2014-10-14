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

    internal class CifsPacket : AbstractPacket
    {
        private int bufferStartIndex;
        private ushort byteCount;
        private byte command;
        private ushort error;
        private byte errorClass;
        private byte flags;
        private ushort flags2;
        private ushort multiplexId;
        private int parametersStartIndex;
        private ushort processId;
        private uint protocolIdentifier;
        private byte reserved;
        private const uint smbProtocolIdentifier = 0xff534d42;
        private ushort treeId;
        private ushort userId;
        private byte wordCount;

        internal CifsPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "CIFS Server Message Block (SMB)")
        {
            this.protocolIdentifier = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex);
            if (this.protocolIdentifier != 0xff534d42)
            {
                throw new Exception("SMB protocol identifier is: " + this.protocolIdentifier.ToString("X2"));
            }
            this.command = parentFrame.Data[packetStartIndex + 4];
            if (!parentFrame.QuickParse)
            {
                try
                {
                    base.Attributes.Add("Command code", ((CommandTypes) this.command).ToString() + " (0x" + this.command.ToString("X2") + ")");
                }
                catch
                {
                    base.Attributes.Add("Command code", "(0x" + this.command.ToString("X2") + ")");
                }
            }
            this.errorClass = parentFrame.Data[packetStartIndex + 5];
            this.reserved = parentFrame.Data[packetStartIndex + 6];
            this.error = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 7, true);
            this.flags = parentFrame.Data[packetStartIndex + 9];
            this.flags2 = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 10, true);
            this.treeId = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x18, true);
            if (!parentFrame.QuickParse)
            {
                base.Attributes.Add("Tree ID", this.treeId.ToString());
            }
            this.processId = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x1a, true);
            if (!parentFrame.QuickParse)
            {
                base.Attributes.Add("Process ID", this.processId.ToString());
            }
            this.userId = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x1c, true);
            if (!parentFrame.QuickParse)
            {
                base.Attributes.Add("User ID", this.userId.ToString());
            }
            this.multiplexId = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 30, true);
            if (!parentFrame.QuickParse)
            {
                base.Attributes.Add("Multiplex ID", this.multiplexId.ToString());
            }
            this.wordCount = parentFrame.Data[packetStartIndex + 0x20];
            this.parametersStartIndex = packetStartIndex + 0x21;
            this.byteCount = ByteConverter.ToUInt16(parentFrame.Data, (packetStartIndex + 0x21) + (this.wordCount * 2), true);
            if (!parentFrame.QuickParse)
            {
                base.Attributes.Add("Buffer Total Length", this.byteCount.ToString());
            }
            this.bufferStartIndex = ((packetStartIndex + 0x21) + (this.wordCount * 2)) + 2;
        }

        internal string DecodeBufferString()
        {
            int bufferStartIndex = this.bufferStartIndex;
            if (this.Flags2UnicodeStrings && (((this.bufferStartIndex - base.PacketStartIndex) % 2) == 1))
            {
                bufferStartIndex++;
                return ByteConverter.ReadString(base.ParentFrame.Data, ref bufferStartIndex, this.byteCount - 1, this.Flags2UnicodeStrings, true);
            }
            return ByteConverter.ReadString(base.ParentFrame.Data, ref bufferStartIndex, this.byteCount, this.Flags2UnicodeStrings, true);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            AbstractPacket iteratorVariable0 = null;
            try
            {
                switch (((CommandTypes) this.command))
                {
                    case CommandTypes.SMB_COM_NT_CREATE_ANDX:
                        if (!this.FlagsResponse)
                        {
                            iteratorVariable0 = new NTCreateAndXRequest(this);
                        }
                        else
                        {
                            iteratorVariable0 = new NTCreateAndXResponse(this);
                        }
                        goto Label_0189;

                    case CommandTypes.SMB_COM_READ_ANDX:
                        if (this.FlagsResponse)
                        {
                            iteratorVariable0 = new ReadAndXResponse(this);
                        }
                        else
                        {
                            iteratorVariable0 = new ReadAndXRequest(this);
                        }
                        goto Label_0189;

                    case CommandTypes.SMB_COM_CLOSE:
                        if (!this.FlagsResponse)
                        {
                            iteratorVariable0 = new CloseRequest(this);
                        }
                        goto Label_0189;

                    case CommandTypes.SMB_COM_NEGOTIATE:
                        if (!this.FlagsResponse)
                        {
                            iteratorVariable0 = new NegotiateProtocolRequest(this);
                        }
                        else
                        {
                            iteratorVariable0 = new NegotiateProtocolResponse(this);
                        }
                        goto Label_0189;

                    case CommandTypes.SMB_COM_SESSION_SETUP_ANDX:
                        if (!this.FlagsResponse)
                        {
                            iteratorVariable0 = new SetupAndXRequest(this);
                            goto Label_0189;
                        }
                        iteratorVariable0 = new SetupAndXResponse(this);
                        break;
                }
            }
            catch (Exception)
            {
                goto Label_0214;
            }
        Label_0189:
            if (iteratorVariable0 != null)
            {
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        Label_0214:
            yield break;
        }

        internal int BufferStartIndex
        {
            get
            {
                return this.bufferStartIndex;
            }
        }

        internal ushort ByteCount
        {
            get
            {
                return this.byteCount;
            }
        }

        internal bool Flags2UnicodeStrings
        {
            get
            {
                return ((this.flags2 & 0x8000) == 0x8000);
            }
        }

        internal bool FlagsResponse
        {
            get
            {
                return ((this.flags & 0x80) == 0x80);
            }
        }

        internal ushort MultiplexId
        {
            get
            {
                return this.multiplexId;
            }
        }

        internal int ParametersStartIndex
        {
            get
            {
                return this.parametersStartIndex;
            }
        }

        internal ushort ProcessId
        {
            get
            {
                return this.processId;
            }
        }

        internal ushort TreeId
        {
            get
            {
                return this.treeId;
            }
        }

        internal byte WordCount
        {
            get
            {
                return this.wordCount;
            }
        }


        internal abstract class AbstractSmbCommand : AbstractPacket
        {
            private CifsPacket parentCifsPacket;
            private int? securityBlobIndex;
            private ushort securityBlobLength;

            internal AbstractSmbCommand(CifsPacket parentCifsPacket, string packetTypeDescription) : base(parentCifsPacket.ParentFrame, parentCifsPacket.ParametersStartIndex, parentCifsPacket.ParentFrame.Data.Length - 1, packetTypeDescription)
            {
                this.parentCifsPacket = parentCifsPacket;
                this.securityBlobIndex = null;
                this.securityBlobLength = 0;
            }

            public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
            {
                if (includeSelfReference)
                {
                    yield return this;
                }
                if (this.securityBlobIndex.HasValue)
                {
                    AbstractPacket iteratorVariable0 = new CifsPacket.SecurityBlob(this.ParentFrame, this.securityBlobIndex.Value, (this.securityBlobIndex.Value + this.securityBlobLength) - 1);
                    yield return iteratorVariable0;
                    foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                    {
                        yield return iteratorVariable1;
                    }
                }
            }

            internal CifsPacket ParentCifsPacket
            {
                get
                {
                    return this.parentCifsPacket;
                }
            }

            internal int? SecurityBlobIndex
            {
                get
                {
                    return this.securityBlobIndex;
                }
                set
                {
                    this.securityBlobIndex = value;
                }
            }

            internal ushort SecurityBlobLength
            {
                get
                {
                    return this.securityBlobLength;
                }
                set
                {
                    this.securityBlobLength = value;
                }
            }

        }

        internal class CloseRequest : CifsPacket.AbstractSmbCommand
        {
            private ushort fileId;

            internal CloseRequest(CifsPacket parentCifsPacket) : base(parentCifsPacket, "Close Request")
            {
                this.fileId = ByteConverter.ToUInt16(base.ParentFrame.Data, parentCifsPacket.parametersStartIndex, true);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("File ID", "0x" + this.fileId.ToString("X2"));
                }
            }

            internal ushort FileId
            {
                get
                {
                    return this.fileId;
                }
            }
        }

        internal enum CommandTypes : byte
        {
            SMB_COM_CHECK_DIRECTORY = 0x10,
            SMB_COM_CLOSE = 4,
            SMB_COM_CLOSE_AND_TREE_DISC = 0x31,
            SMB_COM_CLOSE_PRINT_FILE = 0xc2,
            SMB_COM_COPY = 0x29,
            SMB_COM_CREATE = 3,
            SMB_COM_CREATE_DIRECTORY = 0,
            SMB_COM_CREATE_NEW = 15,
            SMB_COM_CREATE_TEMPORARY = 14,
            SMB_COM_DELETE = 6,
            SMB_COM_DELETE_DIRECTORY = 1,
            SMB_COM_ECHO = 0x2b,
            SMB_COM_FIND = 130,
            SMB_COM_FIND_CLOSE = 0x84,
            SMB_COM_FIND_CLOSE2 = 0x34,
            SMB_COM_FIND_NOTIFY_CLOSE = 0x35,
            SMB_COM_FIND_UNIQUE = 0x83,
            SMB_COM_FLUSH = 5,
            SMB_COM_GET_PRINT_QUEUE = 0xc3,
            SMB_COM_IOCTL = 0x27,
            SMB_COM_IOCTL_SECONDARY = 40,
            SMB_COM_LOCK_AND_READ = 0x13,
            SMB_COM_LOCK_BYTE_RANGE = 12,
            SMB_COM_LOCKING_ANDX = 0x24,
            SMB_COM_LOGOFF_ANDX = 0x74,
            SMB_COM_MOVE = 0x2a,
            SMB_COM_NEGOTIATE = 0x72,
            SMB_COM_NEW_FILE_SIZE = 0x30,
            SMB_COM_NT_CANCEL = 0xa4,
            SMB_COM_NT_CREATE_ANDX = 0xa2,
            SMB_COM_NT_RENAME = 0xa5,
            SMB_COM_NT_TRANSACT = 160,
            SMB_COM_NT_TRANSACT_SECONDARY = 0xa1,
            SMB_COM_OPEN = 2,
            SMB_COM_OPEN_ANDX = 0x2d,
            SMB_COM_OPEN_PRINT_FILE = 0xc0,
            SMB_COM_PROCESS_EXIT = 0x11,
            SMB_COM_QUERY_INFORMATION = 8,
            SMB_COM_QUERY_INFORMATION_DISK = 0x80,
            SMB_COM_QUERY_INFORMATION2 = 0x23,
            SMB_COM_QUERY_SERVER = 0x21,
            SMB_COM_READ = 10,
            SMB_COM_READ_ANDX = 0x2e,
            SMB_COM_READ_BULK = 0xd8,
            SMB_COM_READ_MPX_SECONDARY = 0x1c,
            SMB_COM_READ_MPXv0x1B = 0x1b,
            SMB_COM_READ_RAW = 0x1a,
            SMB_COM_RENAME = 7,
            SMB_COM_SEARCH = 0x81,
            SMB_COM_SEEK = 0x12,
            SMB_COM_SESSION_SETUP_ANDX = 0x73,
            SMB_COM_SET_INFORMATION = 9,
            SMB_COM_SET_INFORMATION2 = 0x22,
            SMB_COM_TRANSACTION = 0x25,
            SMB_COM_TRANSACTION_SECONDARY = 0x26,
            SMB_COM_TRANSACTION2 = 50,
            SMB_COM_TRANSACTION2_SECONDARY = 0x33,
            SMB_COM_TREE_CONNECT = 0x70,
            SMB_COM_TREE_CONNECT_ANDX = 0x75,
            SMB_COM_TREE_DISCONNECT = 0x71,
            SMB_COM_UNLOCK_BYTE_RANGE = 13,
            SMB_COM_WRITE = 11,
            SMB_COM_WRITE_AND_CLOSE = 0x2c,
            SMB_COM_WRITE_AND_UNLOCK = 20,
            SMB_COM_WRITE_ANDX = 0x2f,
            SMB_COM_WRITE_BULK = 0xd9,
            SMB_COM_WRITE_BULK_DATA = 0xda,
            SMB_COM_WRITE_COMPLETE = 0x20,
            SMB_COM_WRITE_MPX = 30,
            SMB_COM_WRITE_MPX_SECONDARY = 0x1f,
            SMB_COM_WRITE_PRINT_FILE = 0xc1,
            SMB_COM_WRITE_RAW = 0x1d
        }

        internal class NegotiateProtocolRequest : CifsPacket.AbstractSmbCommand
        {
            private List<string> dialectList;

            internal NegotiateProtocolRequest(CifsPacket parentCifsPacket) : base(parentCifsPacket, "CIFS Negotiate Protocol Request")
            {
                this.dialectList = new List<string>();
                ushort num = ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex, true);
                int dataIndex = parentCifsPacket.ParametersStartIndex + 2;
                int num3 = dataIndex;
                dataIndex++;
                while (((dataIndex - num3) < num) && (dataIndex < parentCifsPacket.ParentFrame.Data.Length))
                {
                    string item = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex);
                    this.dialectList.Add(item);
                    dataIndex++;
                }
            }

            internal List<string> DialectList
            {
                get
                {
                    return this.dialectList;
                }
            }
        }

        internal class NegotiateProtocolResponse : CifsPacket.AbstractSmbCommand
        {
            private ushort dialectIndex;

            internal NegotiateProtocolResponse(CifsPacket parentCifsPacket) : base(parentCifsPacket, "CIFS Negotiate Protocol Response")
            {
                this.dialectIndex = ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex, true);
            }

            internal ushort DialectIndex
            {
                get
                {
                    return this.dialectIndex;
                }
            }
        }

        internal class NTCreateAndXRequest : CifsPacket.AbstractSmbCommand
        {
            private string filename;

            internal NTCreateAndXRequest(CifsPacket parentCifsPacket) : base(parentCifsPacket, "CIFS NT Create AndX Request")
            {
                if (parentCifsPacket.WordCount != 0x18)
                {
                    throw new Exception("Word Cound is not 24 (" + parentCifsPacket.WordCount.ToString() + ")");
                }
                byte num1 = base.ParentFrame.Data[parentCifsPacket.ParametersStartIndex + 5];
                int bufferStartIndex = parentCifsPacket.BufferStartIndex;
                this.filename = parentCifsPacket.DecodeBufferString();
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Filename", this.filename);
                }
            }

            internal string Filename
            {
                get
                {
                    return this.filename;
                }
            }
        }

        internal class NTCreateAndXResponse : CifsPacket.AbstractSmbCommand
        {
            private ulong endOfFile;
            private ushort fileId;

            internal NTCreateAndXResponse(CifsPacket parentCifsPacket) : base(parentCifsPacket, "NT Create AndX Response")
            {
                this.fileId = ByteConverter.ToUInt16(base.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 5, true);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("File ID", "0x" + this.fileId.ToString("X2"));
                }
                this.endOfFile = ByteConverter.ToUInt64(base.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 0x37, true);
            }

            internal ulong EndOfFile
            {
                get
                {
                    return this.endOfFile;
                }
            }

            internal ushort FileId
            {
                get
                {
                    return this.fileId;
                }
            }
        }

        internal class ReadAndXRequest : CifsPacket.AbstractSmbCommand
        {
            private ushort fileId;

            internal ReadAndXRequest(CifsPacket parentCifsPacket) : base(parentCifsPacket, "CIFS Read AndX Request")
            {
                this.fileId = ByteConverter.ToUInt16(base.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 4, true);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("File ID", "0x" + this.fileId.ToString("X2"));
                }
            }

            internal ushort FileId
            {
                get
                {
                    return this.fileId;
                }
            }
        }

        internal class ReadAndXResponse : CifsPacket.AbstractSmbCommand
        {
            private ushort dataLenght;
            private ushort dataOffset;

            internal ReadAndXResponse(CifsPacket parentCifsPacket) : base(parentCifsPacket, "CIFS Read AndX Response")
            {
                this.dataLenght = ByteConverter.ToUInt16(base.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 10, true);
                this.dataOffset = ByteConverter.ToUInt16(base.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 12, true);
            }

            internal byte[] GetFileData()
            {
                int length = Math.Min((int) this.dataLenght, base.ParentFrame.Data.Length - (base.ParentCifsPacket.PacketStartIndex + this.DataOffset));
                byte[] destinationArray = new byte[length];
                Array.Copy(base.ParentFrame.Data, base.ParentCifsPacket.PacketStartIndex + this.DataOffset, destinationArray, 0, length);
                return destinationArray;
            }

            internal ushort DataLength
            {
                get
                {
                    return this.dataLenght;
                }
            }

            internal ushort DataOffset
            {
                get
                {
                    return this.dataOffset;
                }
            }
        }

        internal class SecurityBlob : AbstractPacket
        {
            private int? ntlmsspIndex;
            private int? spnegoIndex;

            internal SecurityBlob(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Security Blob")
            {
                this.spnegoIndex = null;
                this.ntlmsspIndex = null;
                if (parentFrame.Data[base.PacketStartIndex] == 0x60)
                {
                    this.spnegoIndex = new int?(packetStartIndex + 10);
                }
                else if (parentFrame.Data[base.PacketStartIndex] == 0x4e)
                {
                    this.ntlmsspIndex = new int?(packetStartIndex);
                }
                else if (parentFrame.Data[base.PacketStartIndex] == 160)
                {
                    this.spnegoIndex = new int?(packetStartIndex);
                }
                else if (parentFrame.Data[base.PacketStartIndex] == 0xa1)
                {
                    this.spnegoIndex = new int?(packetStartIndex);
                }
            }

            public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
            {
                if (includeSelfReference)
                {
                    yield return this;
                }
                AbstractPacket iteratorVariable0 = null;
                try
                {
                    if (this.spnegoIndex.HasValue)
                    {
                        iteratorVariable0 = new CifsPacket.SimpleAndProtectedGssapiNegotiation(this.ParentFrame, this.spnegoIndex.Value, this.PacketEndIndex);
                    }
                    else if (this.ntlmsspIndex.HasValue)
                    {
                        iteratorVariable0 = new NtlmSspPacket(this.ParentFrame, this.ntlmsspIndex.Value, this.PacketEndIndex);
                    }
                }
                catch (Exception)
                {
                }
                if (iteratorVariable0 != null)
                {
                    yield return iteratorVariable0;
                    foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                    {
                        yield return iteratorVariable1;
                    }
                }
            }

        }

        internal class SetupAndXRequest : CifsPacket.AbstractSmbCommand
        {
            private string accountName;
            private string accountPassword;
            private string nativeLanManager;
            private string nativeOs;
            private string primaryDomain;

            internal SetupAndXRequest(CifsPacket parentCifsPacket) : base(parentCifsPacket, "CIFS Setup AndX Request")
            {
                this.nativeOs = null;
                this.nativeLanManager = null;
                this.accountName = null;
                this.primaryDomain = null;
                this.accountPassword = null;
                if (parentCifsPacket.WordCount == 10)
                {
                    ushort bytesToRead = ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 14, true);
                    int dataIndex = parentCifsPacket.parametersStartIndex + 0x16;
                    this.accountPassword = ByteConverter.ReadString(parentCifsPacket.ParentFrame.Data, ref dataIndex, bytesToRead, false, true);
                    this.accountName = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.primaryDomain = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeOs = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                }
                else if (parentCifsPacket.WordCount == 12)
                {
                    base.SecurityBlobLength = ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 14, true);
                    int num3 = (parentCifsPacket.parametersStartIndex + 0x1a) + base.SecurityBlobLength;
                    if (parentCifsPacket.Flags2UnicodeStrings && (((num3 - parentCifsPacket.PacketStartIndex) % 2) == 1))
                    {
                        num3++;
                    }
                    this.nativeOs = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num3, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num3, parentCifsPacket.Flags2UnicodeStrings, true);
                }
                else if (parentCifsPacket.WordCount == 13)
                {
                    ushort nBytesToRead = ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 14, true);
                    ushort lenght = ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 0x10, true);
                    if (nBytesToRead > 0)
                    {
                        this.accountPassword = ByteConverter.ReadHexString(parentCifsPacket.ParentFrame.Data, nBytesToRead, parentCifsPacket.parametersStartIndex + 0x1c);
                    }
                    if (lenght > 0)
                    {
                        this.accountPassword = ByteConverter.ReadString(parentCifsPacket.ParentFrame.Data, (int) ((parentCifsPacket.parametersStartIndex + 0x1c) + nBytesToRead), lenght, true, false);
                        string str = this.accountPassword = ByteConverter.ReadHexString(parentCifsPacket.ParentFrame.Data, lenght, (parentCifsPacket.parametersStartIndex + 0x1c) + nBytesToRead);
                        this.accountPassword = str;
                    }
                    int num6 = ((parentCifsPacket.parametersStartIndex + 0x1c) + nBytesToRead) + lenght;
                    if (parentCifsPacket.Flags2UnicodeStrings && (((num6 - parentCifsPacket.PacketStartIndex) % 2) == 1))
                    {
                        num6++;
                    }
                    if (lenght > 0)
                    {
                        this.accountName = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, parentCifsPacket.Flags2UnicodeStrings, true);
                        this.primaryDomain = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, parentCifsPacket.Flags2UnicodeStrings, true);
                        this.nativeOs = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, parentCifsPacket.Flags2UnicodeStrings, true);
                        this.nativeLanManager = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, parentCifsPacket.Flags2UnicodeStrings, true);
                    }
                    else
                    {
                        this.accountName = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, false, true);
                        this.primaryDomain = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, false, true);
                        this.nativeOs = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, false, true);
                        this.nativeLanManager = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num6, false, true);
                    }
                }
                if (base.SecurityBlobLength > 0)
                {
                    base.SecurityBlobIndex = new int?((parentCifsPacket.parametersStartIndex + 2) + (parentCifsPacket.WordCount * 2));
                }
                if (!base.ParentFrame.QuickParse)
                {
                    if ((this.accountName != null) && (this.accountName.Length > 0))
                    {
                        base.Attributes.Add("Account Name", this.accountName);
                    }
                    if ((this.primaryDomain != null) && (this.primaryDomain.Length > 0))
                    {
                        base.Attributes.Add("Primary Domain", this.primaryDomain);
                    }
                    if ((this.nativeOs != null) && (this.nativeOs.Length > 0))
                    {
                        base.Attributes.Add("Native OS", this.nativeOs);
                    }
                    if ((this.nativeLanManager != null) && (this.nativeLanManager.Length > 0))
                    {
                        base.Attributes.Add("Native LAN Manager", this.nativeLanManager);
                    }
                }
            }

            internal string AccountName
            {
                get
                {
                    return this.accountName;
                }
            }

            internal string AccountPassword
            {
                get
                {
                    return this.accountPassword;
                }
            }

            internal string NativeLanManager
            {
                get
                {
                    return this.nativeLanManager;
                }
            }

            internal string NativeOs
            {
                get
                {
                    return this.nativeOs;
                }
            }

            internal string PrimaryDomain
            {
                get
                {
                    return this.primaryDomain;
                }
            }
        }

        internal class SetupAndXResponse : CifsPacket.AbstractSmbCommand
        {
            private string nativeLanManager;
            private string nativeOs;
            private string primaryDomain;

            internal SetupAndXResponse(CifsPacket parentCifsPacket) : base(parentCifsPacket, "CIFS Setup AndX Response")
            {
                this.nativeOs = null;
                this.nativeLanManager = null;
                this.primaryDomain = null;
                if (parentCifsPacket.WordCount == 3)
                {
                    int dataIndex = parentCifsPacket.parametersStartIndex + 9;
                    this.nativeOs = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.primaryDomain = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref dataIndex, parentCifsPacket.Flags2UnicodeStrings, true);
                }
                else if (parentCifsPacket.WordCount == 4)
                {
                    base.SecurityBlobLength = ByteConverter.ToUInt16(parentCifsPacket.ParentFrame.Data, parentCifsPacket.parametersStartIndex + 6, true);
                    int num2 = (parentCifsPacket.parametersStartIndex + 10) + base.SecurityBlobLength;
                    if (parentCifsPacket.Flags2UnicodeStrings && (((num2 - parentCifsPacket.PacketStartIndex) % 2) == 1))
                    {
                        num2++;
                    }
                    this.nativeOs = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num2, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.nativeLanManager = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num2, parentCifsPacket.Flags2UnicodeStrings, true);
                    this.primaryDomain = ByteConverter.ReadNullTerminatedString(parentCifsPacket.ParentFrame.Data, ref num2, parentCifsPacket.Flags2UnicodeStrings, true);
                }
                if (base.SecurityBlobLength > 0)
                {
                    base.SecurityBlobIndex = new int?((parentCifsPacket.parametersStartIndex + 2) + (parentCifsPacket.WordCount * 2));
                }
            }

            internal string NativeLanManager
            {
                get
                {
                    return this.nativeLanManager;
                }
            }

            internal string NativeOs
            {
                get
                {
                    return this.nativeOs;
                }
            }
        }

        internal class SimpleAndProtectedGssapiNegotiation : AbstractPacket
        {
            private byte basicTokenType;
            private int? ntlmsspIndex;
            private int ntlmsspLength;

            internal SimpleAndProtectedGssapiNegotiation(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "SPNEGO")
            {
                this.basicTokenType = parentFrame.Data[packetStartIndex];
                this.ntlmsspIndex = null;
                this.ntlmsspLength = 0;
                int index = packetStartIndex;
                int sequenceElementLength = this.GetSequenceElementLength(parentFrame.Data, ref index);
                if (parentFrame.Data[index] != 0x30)
                {
                    throw new Exception("Not a valid SPNEGO packet format");
                }
                if (this.GetSequenceElementLength(parentFrame.Data, ref index) >= sequenceElementLength)
                {
                    throw new Exception("SPNEGO Packet length is not larger than Constructed Sequence length");
                }
                while ((index < packetEndIndex) && (index < (packetStartIndex + sequenceElementLength)))
                {
                    byte num4 = parentFrame.Data[index];
                    int num5 = this.GetSequenceElementLength(parentFrame.Data, ref index);
                    if ((num4 & 240) == 160)
                    {
                        int num6 = num4 & 15;
                        if (!base.ParentFrame.QuickParse)
                        {
                            base.Attributes.Add("SPNEGO Element " + num6 + " length", num5.ToString());
                        }
                    }
                    else if (num4 == 4)
                    {
                        if (parentFrame.Data[index] == 0x4e)
                        {
                            this.ntlmsspIndex = new int?(index);
                            this.ntlmsspLength = num5;
                        }
                        index += num5;
                        return;
                    }
                }
            }

            private int GetSequenceElementLength(byte[] data, ref int index)
            {
                index++;
                int num = 0;
                if (data[index] >= 0x80)
                {
                    int nBytes = data[index] & 15;
                    index++;
                    num = (int) ByteConverter.ToUInt32(data, index, nBytes, true);
                    index += nBytes;
                    return num;
                }
                num = data[index];
                index++;
                return num;
            }

            public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
            {
                if (includeSelfReference)
                {
                    yield return this;
                }
                if (this.ntlmsspIndex.HasValue && (this.ntlmsspLength > 0))
                {
                    NtlmSspPacket iteratorVariable0 = null;
                    try
                    {
                        iteratorVariable0 = new NtlmSspPacket(this.ParentFrame, this.ntlmsspIndex.Value, (this.ntlmsspIndex.Value + this.ntlmsspLength) - 1);
                    }
                    catch (Exception exception)
                    {
                        if (!this.ParentFrame.QuickParse)
                        {
                            this.ParentFrame.Errors.Add(new Frame.Error(this.ParentFrame, this.ntlmsspIndex.Value, (this.ntlmsspIndex.Value + this.ntlmsspLength) - 1, exception.Message));
                        }
                        goto Label_014A;
                    }
                    yield return iteratorVariable0;
                }
            Label_014A:;
            }


            internal enum BasicTokenTypes : byte
            {
                NegTokenInit = 160,
                NegTokenTarg = 0xa1
            }
        }
    }
}

