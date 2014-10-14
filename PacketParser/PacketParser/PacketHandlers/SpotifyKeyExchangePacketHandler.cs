namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class SpotifyKeyExchangePacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public SpotifyKeyExchangePacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(SpotifyKeyExchangePacket))
                {
                    SpotifyKeyExchangePacket packet2 = (SpotifyKeyExchangePacket) packet;
                    if (packet2.IsClientToServer)
                    {
                        if (!tcpSession.ClientHost.ExtraDetailsList.ContainsKey("Spotify application OS"))
                        {
                            tcpSession.ClientHost.ExtraDetailsList.Add("Spotify application OS", packet2.ClientOperatingSystem);
                        }
                        NetworkCredential credential = new NetworkCredential(tcpSession.ClientHost, tcpSession.ServerHost, packet2.PacketTypeDescription, packet2.ClientUsername, packet2.ParentFrame.Timestamp) {
                            Password = "Client DH public key: " + packet2.PublicKeyHexString
                        };
                        base.MainPacketHandler.AddCredential(credential);
                    }
                    else
                    {
                        NetworkCredential credential2 = new NetworkCredential(tcpSession.ClientHost, tcpSession.ServerHost, packet2.PacketTypeDescription, packet2.ClientUsername, packet2.ParentFrame.Timestamp) {
                            Password = "Server DH public key: " + packet2.PublicKeyHexString
                        };
                        base.MainPacketHandler.AddCredential(credential2);
                    }
                    return packet2.PacketLength;
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
                return ApplicationLayerProtocol.SpotifyServerProtocol;
            }
        }
    }
}

