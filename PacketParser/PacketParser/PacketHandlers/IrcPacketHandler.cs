namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    internal class IrcPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        private PopularityList<NetworkTcpSession, IrcSession> ircSessionList;

        public IrcPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.ircSessionList = new PopularityList<NetworkTcpSession, IrcSession>(0x3e8);
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            IrcPacket packet = null;
            TcpPacket packet2 = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(TcpPacket))
                {
                    packet2 = (TcpPacket) packet3;
                }
                else if (packet3.GetType() == typeof(IrcPacket))
                {
                    packet = (IrcPacket) packet3;
                }
            }
            if ((packet != null) && (packet2 != null))
            {
                NameValueCollection parameters = new NameValueCollection();
                foreach (IrcPacket.Message message in packet.Messages)
                {
                    parameters.Add(message.Command, message.ToString());
                    if (message.Command.Equals("USER", StringComparison.InvariantCultureIgnoreCase))
                    {
                        List<string> list = new List<string>();
                        foreach (string str in message.Parameters)
                        {
                            list.Add(str);
                        }
                        if (list.Count > 0)
                        {
                            IrcSession session;
                            string str2 = list[0];
                            if (this.ircSessionList.ContainsKey(tcpSession))
                            {
                                session = this.ircSessionList[tcpSession];
                            }
                            else
                            {
                                session = new IrcSession();
                                this.ircSessionList.Add(tcpSession, session);
                            }
                            session.User = str2;
                            sourceHost.AddNumberedExtraDetail("IRC Username", str2);
                            base.MainPacketHandler.AddCredential(session.GetCredential(sourceHost, destinationHost, packet.ParentFrame.Timestamp));
                        }
                        if (list.Count > 1)
                        {
                            sourceHost.AddHostName(list[1]);
                        }
                        if (list.Count > 2)
                        {
                            destinationHost.AddHostName(list[2]);
                        }
                    }
                    else if (message.Command.Equals("NICK", StringComparison.InvariantCultureIgnoreCase))
                    {
                        IEnumerator<string> enumerator = message.Parameters.GetEnumerator();
                        if (enumerator.MoveNext())
                        {
                            IrcSession session2;
                            string current = enumerator.Current;
                            if (this.ircSessionList.ContainsKey(tcpSession))
                            {
                                session2 = this.ircSessionList[tcpSession];
                            }
                            else
                            {
                                session2 = new IrcSession();
                                this.ircSessionList.Add(tcpSession, session2);
                            }
                            session2.Nick = current;
                            sourceHost.AddNumberedExtraDetail("IRC Nick", current);
                            base.MainPacketHandler.AddCredential(session2.GetCredential(sourceHost, destinationHost, packet.ParentFrame.Timestamp));
                        }
                    }
                    else if (message.Command.Equals("PASS", StringComparison.InvariantCultureIgnoreCase))
                    {
                        IEnumerator<string> enumerator2 = message.Parameters.GetEnumerator();
                        if (enumerator2.MoveNext())
                        {
                            IrcSession session3;
                            string str4 = enumerator2.Current;
                            if (this.ircSessionList.ContainsKey(tcpSession))
                            {
                                session3 = this.ircSessionList[tcpSession];
                            }
                            else
                            {
                                session3 = new IrcSession();
                                this.ircSessionList.Add(tcpSession, session3);
                            }
                            session3.Pass = str4;
                            base.MainPacketHandler.AddCredential(session3.GetCredential(sourceHost, destinationHost, packet.ParentFrame.Timestamp));
                        }
                    }
                    else if (message.Command.Equals("PRIVMSG", StringComparison.InvariantCultureIgnoreCase))
                    {
                        List<string> list2 = new List<string>();
                        foreach (string str5 in message.Parameters)
                        {
                            list2.Add(str5);
                        }
                        if (list2.Count >= 2)
                        {
                            NameValueCollection attributes = new NameValueCollection();
                            attributes.Add("Command", message.Command);
                            string from = "";
                            if ((message.Prefix != null) && (message.Prefix.Length > 0))
                            {
                                attributes.Add("Prefix", message.Prefix);
                                from = message.Prefix;
                            }
                            for (int i = 0; i < list2.Count; i++)
                            {
                                attributes.Add("Parameter " + (i + 1), list2[i]);
                            }
                            base.MainPacketHandler.OnMessageDetected(new MessageEventArgs(ApplicationLayerProtocol.Irc, sourceHost, destinationHost, packet.ParentFrame.FrameNumber, packet.ParentFrame.Timestamp, from, list2[0], list2[1], list2[1], attributes));
                        }
                    }
                }
                if (parameters.Count > 0)
                {
                    base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(packet.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + packet2.SourcePort, "TCP " + packet2.DestinationPort, parameters, packet2.ParentFrame.Timestamp, "IRC packet"));
                    return packet.ParsedBytesCount;
                }
            }
            return 0;
        }

        public void Reset()
        {
            this.ircSessionList.Clear();
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.Irc;
            }
        }

        private class IrcSession
        {
            private string nick;
            private string pass;
            private string user;

            internal NetworkCredential GetCredential(NetworkHost sourceHost, NetworkHost destinationHost, DateTime timestamp)
            {
                string username = "";
                if (this.nick != null)
                {
                    username = this.nick;
                }
                if (this.user != null)
                {
                    username = username + "(IRC User: " + this.user + ")";
                }
                string password = "N/A";
                if (this.pass != null)
                {
                    password = this.pass;
                }
                return new NetworkCredential(sourceHost, destinationHost, "IRC", username, password, timestamp);
            }

            internal string Nick
            {
                set
                {
                    this.nick = value;
                }
            }

            internal string Pass
            {
                set
                {
                    this.pass = value;
                }
            }

            internal string User
            {
                set
                {
                    this.user = value;
                }
            }
        }
    }
}

