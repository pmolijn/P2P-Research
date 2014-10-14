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

    internal class NetBiosSessionService : NetBiosPacket, ISessionPacket
    {
        private int length;
        private byte messageType;
        private bool raw;

        private NetBiosSessionService(Frame parentFrame, int packetStartIndex, int packetEndIndex, bool raw) : base(parentFrame, packetStartIndex, packetEndIndex, "NetBIOS Session Service")
        {
            this.messageType = parentFrame.Data[packetStartIndex];
            if ((this.messageType == 0x85) && ((packetEndIndex - packetStartIndex) == 3))
            {
                this.length = 0;
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Message", "NetBios Session Service session keep-alive");
                }
            }
            else
            {
                uint num = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex);
                if (raw)
                {
                    this.length = ((int) num) & 0xffffff;
                }
                else
                {
                    this.length = ((int) num) & 0x1ffff;
                }
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Length", this.length.ToString());
                }
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            if ((this.messageType == 0) && ((this.PacketStartIndex + 4) < this.PacketEndIndex))
            {
                AbstractPacket iteratorVariable0;
                try
                {
                    iteratorVariable0 = new CifsPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                }
                catch
                {
                    iteratorVariable0 = new RawPacket(this.ParentFrame, this.PacketStartIndex + 4, this.PacketEndIndex);
                }
                yield return iteratorVariable0;
                foreach (AbstractPacket iteratorVariable1 in iteratorVariable0.GetSubPackets(false))
                {
                    yield return iteratorVariable1;
                }
            }
        }

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            uint num2;
            result = null;
            bool raw = false;
            uint num = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex);
            if (num == 0x85000000)
            {
                result = new NetBiosSessionService(parentFrame, packetStartIndex, packetStartIndex + 3, raw);
                return true;
            }
            if ((num & 0xff000000) != 0)
            {
                return false;
            }
            if (raw)
            {
                num2 = num & 0xffffff;
            }
            else
            {
                num2 = num & 0x1ffff;
            }
            if (num2 == (((packetEndIndex - packetStartIndex) + 1) - 4))
            {
                result = new NetBiosSessionService(parentFrame, packetStartIndex, packetEndIndex, raw);
                return true;
            }
            if (num2 >= (((packetEndIndex - packetStartIndex) + 1) - 4))
            {
                return false;
            }
            byte num3 = parentFrame.Data[(int) ((IntPtr) ((packetStartIndex + num2) + 4))];
            if ((num3 != 0) && (num3 != 0x85))
            {
                return false;
            }
            result = new NetBiosSessionService(parentFrame, packetStartIndex, (packetStartIndex + ((int) num2)) + 3, raw);
            return true;
        }

        internal int Length
        {
            get
            {
                return this.length;
            }
        }

        internal byte MessageType
        {
            get
            {
                return this.messageType;
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
                return (4 + this.length);
            }
        }


        internal enum MessageTypes : byte
        {
            PositiveSessionResponse = 130,
            SessionMessage = 0,
            SessionRequest = 0x81
        }
    }
}

