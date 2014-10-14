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

    internal class TabularDataStreamPacket : AbstractPacket, ISessionPacket
    {
        private string appname;
        private string clientHostname;
        private string databaseName;
        private bool isLastPacket;
        private string libraryName;
        private ushort packetSize;
        private byte packetType;
        private string password;
        private string query;
        private string serverHostname;
        private string username;

        internal TabularDataStreamPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Tabular Data Stream (SQL)")
        {
            this.packetType = parentFrame.Data[base.PacketStartIndex];
            this.isLastPacket = parentFrame.Data[base.PacketStartIndex + 1] == 1;
            this.packetSize = ByteConverter.ToUInt16(parentFrame.Data, base.PacketStartIndex + 2);
            int startIndex = (base.PacketStartIndex + 4) + 4;
            if (this.packetType == 1)
            {
                this.query = ByteConverter.ReadString(parentFrame.Data, startIndex, Math.Min((int) ((base.PacketEndIndex - startIndex) + 1), (int) (this.packetSize - 8)), true, true);
            }
            if (this.packetType == 0x10)
            {
                this.clientHostname = ByteConverter.ReadString(parentFrame.Data, (int) (startIndex + ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x24, true)), 2 * ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x26, true), true, true);
                this.username = ByteConverter.ReadString(parentFrame.Data, (int) (startIndex + ByteConverter.ToUInt16(parentFrame.Data, startIndex + 40, true)), 2 * ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x2a, true), true, true);
                int dataIndex = startIndex + ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x2c, true);
                this.password = ByteConverter.ReadString(parentFrame.Data, ref dataIndex, 2 * ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x2e, true), true, true, ByteConverter.Encoding.TDS_password);
                this.appname = ByteConverter.ReadString(parentFrame.Data, (int) (startIndex + ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x30, true)), 2 * ByteConverter.ToUInt16(parentFrame.Data, startIndex + 50, true), true, true);
                this.serverHostname = ByteConverter.ReadString(parentFrame.Data, (int) (startIndex + ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x34, true)), 2 * ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x36, true), true, true);
                this.libraryName = ByteConverter.ReadString(parentFrame.Data, (int) (startIndex + ByteConverter.ToUInt16(parentFrame.Data, startIndex + 60, true)), 2 * ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x3e, true), true, true);
                this.databaseName = ByteConverter.ReadString(parentFrame.Data, (int) (startIndex + ByteConverter.ToUInt16(parentFrame.Data, startIndex + 0x44, true)), 2 * ByteConverter.ToUInt16(parentFrame.Data, startIndex + 70, true), true, true);
                if (!base.ParentFrame.QuickParse)
                {
                    if (this.clientHostname.Length > 0)
                    {
                        base.Attributes.Add("SQL client hostname", this.clientHostname);
                    }
                    if (this.username.Length > 0)
                    {
                        base.Attributes.Add("SQL username", this.username);
                    }
                    if (this.password.Length > 0)
                    {
                        base.Attributes.Add("SQL password", this.password);
                    }
                    if (this.appname.Length > 0)
                    {
                        base.Attributes.Add("App name", this.appname);
                    }
                    if (this.serverHostname.Length > 0)
                    {
                        base.Attributes.Add("SQL server", this.serverHostname);
                    }
                    if (this.libraryName.Length > 0)
                    {
                        base.Attributes.Add("SQL library", this.libraryName);
                    }
                    if (this.databaseName.Length > 0)
                    {
                        base.Attributes.Add("Database name", this.databaseName);
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

        public string AppName
        {
            get
            {
                return this.appname;
            }
        }

        public string ClientHostname
        {
            get
            {
                return this.clientHostname;
            }
        }

        public string DatabaseName
        {
            get
            {
                return this.databaseName;
            }
        }

        public bool IsLastPacket
        {
            get
            {
                return this.IsLastPacket;
            }
        }

        public string LibraryName
        {
            get
            {
                return this.libraryName;
            }
        }

        public bool PacketHeaderIsComplete
        {
            get
            {
                return (base.PacketLength >= this.packetSize);
            }
        }

        public ushort PacketSize
        {
            get
            {
                return this.PacketSize;
            }
        }

        public byte PacketType
        {
            get
            {
                return this.packetType;
            }
        }

        public int ParsedBytesCount
        {
            get
            {
                return base.PacketLength;
            }
        }

        public string Password
        {
            get
            {
                return this.password;
            }
        }

        public string Query
        {
            get
            {
                return this.query;
            }
        }

        public string ServerHostname
        {
            get
            {
                return this.serverHostname;
            }
        }

        public string Username
        {
            get
            {
                return this.username;
            }
        }

        internal enum PacketTypes : byte
        {
            AttentionSignal = 6,
            BulkLoadData = 7,
            PreLoginMessage = 0x12,
            PreTds7Login = 2,
            RemoteProcedureCall = 3,
            SqlQuery = 1,
            SspiMessage = 0x11,
            TableResponse = 4,
            Tds7Login = 0x10,
            TransactionManagerRequest = 14
        }
    }
}

