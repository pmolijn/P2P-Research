namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    internal class TabularDataStreamPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public TabularDataStreamPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        private int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, TabularDataStreamPacket tdsPacket)
        {
            if (!tdsPacket.PacketHeaderIsComplete)
            {
                return 0;
            }
            if (tdsPacket.PacketType == 1)
            {
                NameValueCollection parameters = new NameValueCollection();
                char[] separator = new char[] { ';' };
                foreach (string str in tdsPacket.Query.Split(separator))
                {
                    parameters.Add("SQL Query " + parameters.Count + 1, str);
                }
                if (parameters.Count > 0)
                {
                    base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(tdsPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "", "", parameters, tdsPacket.ParentFrame.Timestamp, ""));
                }
            }
            if (tdsPacket.PacketType == 0x10)
            {
                NetworkCredential credential = null;
                if ((tdsPacket.ClientHostname != null) && (tdsPacket.ClientHostname.Length > 0))
                {
                    tcpSession.ClientHost.AddHostName(tdsPacket.ClientHostname);
                }
                if ((tdsPacket.Username != null) && (tdsPacket.Username.Length > 0))
                {
                    credential = new NetworkCredential(tcpSession.ClientHost, tcpSession.ServerHost, "TDS (SQL)", tdsPacket.Username, tdsPacket.ParentFrame.Timestamp);
                }
                if (((tdsPacket.Password != null) && (tdsPacket.Password.Length > 0)) && (credential != null))
                {
                    credential.Password = tdsPacket.Password;
                }
                if (((tdsPacket.AppName != null) && (tdsPacket.AppName.Length > 0)) && !tcpSession.ServerHost.ExtraDetailsList.ContainsKey("SQL AppName"))
                {
                    tcpSession.ServerHost.ExtraDetailsList.Add("SQL AppName", tdsPacket.AppName);
                }
                if ((tdsPacket.ServerHostname != null) && (tdsPacket.ServerHostname.Length > 0))
                {
                    tcpSession.ServerHost.AddHostName(tdsPacket.ServerHostname);
                }
                if (((tdsPacket.LibraryName != null) && (tdsPacket.LibraryName.Length > 0)) && !tcpSession.ServerHost.ExtraDetailsList.ContainsKey("SQL Library"))
                {
                    tcpSession.ServerHost.ExtraDetailsList.Add("SQL Library", tdsPacket.LibraryName);
                }
                if (((tdsPacket.DatabaseName != null) && (tdsPacket.DatabaseName.Length > 0)) && !tcpSession.ServerHost.ExtraDetailsList.ContainsKey("SQL Database Name"))
                {
                    tcpSession.ServerHost.ExtraDetailsList.Add("SQL Database Name", tdsPacket.DatabaseName);
                }
                if (credential != null)
                {
                    base.MainPacketHandler.AddCredential(credential);
                }
            }
            return tdsPacket.PacketLength;
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            int num = 0;
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(TabularDataStreamPacket))
                {
                    num = this.ExtractData(tcpSession, sourceHost, destinationHost, (TabularDataStreamPacket) packet);
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
                return ApplicationLayerProtocol.TabularDataStream;
            }
        }
    }
}

