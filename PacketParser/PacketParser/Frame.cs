namespace PacketParser
{
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    public class Frame
    {
        private byte[] data;
        private List<Error> errorList;
        private int frameNumber;
        private const int MAX_FRAME_SIZE = 0x10000;
        private SortedList<int, AbstractPacket> packetList;
        private bool precomputePacketList;
        private bool quickParse;
        private DateTime timestamp;

        public Frame(DateTime timestamp, byte[] data, int frameNumber)
        {
            this.timestamp = timestamp;
            this.data = data;
            this.frameNumber = frameNumber;
        }

        public Frame(DateTime timestamp, byte[] data, Type packetType, int frameNumber) : this(timestamp, data, packetType, frameNumber, true)
        {
        }

        public Frame(DateTime timestamp, byte[] data, Type packetType, int frameNumber, bool precomputePacketList) : this(timestamp, data, packetType, frameNumber, precomputePacketList, false)
        {
        }

        public Frame(DateTime timestamp, byte[] data, Type packetType, int frameNumber, bool precomputePacketList, bool quickParse) : this(timestamp, data, packetType, frameNumber, precomputePacketList, quickParse, 0x10000)
        {
        }

        public Frame(DateTime timestamp, byte[] data, Type packetType, int frameNumber, bool precomputePacketList, bool quickParse, int maxFrameSize)
        {
            if (data.Length > maxFrameSize)
            {
                throw new ArgumentException("Frame larger than max allowed size " + maxFrameSize);
            }
            this.precomputePacketList = precomputePacketList;
            this.frameNumber = frameNumber;
            this.quickParse = quickParse;
            this.timestamp = timestamp;
            this.data = data;
            if (!quickParse)
            {
                this.errorList = new List<Error>();
            }
            this.packetList = new SortedList<int, AbstractPacket>();
            AbstractPacket packet = null;
            if (data.Length > 0)
            {
                PacketFactory.TryGetPacket(out packet, packetType, this, 0, data.Length - 1);
            }
            if (packet != null)
            {
                this.packetList.Add(packet.PacketStartIndex, packet);
                if (this.precomputePacketList)
                {
                    foreach (AbstractPacket packet2 in packet.GetSubPackets(false))
                    {
                        if (this.packetList.ContainsKey(packet2.PacketStartIndex))
                        {
                            this.packetList[packet2.PacketStartIndex] = packet2;
                        }
                        else
                        {
                            this.packetList.Add(packet2.PacketStartIndex, packet2);
                        }
                    }
                }
            }
        }

        public int IndexOf(byte[] bytes)
        {
            int num2;
            for (int i = 0; ((bytes != null) && (bytes.Length > 0)) && (i <= (this.data.Length - bytes.Length)); i = num2 + 1)
            {
                num2 = Array.IndexOf<byte>(this.data, bytes[0], i);
                if ((num2 < 0) || (num2 > (this.data.Length - bytes.Length)))
                {
                    return -1;
                }
                bool flag = true;
                for (int j = 1; flag && (j < bytes.Length); j++)
                {
                    if (this.data[num2 + j] != bytes[j])
                    {
                        flag = false;
                    }
                }
                if (flag)
                {
                    return num2;
                }
            }
            return -1;
        }

        public override string ToString()
        {
            return string.Concat(new object[] { "Frame ", this.frameNumber, " [", this.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff tt"), "]" });
        }

        public AbstractPacket BasePacket
        {
            get
            {
                if (this.packetList.ContainsKey(0))
                {
                    return this.packetList[0];
                }
                return null;
            }
        }

        public byte[] Data
        {
            get
            {
                return this.data;
            }
        }

        public IList<Error> Errors
        {
            get
            {
                return this.errorList;
            }
        }

        public int FrameNumber
        {
            get
            {
                return this.frameNumber;
            }
        }

        public IEnumerable<AbstractPacket> PacketList
        {
            get
            {
                if (!this.precomputePacketList && this.packetList.ContainsKey(0))
                {
                    return this.packetList[0].GetSubPackets(true);
                }
                return this.packetList.Values;
            }
        }

        public bool QuickParse
        {
            get
            {
                return this.quickParse;
            }
        }

        public DateTime Timestamp
        {
            get
            {
                return this.timestamp.ToLocalTime();
            }
        }

        public class Error
        {
            private string description;
            private int errorEndIndex;
            private int errorStartIndex;
            private Frame frame;

            internal Error(Frame frame, int errorStartIndex, int errorEndIndex, string description)
            {
                this.frame = frame;
                this.errorStartIndex = errorStartIndex;
                this.errorEndIndex = errorEndIndex;
                this.description = description;
            }

            public override string ToString()
            {
                return string.Concat(new object[] { this.description, ", [", this.errorStartIndex, ",", this.errorEndIndex, "]" });
            }
        }
    }
}

