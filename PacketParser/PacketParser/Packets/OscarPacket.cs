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

    internal class OscarPacket : AbstractPacket, ISessionPacket
    {
        private int bytesToParse;
        private string destinationLoginId;
        private string imText;
        private string sourceLoginId;
        private DateTime sourceUserSince;

        internal OscarPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "OSCAR Instant Messaging Protocol")
        {
            this.sourceUserSince = DateTime.MinValue;
            byte num1 = parentFrame.Data[base.PacketStartIndex];
            byte num = parentFrame.Data[base.PacketStartIndex + 1];
            ushort num2 = ByteConverter.ToUInt16(parentFrame.Data, base.PacketStartIndex + 4);
            this.bytesToParse = 6 + num2;
            if (base.PacketLength < num2)
            {
                throw new Exception("Packet is not complete, wait for more TCP segments");
            }
            int startIndex = base.PacketStartIndex + 6;
            switch (num)
            {
                case 1:
                    ushort num5;
                    ByteConverter.ToUInt32(parentFrame.Data, startIndex);
                    for (startIndex += 4; (startIndex < packetEndIndex) && (startIndex < (base.PacketStartIndex + this.bytesToParse)); startIndex += num5)
                    {
                        ushort num4 = ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                        startIndex += 2;
                        num5 = ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                        startIndex += 2;
                        if (Enum.IsDefined(typeof(SignonTags), num4))
                        {
                            string str = ByteConverter.ReadHexString(parentFrame.Data, num5, startIndex);
                            string str2 = ByteConverter.ReadString(parentFrame.Data, startIndex, num5);
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add(((SignonTags) num4).ToString(), str + " (" + str2 + ")");
                            }
                        }
                    }
                    break;

                case 2:
                {
                    ushort num6 = ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                    startIndex += 2;
                    ushort num7 = ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                    startIndex += 2;
                    ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                    startIndex += 2;
                    ByteConverter.ToUInt32(parentFrame.Data, startIndex);
                    startIndex += 4;
                    if (num6 == 4)
                    {
                        switch (num7)
                        {
                            case 6:
                                startIndex += 8;
                                ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                                startIndex += 2;
                                this.destinationLoginId = ByteConverter.ReadLengthValueString(parentFrame.Data, ref startIndex, 1);
                                if (!base.ParentFrame.QuickParse)
                                {
                                    base.Attributes.Add("Destination User", this.destinationLoginId);
                                }
                                while ((startIndex < parentFrame.Data.Length) && (startIndex < packetEndIndex))
                                {
                                    if (startIndex >= (base.PacketStartIndex + this.bytesToParse))
                                    {
                                        return;
                                    }
                                    TagLengthValue value2 = new TagLengthValue(parentFrame.Data, ref startIndex);
                                    if (value2.Tag == 2)
                                    {
                                        int offset = 0;
                                        while (offset < value2.Length)
                                        {
                                            TagLengthValue value3 = new TagLengthValue(value2.Value, ref offset);
                                            if (value3.Tag == 0x101)
                                            {
                                                ByteConverter.ToUInt16(value3.Value, 0);
                                                ByteConverter.ToUInt16(value3.Value, 2);
                                                this.imText = ByteConverter.ReadString(value3.Value, 4, value3.Length - 4);
                                                if (!base.ParentFrame.QuickParse)
                                                {
                                                    base.Attributes.Add("IM Text", this.imText);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;

                            case 7:
                            {
                                startIndex += 8;
                                ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                                startIndex += 2;
                                this.sourceLoginId = ByteConverter.ReadLengthValueString(parentFrame.Data, ref startIndex, 1);
                                ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                                startIndex += 2;
                                uint num9 = ByteConverter.ToUInt16(parentFrame.Data, startIndex);
                                startIndex += 2;
                                for (int i = 0; i < num9; i++)
                                {
                                    new TagLengthValue(parentFrame.Data, ref startIndex);
                                }
                                while (((startIndex < parentFrame.Data.Length) && (startIndex < packetEndIndex)) && (startIndex < (base.PacketStartIndex + this.bytesToParse)))
                                {
                                    TagLengthValue value4 = new TagLengthValue(parentFrame.Data, ref startIndex);
                                    if (value4.Tag == 2)
                                    {
                                        int num11 = 0;
                                        while (num11 < value4.Length)
                                        {
                                            TagLengthValue value5 = new TagLengthValue(value4.Value, ref num11);
                                            if (value5.Tag == 0x101)
                                            {
                                                ByteConverter.ToUInt16(value5.Value, 0);
                                                ByteConverter.ToUInt16(value5.Value, 2);
                                                this.imText = ByteConverter.ReadString(value5.Value, 4, value5.Length - 4);
                                                if (!base.ParentFrame.QuickParse)
                                                {
                                                    base.Attributes.Add("IM Text", this.imText);
                                                }
                                            }
                                        }
                                    }
                                }
                                break;
                            }
                        }
                    }
                    break;
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

        private void ReadIcbmTags()
        {
        }

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            try
            {
                if (parentFrame.Data[packetStartIndex] != 0x2a)
                {
                    return false;
                }
                if (ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 4) > ((packetEndIndex - packetStartIndex) - 5))
                {
                    return false;
                }
                result = new OscarPacket(parentFrame, packetStartIndex, packetEndIndex);
            }
            catch
            {
                return false;
            }
            return true;
        }

        internal int BytesParsed
        {
            get
            {
                return this.bytesToParse;
            }
        }

        internal string DestinationLoginId
        {
            get
            {
                return this.destinationLoginId;
            }
        }

        internal string ImText
        {
            get
            {
                return this.imText;
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
                throw new Exception("The method or operation is not implemented.");
            }
        }

        internal string SourceLoginId
        {
            get
            {
                return this.sourceLoginId;
            }
        }

        private enum FrameTypes : byte
        {
            Data = 2,
            Error = 3,
            KeepAlive = 5,
            SignOff = 4,
            SignOn = 1
        }

        private enum SignonTags : ushort
        {
            BuildNum = 0x1a,
            ClientName = 3,
            ClientReconnect = 0x94,
            LoginCookie = 6,
            MajorVersion = 0x17,
            MinorVersion = 0x18,
            MulticonnFlags = 0x4a,
            PointVersion = 0x19
        }

        internal class TagLengthValue
        {
            private ushort length;
            private ushort tag;
            private byte[] value;

            internal TagLengthValue(byte[] data, ref int offset)
            {
                this.tag = ByteConverter.ToUInt16(data, offset);
                offset += 2;
                this.length = ByteConverter.ToUInt16(data, offset);
                offset += 2;
                this.value = new byte[this.length];
                Array.Copy(data, offset, this.value, 0, this.length);
                offset += this.length;
            }

            internal ushort Length
            {
                get
                {
                    return this.length;
                }
            }

            internal ushort Tag
            {
                get
                {
                    return this.tag;
                }
            }

            internal byte[] Value
            {
                get
                {
                    return this.value;
                }
            }

            internal string ValueString
            {
                get
                {
                    return ByteConverter.ReadString(this.value);
                }
            }

            internal enum IcbmImDataTag : ushort
            {
                IM_CAPABILITIES = 0x501,
                IM_TEXT = 0x101,
                MIME_ARRAY = 0xd01
            }

            internal enum IcbmTag : ushort
            {
                ANONYMOUS = 0x18,
                AUTO_RESPONSE = 4,
                BART = 13,
                DATA = 5,
                FRIENDLY_NAME = 0x17,
                HOST_IM_ARGS = 0x11,
                HOST_IM_ID = 0x10,
                IM_DATA = 2,
                REQUEST_HOST_ACK = 3,
                SEND_TIME = 0x16,
                STORE = 6,
                WANT_EVENTS = 11,
                WIDGET_NAME = 0x19
            }

            internal enum OserviceNickInfoTag : ushort
            {
                AWAY_TIME = 0x27,
                BART_INFO = 0x1d,
                BUDDYFEED_TIME = 0x23,
                CAPS = 13,
                GEO_COUNTRY = 0x2a,
                IDLE_TIME = 4,
                MEMBER_SINCE = 5,
                MY_INSTANCE_NUM = 20,
                NICK_FLAGS = 1,
                NICK_FLAGS2 = 0x1f,
                ONLINE_TIME = 15,
                REALIPADDRESS = 10,
                SHORT_CAPS = 0x19,
                SIG_TIME = 0x26,
                SIGNON_TOD = 3
            }
        }
    }
}

