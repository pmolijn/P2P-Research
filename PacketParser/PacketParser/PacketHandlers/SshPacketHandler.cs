namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class SshPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public SshPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(SshPacket))
                {
                    SshPacket packet2 = (SshPacket) packet;
                    if (!sourceHost.ExtraDetailsList.ContainsKey("SSH Version"))
                    {
                        sourceHost.ExtraDetailsList.Add("SSH Version", packet2.SshVersion);
                    }
                    if (!sourceHost.ExtraDetailsList.ContainsKey("SSH Application"))
                    {
                        sourceHost.ExtraDetailsList.Add("SSH Application", packet2.SshApplication);
                    }
                    return packet.PacketLength;
                }
            }
            return 0;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.Ssh;
            }
        }
    }
}

