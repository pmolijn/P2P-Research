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

    internal class UpnpPacket : AbstractPacket
    {
        private List<string> fieldList;

        internal UpnpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "UPnP")
        {
            this.fieldList = new List<string>();
            int dataIndex = packetStartIndex;
            while (dataIndex < packetEndIndex)
            {
                string item = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
                if ((item == null) || (item.Length <= 0))
                {
                    break;
                }
                this.fieldList.Add(item);
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

        internal List<string> FieldList
        {
            get
            {
                return this.fieldList;
            }
        }

    }
}

