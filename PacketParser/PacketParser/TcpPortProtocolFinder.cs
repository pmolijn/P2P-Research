namespace PacketParser
{
    using PacketParser.Events;
    using PacketParser.Packets;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class TcpPortProtocolFinder : ISessionProtocolFinder
    {
        private NetworkHost client;
        private ushort clientPort;
        private ApplicationLayerProtocol confirmedProtocol = ApplicationLayerProtocol.Unknown;
        private PacketHandler packetHandler;
        private List<ApplicationLayerProtocol> probableProtocols = new List<ApplicationLayerProtocol>();
        private NetworkHost server;
        private ushort serverPort;
        private int startFrameNumber;
        private DateTime startTimestamp;

        internal TcpPortProtocolFinder(NetworkHost client, NetworkHost server, ushort clientPort, ushort serverPort, int startFrameNumber, DateTime startTimestamp, PacketHandler packetHandler)
        {
            this.client = client;
            this.server = server;
            this.clientPort = clientPort;
            this.serverPort = serverPort;
            this.startFrameNumber = startFrameNumber;
            this.startTimestamp = startTimestamp;
            this.packetHandler = packetHandler;
            if ((this.serverPort == 0x15) || (this.serverPort == 0x1f55))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.FtpControl);
            }
            if (this.serverPort == 0x16)
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.Ssh);
            }
            if ((this.serverPort == 0x19) || (this.serverPort == 0x24b))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.Smtp);
            }
            if (((this.serverPort == 80) || (this.serverPort == 0x1f90)) || (this.serverPort == 0xc38))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.Http);
            }
            if ((this.serverPort == 0x89) || (this.clientPort == 0x89))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.NetBiosNameService);
            }
            if ((this.serverPort == 0x8b) || (this.clientPort == 0x8b))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.NetBiosSessionService);
            }
            if (((((this.serverPort == 0x1bb) || (this.serverPort == 0x1d1)) || ((this.serverPort == 0x233) || (this.serverPort == 0x3e0))) || (((this.serverPort == 0x3e1) || (this.serverPort == 0x3e2)) || ((this.serverPort == 0x3e3) || (this.serverPort == 0x3dd)))) || ((((this.serverPort == 990) || (this.serverPort == 0x1467)) || ((this.serverPort == 0x1fea) || (this.serverPort == 0x20fb))) || ((this.serverPort == 0x2329) || (this.serverPort == 0x2346))))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.Ssl);
            }
            if ((this.serverPort == 0x1bd) || (this.clientPort == 0x1bd))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.NetBiosSessionService);
            }
            if (this.serverPort == 0x599)
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.TabularDataStream);
            }
            if (this.serverPort == 0xfe6)
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.SpotifyServerProtocol);
            }
            if (((this.serverPort == 0xc2) || ((this.serverPort >= 0x1a04) && (this.serverPort <= 0x1a0e))) || ((this.serverPort == 0x1e61) || ((this.serverPort >= 0x17e0) && (this.serverPort <= 0x17e7))))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.Irc);
            }
            if (((this.serverPort == 0x1446) || (this.clientPort == 0x1446)) || ((this.clientPort == 0x1bb) || (this.serverPort == 0x1bb)))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.Oscar);
            }
            if (((this.serverPort == 0x1446) || (this.clientPort == 0x1446)) || ((this.clientPort == 0x1bb) || (this.serverPort == 0x1bb)))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.OscarFileTransfer);
            }
            if ((this.serverPort == 0x964) || (this.clientPort == 0x964))
            {
                this.probableProtocols.Add(ApplicationLayerProtocol.IEC_104);
            }
        }

        public void AddPacket(TcpPacket tcpPacket, NetworkHost source, NetworkHost destination)
        {
        }

        public IEnumerable<ApplicationLayerProtocol> GetProbableApplicationLayerProtocols()
        {
            if (this.confirmedProtocol != ApplicationLayerProtocol.Unknown)
            {
                yield return this.confirmedProtocol;
            }
            else
            {
                foreach (ApplicationLayerProtocol iteratorVariable0 in this.probableProtocols)
                {
                    yield return iteratorVariable0;
                }
            }
        }

        public static IEnumerable<ApplicationLayerProtocol> GetProbableApplicationLayerProtocols(ushort serverPort, ushort clientPort)
        {
            TcpPortProtocolFinder finder = new TcpPortProtocolFinder(null, null, clientPort, serverPort, 0, DateTime.MinValue, null);
            return finder.GetProbableApplicationLayerProtocols();
        }

        public NetworkHost Client
        {
            get
            {
                return this.client;
            }
        }

        public ushort ClientPort
        {
            get
            {
                return this.clientPort;
            }
        }

        public ApplicationLayerProtocol ConfirmedApplicationLayerProtocol
        {
            get
            {
                return this.confirmedProtocol;
            }
            set
            {
                if (this.confirmedProtocol == ApplicationLayerProtocol.Unknown)
                {
                    this.confirmedProtocol = value;
                    this.packetHandler.OnSessionDetected(new SessionEventArgs(value, this.client, this.server, this.clientPort, this.serverPort, true, this.startFrameNumber, this.startTimestamp));
                    if ((value != ApplicationLayerProtocol.Unknown) && this.server.NetworkServiceMetadataList.ContainsKey(this.serverPort))
                    {
                        this.server.NetworkServiceMetadataList[this.serverPort].ApplicationLayerProtocol = value;
                    }
                }
            }
        }

        public NetworkHost Server
        {
            get
            {
                return this.server;
            }
        }

        public ushort ServerPort
        {
            get
            {
                return this.serverPort;
            }
        }

        public PacketParser.TransportLayerProtocol TransportLayerProtocol
        {
            get
            {
                return PacketParser.TransportLayerProtocol.TCP;
            }
        }

    }
}

