namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class SipPacketHandler : AbstractPacketHandler, IPacketHandler
    {
        public SipPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(SipPacket))
                {
                    SipPacket packet2 = (SipPacket) packet;
                    if ((packet2.To != null) && (packet2.To.Length > 0))
                    {
                        string to = packet2.To;
                        if (to.Contains(";"))
                        {
                            to = to.Substring(0, to.IndexOf(';'));
                        }
                        destinationHost.ExtraDetailsList["SIP User"] = to;
                    }
                    if ((packet2.From != null) && (packet2.From.Length > 0))
                    {
                        string from = packet2.From;
                        if (from.Contains(";"))
                        {
                            from = from.Substring(0, from.IndexOf(';'));
                        }
                        sourceHost.ExtraDetailsList["SIP User"] = from;
                    }
                }
            }
        }

        public void Reset()
        {
        }
    }
}

