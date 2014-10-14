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

    internal class SpotifyKeyExchangePacket : AbstractPacket, ISessionPacket
    {
        private byte[] blob;
        private uint clientID;
        private byte clientOS;
        private uint clientRevision;
        private bool clientToServer;
        private const ushort CONTENT_END_USHORT = 320;
        private ushort keyExchangePacketLength;
        private byte[] publicKey;
        private byte[] random;
        private byte[] salt;
        private string username;
        private ushort version;

        private SpotifyKeyExchangePacket(Frame parentFrame, int packetStartIndex, int packetEndIndex, bool clientToServer) : base(parentFrame, packetStartIndex, packetEndIndex, "Spotify Key Exchange")
        {
            this.clientToServer = clientToServer;
            this.random = new byte[0x10];
            this.publicKey = new byte[0x60];
            if (clientToServer)
            {
                this.version = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex);
                if (this.version != 2)
                {
                    if (this.version == 3)
                    {
                        this.keyExchangePacketLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
                        if (((packetStartIndex + this.keyExchangePacketLength) - 1) < packetEndIndex)
                        {
                            base.PacketEndIndex = (packetStartIndex + this.keyExchangePacketLength) - 1;
                        }
                        this.clientRevision = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 12);
                        if (!base.ParentFrame.QuickParse)
                        {
                            base.Attributes.Add("Client Revision", this.clientRevision.ToString());
                        }
                        this.clientID = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 0x18);
                        if (!base.ParentFrame.QuickParse)
                        {
                            base.Attributes.Add("Client ID", "0x" + this.clientID.ToString("X2"));
                        }
                        Array.Copy(parentFrame.Data, packetStartIndex + 0x30, this.publicKey, 0, 0x60);
                        byte num2 = parentFrame.Data[packetStartIndex + 0x110];
                        byte lenght = parentFrame.Data[packetStartIndex + 0x111];
                        this.username = ByteConverter.ReadString(parentFrame.Data, (packetStartIndex + 0x114) + num2, lenght);
                        if (!base.ParentFrame.QuickParse)
                        {
                            base.Attributes.Add("Client Username", this.username);
                        }
                    }
                }
                else
                {
                    this.keyExchangePacketLength = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
                    if (((packetStartIndex + this.keyExchangePacketLength) - 1) < packetEndIndex)
                    {
                        base.PacketEndIndex = (packetStartIndex + this.keyExchangePacketLength) - 1;
                    }
                    this.clientOS = parentFrame.Data[packetStartIndex + 4];
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.Attributes.Add("Client OS", this.ClientOperatingSystem);
                    }
                    this.clientID = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 5);
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.Attributes.Add("Client ID", "0x" + this.clientID.ToString("X2"));
                    }
                    this.clientRevision = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 9);
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.Attributes.Add("Client Revision", this.clientRevision.ToString());
                    }
                    Array.Copy(parentFrame.Data, packetStartIndex + 13, this.random, 0, 0x10);
                    Array.Copy(parentFrame.Data, packetStartIndex + 0x1d, this.publicKey, 0, 0x60);
                    this.blob = new byte[0x80];
                    Array.Copy(parentFrame.Data, packetStartIndex + 0x7d, this.blob, 0, 0x80);
                    byte num = parentFrame.Data[packetStartIndex + 0xfd];
                    this.username = ByteConverter.ReadString(parentFrame.Data, packetStartIndex + 0xfe, num);
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.Attributes.Add("Client Username", this.username);
                    }
                    if (ByteConverter.ToUInt16(parentFrame.Data, (packetStartIndex + 0xfe) + num) != 320)
                    {
                        throw new Exception("Not a valid SpotifyKeyExchangePacket");
                    }
                }
            }
            else
            {
                int num4 = -1;
                if ((((packetStartIndex + 380) + parentFrame.Data[packetStartIndex + 0x11]) <= packetEndIndex) && (((packetEndIndex - packetStartIndex) + 1) == ((380 + parentFrame.Data[packetStartIndex + 0x11]) + parentFrame.Data[(packetStartIndex + 380) + parentFrame.Data[packetStartIndex + 0x11]])))
                {
                    num4 = 2;
                }
                else if ((((packetStartIndex + 0x182) + 1) <= packetEndIndex) && (((packetEndIndex - packetStartIndex) + 1) == ((((((0x184 + parentFrame.Data[packetStartIndex + 0x17a]) + parentFrame.Data[packetStartIndex + 0x17b]) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 380)) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x17e)) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x180)) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x182))))
                {
                    num4 = 3;
                }
                if (num4 == 2)
                {
                    Array.Copy(parentFrame.Data, packetStartIndex, this.random, 0, 0x10);
                    byte num5 = parentFrame.Data[base.PacketStartIndex + 0x11];
                    this.username = ByteConverter.ReadString(parentFrame.Data, packetStartIndex + 0x12, num5);
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.Attributes.Add("Client Username", this.username);
                    }
                    Array.Copy(parentFrame.Data, (packetStartIndex + 0x12) + num5, this.publicKey, 0, 0x60);
                    this.salt = new byte[10];
                    Array.Copy(parentFrame.Data, (packetStartIndex + 370) + num5, this.salt, 0, 10);
                }
                else if (num4 == 3)
                {
                    Array.Copy(parentFrame.Data, packetStartIndex, this.random, 0, 0x10);
                    Array.Copy(parentFrame.Data, packetStartIndex + 0x10, this.publicKey, 0, 0x60);
                    byte num6 = parentFrame.Data[packetStartIndex + 0x17a];
                    byte num7 = parentFrame.Data[base.PacketStartIndex + 0x17b];
                    this.username = ByteConverter.ReadString(parentFrame.Data, (packetStartIndex + 0x184) + num6, num7);
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.Attributes.Add("Client Username", this.username);
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

        public static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, bool clientToServer, out AbstractPacket result)
        {
            result = null;
            if (clientToServer)
            {
                if (ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2) != ((packetEndIndex - packetStartIndex) + 1))
                {
                    return false;
                }
                ushort num2 = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex);
                if ((num2 != 2) && (num2 != 3))
                {
                    return false;
                }
                try
                {
                    result = new SpotifyKeyExchangePacket(parentFrame, packetStartIndex, packetEndIndex, clientToServer);
                }
                catch
                {
                    result = null;
                }
            }
            else
            {
                if (((packetEndIndex - packetStartIndex) + 1) < 0x17d)
                {
                    return false;
                }
                if (((packetEndIndex - packetStartIndex) + 1) > 0x40380)
                {
                    return false;
                }
                int num3 = -1;
                if (((packetStartIndex + 380) + parentFrame.Data[packetStartIndex + 0x11]) <= packetEndIndex)
                {
                    num3 = (380 + parentFrame.Data[packetStartIndex + 0x11]) + parentFrame.Data[(packetStartIndex + 380) + parentFrame.Data[packetStartIndex + 0x11]];
                }
                int num4 = -1;
                if (((packetStartIndex + 0x182) + 1) <= packetEndIndex)
                {
                    num4 = (((((0x184 + parentFrame.Data[packetStartIndex + 0x17a]) + parentFrame.Data[packetStartIndex + 0x17b]) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 380)) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x17e)) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x180)) + ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 0x182);
                }
                if ((((packetEndIndex - packetStartIndex) + 1) != num3) && (((packetEndIndex - packetStartIndex) + 1) != num4))
                {
                    return false;
                }
                try
                {
                    result = new SpotifyKeyExchangePacket(parentFrame, packetStartIndex, packetEndIndex, clientToServer);
                }
                catch
                {
                    result = null;
                }
            }
            if (result == null)
            {
                return false;
            }
            return true;
        }

        public string ClientOperatingSystem
        {
            get
            {
                if (this.clientOS == 0)
                {
                    return "Windows (0x00)";
                }
                if (this.clientOS == 1)
                {
                    return "Mac OS X (0x01)";
                }
                return ("(0x" + this.clientOS.ToString("X2") + ")");
            }
        }

        public string ClientUsername
        {
            get
            {
                return this.username;
            }
        }

        public bool IsClientToServer
        {
            get
            {
                return this.clientToServer;
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
                return base.PacketLength;
            }
        }

        public string PublicKeyHexString
        {
            get
            {
                return ByteConverter.ReadHexString(this.publicKey, this.publicKey.Length);
            }
        }
    }
}

