namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class NtlmSspPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        private PopularityList<int, string> ntlmChallengeList;

        public NtlmSspPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.ntlmChallengeList = new PopularityList<int, string>(20);
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            int num = 0;
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(NtlmSspPacket))
                {
                    NtlmSspPacket packet2 = (NtlmSspPacket) packet;
                    if (packet2.NtlmChallenge != null)
                    {
                        if (this.ntlmChallengeList.ContainsKey(tcpSession.GetHashCode()))
                        {
                            this.ntlmChallengeList[tcpSession.GetHashCode()] = packet2.NtlmChallenge;
                        }
                        else
                        {
                            this.ntlmChallengeList.Add(tcpSession.GetHashCode(), packet2.NtlmChallenge);
                        }
                    }
                    if (packet2.DomainName != null)
                    {
                        sourceHost.AddDomainName(packet2.DomainName);
                    }
                    if (packet2.HostName != null)
                    {
                        sourceHost.AddHostName(packet2.HostName);
                    }
                    if (packet2.UserName != null)
                    {
                        if (!sourceHost.ExtraDetailsList.ContainsKey("NTLM Username " + packet2.UserName))
                        {
                            sourceHost.ExtraDetailsList.Add("NTLM Username " + packet2.UserName, packet2.UserName);
                        }
                        string password = null;
                        if (packet2.LanManagerResponse != null)
                        {
                            password = "LAN Manager Response: " + packet2.LanManagerResponse;
                        }
                        if (packet2.NtlmResponse != null)
                        {
                            if (password == null)
                            {
                                password = "";
                            }
                            else
                            {
                                password = password + " - ";
                            }
                            password = password + "NTLM Response: " + packet2.NtlmResponse;
                        }
                        if (password == null)
                        {
                            base.MainPacketHandler.AddCredential(new NetworkCredential(sourceHost, destinationHost, "NTLMSSP", packet2.UserName, packet2.ParentFrame.Timestamp));
                        }
                        else
                        {
                            if (this.ntlmChallengeList.ContainsKey(tcpSession.GetHashCode()))
                            {
                                password = "NTLM Challenge: " + this.ntlmChallengeList[tcpSession.GetHashCode()] + " - " + password;
                            }
                            base.MainPacketHandler.AddCredential(new NetworkCredential(sourceHost, destinationHost, "NTLMSSP", packet2.UserName, password, packet2.ParentFrame.Timestamp));
                        }
                    }
                    num += packet2.ParentFrame.Data.Length;
                }
            }
            return num;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.NetBiosSessionService;
            }
        }
    }
}

