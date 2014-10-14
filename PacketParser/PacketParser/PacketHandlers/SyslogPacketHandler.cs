namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    internal class SyslogPacketHandler : AbstractPacketHandler, IPacketHandler
    {
        public SyslogPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            SyslogPacket packet = null;
            UdpPacket packet2 = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(SyslogPacket))
                {
                    packet = (SyslogPacket) packet3;
                }
                else if (packet3.GetType() == typeof(UdpPacket))
                {
                    packet2 = (UdpPacket) packet3;
                }
                if (((packet != null) && (packet2 != null)) && ((packet.SyslogMessage != null) && (packet.SyslogMessage.Length > 0)))
                {
                    NameValueCollection parameters = new NameValueCollection();
                    parameters.Add("Syslog Message", packet.SyslogMessage);
                    base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(packet.ParentFrame.FrameNumber, sourceHost, destinationHost, "UDP " + packet2.SourcePort, "UDP " + packet2.DestinationPort, parameters, packet.ParentFrame.Timestamp, "Syslog Message"));
                }
            }
        }

        public void Reset()
        {
        }
    }
}

