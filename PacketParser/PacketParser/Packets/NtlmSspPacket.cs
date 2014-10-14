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

    internal class NtlmSspPacket : AbstractPacket
    {
        private string domainName;
        private string hostName;
        private string lanManagerResponse;
        private string ntlmChallenge;
        private string ntlmResponse;
        private string userName;

        internal NtlmSspPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "NTLMSSP")
        {
            this.domainName = null;
            this.userName = null;
            this.hostName = null;
            this.lanManagerResponse = null;
            this.ntlmResponse = null;
            int dataIndex = packetStartIndex;
            if (ByteConverter.ReadNullTerminatedString(parentFrame.Data, ref dataIndex) != "NTLMSSP")
            {
                throw new Exception("Expected NTLMSSP signature string missing!");
            }
            uint num2 = ByteConverter.ToUInt32(parentFrame.Data, dataIndex);
            dataIndex += 4;
            switch (num2)
            {
                case 0x1000000:
                    return;

                case 0x2000000:
                {
                    bool unicodeData = false;
                    SecurityBuffer buffer = new SecurityBuffer(parentFrame.Data, ref dataIndex);
                    if (buffer.length > 0)
                    {
                        unicodeData = parentFrame.Data[(int) ((IntPtr) (((packetStartIndex + buffer.offset) + buffer.length) - 1))] == 0;
                    }
                    dataIndex += 4;
                    this.ntlmChallenge = ByteConverter.ReadHexString(parentFrame.Data, 8, dataIndex);
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.Attributes.Add("NTLM Challenge", this.ntlmChallenge);
                    }
                    if (buffer.length > 0)
                    {
                        dataIndex = packetStartIndex + ((int) buffer.offset);
                        this.domainName = ByteConverter.ReadString(parentFrame.Data, ref dataIndex, buffer.length, unicodeData, true);
                        if (!base.ParentFrame.QuickParse)
                        {
                            base.Attributes.Add("Domain Name", this.domainName);
                            return;
                        }
                    }
                    break;
                }
                default:
                    if (num2 == 0x3000000)
                    {
                        bool flag2 = false;
                        SecurityBuffer buffer2 = new SecurityBuffer(parentFrame.Data, ref dataIndex);
                        SecurityBuffer buffer3 = new SecurityBuffer(parentFrame.Data, ref dataIndex);
                        SecurityBuffer buffer4 = new SecurityBuffer(parentFrame.Data, ref dataIndex);
                        SecurityBuffer buffer5 = new SecurityBuffer(parentFrame.Data, ref dataIndex);
                        SecurityBuffer buffer6 = new SecurityBuffer(parentFrame.Data, ref dataIndex);
                        new SecurityBuffer(parentFrame.Data, ref dataIndex);
                        if (buffer4.length > 0)
                        {
                            flag2 = parentFrame.Data[(int) ((IntPtr) (((packetStartIndex + buffer4.offset) + buffer4.length) - 1))] == 0;
                        }
                        else if (buffer5.length > 0)
                        {
                            flag2 = parentFrame.Data[(int) ((IntPtr) (((packetStartIndex + buffer5.offset) + buffer5.length) - 1))] == 0;
                        }
                        else if (buffer6.length > 0)
                        {
                            flag2 = parentFrame.Data[(int) ((IntPtr) (((packetStartIndex + buffer6.offset) + buffer6.length) - 1))] == 0;
                        }
                        if (buffer2.length > 0)
                        {
                            byte[] bufferData = buffer2.GetBufferData(parentFrame.Data, packetStartIndex);
                            this.lanManagerResponse = ByteConverter.ReadHexString(bufferData, bufferData.Length);
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add("LAN Manager Response", this.lanManagerResponse);
                            }
                        }
                        if (buffer3.length > 0)
                        {
                            byte[] data = buffer3.GetBufferData(parentFrame.Data, base.PacketStartIndex);
                            this.ntlmResponse = ByteConverter.ReadHexString(data, data.Length);
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add("NTLM Response", this.ntlmResponse);
                            }
                        }
                        if (buffer4.length > 0)
                        {
                            dataIndex = packetStartIndex + ((int) buffer4.offset);
                            this.domainName = ByteConverter.ReadString(parentFrame.Data, ref dataIndex, buffer4.length, flag2, true);
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add("Domain Name", this.domainName);
                            }
                        }
                        if (buffer5.length > 0)
                        {
                            dataIndex = packetStartIndex + ((int) buffer5.offset);
                            this.userName = ByteConverter.ReadString(parentFrame.Data, ref dataIndex, buffer5.length, flag2, true);
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add("User Name", this.userName);
                            }
                        }
                        if (buffer6.length > 0)
                        {
                            dataIndex = packetStartIndex + ((int) buffer6.offset);
                            this.hostName = ByteConverter.ReadString(parentFrame.Data, ref dataIndex, buffer6.length, flag2, true);
                            if (!base.ParentFrame.QuickParse)
                            {
                                base.Attributes.Add("Host Name", this.hostName);
                            }
                        }
                    }
                    break;
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

        internal string DomainName
        {
            get
            {
                return this.domainName;
            }
        }

        internal string HostName
        {
            get
            {
                return this.hostName;
            }
        }

        internal string LanManagerResponse
        {
            get
            {
                return this.lanManagerResponse;
            }
        }

        internal string NtlmChallenge
        {
            get
            {
                return this.ntlmChallenge;
            }
        }

        internal string NtlmResponse
        {
            get
            {
                return this.ntlmResponse;
            }
        }

        internal string UserName
        {
            get
            {
                return this.userName;
            }
        }


        internal enum NtlmMessageTypes : uint
        {
            Authentication = 0x3000000,
            Challenge = 0x2000000,
            Negotiate = 0x1000000
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SecurityBuffer
        {
            internal ushort length;
            internal ushort lengthAllocated;
            internal uint offset;
            internal SecurityBuffer(byte[] data, ref int dataOffset)
            {
                this.length = ByteConverter.ToUInt16(data, dataOffset, true);
                dataOffset += 2;
                this.lengthAllocated = ByteConverter.ToUInt16(data, dataOffset, true);
                dataOffset += 2;
                this.offset = ByteConverter.ToUInt32(data, dataOffset, 4, true);
                dataOffset += 4;
            }

            internal byte[] GetBufferData(byte[] frameData, int packetStartIndex)
            {
                byte[] destinationArray = new byte[this.length];
                Array.Copy(frameData, packetStartIndex + this.offset, destinationArray, 0L, (long) this.length);
                return destinationArray;
            }
        }
    }
}

