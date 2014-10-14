namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class UpnpPacketHandler : AbstractPacketHandler, IPacketHandler
    {
        public UpnpPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        private void ExtractData(UpnpPacket upnpPacket, NetworkHost sourceHost)
        {
            if (upnpPacket.FieldList.Count > 0)
            {
                if (sourceHost.UniversalPlugAndPlayFieldList == null)
                {
                    sourceHost.UniversalPlugAndPlayFieldList = new SortedList<string, string>();
                }
                foreach (string str in upnpPacket.FieldList)
                {
                    if (!sourceHost.UniversalPlugAndPlayFieldList.ContainsKey(str))
                    {
                        sourceHost.UniversalPlugAndPlayFieldList.Add(str, str);
                    }
                }
            }
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(UpnpPacket))
                {
                    this.ExtractData((UpnpPacket) packet, sourceHost);
                }
            }
        }

        public void Reset()
        {
        }
    }
}

