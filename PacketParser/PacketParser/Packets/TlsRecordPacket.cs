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

    internal class TlsRecordPacket : AbstractPacket
    {
        private ContentTypes contentType;
        private ushort length;
        private byte versionMajor;
        private byte versionMinor;

        internal TlsRecordPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "TLS Record")
        {
            this.contentType = (ContentTypes) parentFrame.Data[packetStartIndex];
            this.versionMajor = parentFrame.Data[packetStartIndex + 1];
            this.versionMinor = parentFrame.Data[packetStartIndex + 2];
            this.length = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 3);
            base.PacketEndIndex = Math.Min(((packetStartIndex + 5) + this.length) - 1, base.PacketEndIndex);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Content Type", this.contentType.ToString());
                base.Attributes.Add("TLS Version major", this.versionMajor.ToString());
                base.Attributes.Add("TLS Version minor", this.versionMinor.ToString());
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if (this.contentType == ContentTypes.Handshake)
            {
                int packetStartIndex = this.PacketStartIndex + 5;
                while (packetStartIndex < this.PacketEndIndex)
                {
                    AbstractPacket iteratorVariable1;
                    if (this.contentType == ContentTypes.Handshake)
                    {
                        try
                        {
                            iteratorVariable1 = new HandshakePacket(this.ParentFrame, packetStartIndex, this.PacketEndIndex);
                        }
                        catch
                        {
                            iteratorVariable1 = new RawPacket(this.ParentFrame, packetStartIndex, this.PacketEndIndex);
                        }
                    }
                    else
                    {
                        iteratorVariable1 = new RawPacket(this.ParentFrame, packetStartIndex, this.PacketEndIndex);
                    }
                    packetStartIndex = iteratorVariable1.PacketEndIndex + 1;
                    yield return iteratorVariable1;
                }
            }
        }

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            if (!Enum.IsDefined(typeof(ContentTypes), parentFrame.Data[packetStartIndex]))
            {
                return false;
            }
            if ((ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 3) + 5) > ((packetEndIndex - packetStartIndex) + 1))
            {
                return false;
            }
            try
            {
                result = new TlsRecordPacket(parentFrame, packetStartIndex, packetEndIndex);
            }
            catch
            {
                result = null;
            }
            if (result == null)
            {
                return false;
            }
            return true;
        }

        internal ushort Length
        {
            get
            {
                return this.length;
            }
        }

        internal bool TlsRecordIsComplete
        {
            get
            {
                return (((base.PacketEndIndex - base.PacketStartIndex) + 1) == (5 + this.length));
            }
        }


        internal enum ContentTypes : byte
        {
            Alert = 0x15,
            Application = 0x17,
            ChangeCipherSpec = 20,
            Handshake = 0x16
        }

        internal class HandshakePacket : AbstractPacket
        {
            private List<byte[]> certificateList;
            private uint messageLength;
            private MessageTypes messageType;

            internal HandshakePacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "TLS Handshake Protocol")
            {
                this.certificateList = new List<byte[]>();
                this.messageType = (MessageTypes) parentFrame.Data[packetStartIndex];
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Message Type", this.messageType.ToString());
                }
                this.messageLength = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 1, 3);
                base.PacketEndIndex = (int) (((packetStartIndex + 4) + this.messageLength) - 1);
                if (this.messageType == MessageTypes.Certificate)
                {
                    byte[] buffer;
                    uint num = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 4, 3);
                    int num2 = packetStartIndex + 7;
                    for (int i = 0; i < num; i += buffer.Length)
                    {
                        uint num4 = ByteConverter.ToUInt32(parentFrame.Data, num2 + i, 3);
                        i += 3;
                        buffer = new byte[num4];
                        Array.Copy(parentFrame.Data, num2 + i, buffer, 0, buffer.Length);
                        this.certificateList.Add(buffer);
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

            internal List<byte[]> CertificateList
            {
                get
                {
                    return this.certificateList;
                }
            }

            internal uint MessageLenght
            {
                get
                {
                    return this.messageLength;
                }
            }

            internal MessageTypes MessageType
            {
                get
                {
                    return this.messageType;
                }
            }


            internal enum MessageTypes : byte
            {
                Certificate = 11,
                CertificateRequest = 13,
                CertificateVerify = 15,
                ClientHello = 1,
                ClientKeyExchange = 0x10,
                Finished = 20,
                HelloRequest = 0,
                ServerHello = 2,
                ServerHelloDone = 14,
                ServerKeyExchange = 12
            }
        }
    }
}

