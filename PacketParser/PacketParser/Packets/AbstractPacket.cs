namespace PacketParser.Packets
{
    using PacketParser;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;

    public abstract class AbstractPacket : IPacket
    {
        private NameValueCollection attributes;
        private int packetEndIndex;
        private int packetStartIndex;
        private string packetTypeDescription;
        private Frame parentFrame;

        internal AbstractPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex, string packetTypeDescription)
        {
            this.parentFrame = parentFrame;
            this.packetStartIndex = packetStartIndex;
            this.packetEndIndex = packetEndIndex;
            this.packetTypeDescription = packetTypeDescription;
            if (!parentFrame.QuickParse)
            {
                this.attributes = new NameValueCollection();
                if (packetStartIndex > packetEndIndex)
                {
                    string description = string.Concat(new object[] { "PacketStartIndex (", packetStartIndex, ") > PacketEndIndex (", packetEndIndex, ")" });
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetEndIndex, packetEndIndex, description));
                    throw new Exception(description);
                }
            }
        }

        public byte[] GetPacketData()
        {
            byte[] destinationArray = new byte[this.PacketByteCount];
            Array.Copy(this.parentFrame.Data, this.PacketStartIndex, destinationArray, 0, destinationArray.Length);
            return destinationArray;
        }

        public abstract IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference);
        public static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            return false;
        }

        public NameValueCollection Attributes
        {
            get
            {
                return this.attributes;
            }
        }

        public int PacketByteCount
        {
            get
            {
                return ((this.packetEndIndex - this.packetStartIndex) + 1);
            }
        }

        public int PacketEndIndex
        {
            get
            {
                return this.packetEndIndex;
            }
            set
            {
                if ((value >= this.packetStartIndex) && (value < this.parentFrame.Data.Length))
                {
                    this.packetEndIndex = value;
                }
            }
        }

        public int PacketLength
        {
            get
            {
                return ((this.packetEndIndex - this.packetStartIndex) + 1);
            }
        }

        public int PacketStartIndex
        {
            get
            {
                return this.packetStartIndex;
            }
        }

        public string PacketTypeDescription
        {
            get
            {
                return this.packetTypeDescription;
            }
        }

        public Frame ParentFrame
        {
            get
            {
                return this.parentFrame;
            }
        }
    }
}

