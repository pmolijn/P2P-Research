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

    internal class HpSwitchProtocolPacket : AbstractPacket
    {
        private byte type;
        private byte version;

        internal HpSwitchProtocolPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "HP Switch Protocol")
        {
            this.version = parentFrame.Data[base.PacketStartIndex];
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Version", "0x" + this.version.ToString("X2"));
            }
            this.type = parentFrame.Data[base.PacketStartIndex + 1];
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Type", "0x" + this.type.ToString("X2"));
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            int iteratorVariable0 = 0;
            int packetStartIndex = this.PacketStartIndex + 2;
            while ((packetStartIndex < this.PacketEndIndex) && (iteratorVariable0 < 20))
            {
                HpSwField iteratorVariable2 = new HpSwField(this.ParentFrame, packetStartIndex, this.PacketEndIndex);
                packetStartIndex += iteratorVariable2.PacketLength;
                iteratorVariable0++;
                yield return iteratorVariable2;
            }
        }


        internal class HpSwField : AbstractPacket
        {
            private byte typeByte;
            private byte[] valueBytes;
            private byte valueLength;

            internal HpSwField(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "HP Switch Protocol Field")
            {
                this.typeByte = parentFrame.Data[base.PacketStartIndex];
                this.valueLength = parentFrame.Data[base.PacketStartIndex + 1];
                this.valueBytes = new byte[Math.Min((int) this.valueLength, base.PacketLength - 2)];
                Array.Copy(parentFrame.Data, base.PacketStartIndex + 2, this.valueBytes, 0, this.valueBytes.Length);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Field 0x" + this.typeByte.ToString("X2"), this.ValueString);
                }
                base.PacketEndIndex = (base.PacketStartIndex + 1) + this.valueLength;
            }

            public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
            {
                if (!includeSelfReference)
                {
                    yield break;
                }
                yield return this;
            }

            internal byte TypeByte
            {
                get
                {
                    return this.typeByte;
                }
            }

            internal byte[] ValueBytes
            {
                get
                {
                    return this.valueBytes;
                }
            }

            internal string ValueString
            {
                get
                {
                    int dataIndex = 0;
                    return ByteConverter.ReadNullTerminatedString(this.valueBytes, ref dataIndex, false, false, this.valueLength);
                }
            }


            internal enum FieldType : byte
            {
                Config = 3,
                DeviceName = 1,
                IpAddress = 5,
                MacAddress = 14,
                Version = 2
            }
        }
    }
}

