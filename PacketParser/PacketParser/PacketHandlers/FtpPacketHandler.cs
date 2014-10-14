namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.FileTransfer;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class FtpPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        private PopularityList<NetworkTcpSession, FtpSession> ftpSessionList;
        private PopularityList<string, PendingFileTransfer> pendingFileTransferList;

        public FtpPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.ftpSessionList = new PopularityList<NetworkTcpSession, FtpSession>(100);
            this.pendingFileTransferList = new PopularityList<string, PendingFileTransfer>(20);
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            TcpPacket packet = null;
            FtpPacket packet2 = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(TcpPacket))
                {
                    packet = (TcpPacket) packet3;
                }
                else if (packet3.GetType() == typeof(FtpPacket))
                {
                    packet2 = (FtpPacket) packet3;
                }
            }
            FtpSession ftpControlSession = null;
            bool flag = false;
            if (tcpSession.SynPacketReceived && tcpSession.SynAckPacketReceived)
            {
                if (!tcpSession.SessionEstablished)
                {
                    if (this.pendingFileTransferList.ContainsKey(PendingFileTransfer.GetKey(tcpSession.ClientHost, new ushort?(tcpSession.ClientTcpPort), tcpSession.ServerHost, new ushort?(tcpSession.ServerTcpPort))))
                    {
                        PendingFileTransfer transfer = this.pendingFileTransferList[PendingFileTransfer.GetKey(tcpSession.ClientHost, new ushort?(tcpSession.ClientTcpPort), tcpSession.ServerHost, new ushort?(tcpSession.ServerTcpPort))];
                        transfer.FileTransferSessionEstablished = true;
                        ftpControlSession = transfer.FtpControlSession;
                        flag = true;
                    }
                    else if (this.pendingFileTransferList.ContainsKey(PendingFileTransfer.GetKey(tcpSession.ClientHost, null, tcpSession.ServerHost, new ushort?(tcpSession.ServerTcpPort))))
                    {
                        PendingFileTransfer transfer2 = this.pendingFileTransferList[PendingFileTransfer.GetKey(tcpSession.ClientHost, null, tcpSession.ServerHost, new ushort?(tcpSession.ServerTcpPort))];
                        this.pendingFileTransferList.Remove(transfer2.GetKey());
                        transfer2.DataSessionClientPort = new ushort?(tcpSession.ClientTcpPort);
                        transfer2.FileTransferSessionEstablished = true;
                        this.pendingFileTransferList.Add(transfer2.GetKey(), transfer2);
                        ftpControlSession = transfer2.FtpControlSession;
                        flag = true;
                    }
                }
                else if (((packet != null) && packet.FlagBits.Fin) && base.MainPacketHandler.FileStreamAssemblerList.ContainsAssembler(sourceHost, packet.SourcePort, destinationHost, packet.DestinationPort, true, true, FileStreamTypes.FTP))
                {
                    FileStreamAssembler assembler = base.MainPacketHandler.FileStreamAssemblerList.GetAssembler(sourceHost, packet.SourcePort, destinationHost, packet.DestinationPort, true);
                    if ((assembler.FileContentLength == -1) && (assembler.FileSegmentRemainingBytes == -1))
                    {
                        assembler.FinishAssembling();
                    }
                }
            }
            if ((packet2 != null) && (packet != null))
            {
                flag = true;
                if (this.ftpSessionList.ContainsKey(tcpSession))
                {
                    ftpControlSession = this.ftpSessionList[tcpSession];
                }
                else
                {
                    ftpControlSession = new FtpSession(tcpSession.ClientHost, tcpSession.ServerHost);
                    this.ftpSessionList.Add(tcpSession, ftpControlSession);
                }
                if (packet2.ClientToServer)
                {
                    if (packet2.RequestCommand != null)
                    {
                        if (packet2.RequestArgument != null)
                        {
                            NameValueCollection parameters = new NameValueCollection();
                            parameters.Add(packet2.RequestCommand, packet2.RequestArgument);
                            base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(packet2.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + packet.SourcePort, "TCP " + packet.DestinationPort, parameters, packet2.ParentFrame.Timestamp, "FTP Request"));
                        }
                        if (packet2.RequestCommand.ToUpper() == "USER")
                        {
                            ftpControlSession.Username = packet2.RequestArgument;
                        }
                        else if (packet2.RequestCommand.ToUpper() == "PASS")
                        {
                            ftpControlSession.Password = packet2.RequestArgument;
                            if ((ftpControlSession.Username != null) && (ftpControlSession.Password != null))
                            {
                                base.MainPacketHandler.AddCredential(new NetworkCredential(tcpSession.ClientHost, tcpSession.ServerHost, packet2.PacketTypeDescription, ftpControlSession.Username, ftpControlSession.Password, packet2.ParentFrame.Timestamp));
                            }
                        }
                        else if (packet2.RequestCommand.ToUpper() == "PORT")
                        {
                            ushort num;
                            if (this.TryGetPort(packet2.RequestArgument, out num))
                            {
                                ftpControlSession.PendingFileTransfer = new PendingFileTransfer(ftpControlSession.ServerHost, null, ftpControlSession.ClientHost, num, false, ftpControlSession);
                                if (this.pendingFileTransferList.ContainsKey(ftpControlSession.PendingFileTransfer.GetKey()))
                                {
                                    this.pendingFileTransferList.Remove(ftpControlSession.PendingFileTransfer.GetKey());
                                }
                                this.pendingFileTransferList.Add(ftpControlSession.PendingFileTransfer.GetKey(), ftpControlSession.PendingFileTransfer);
                            }
                        }
                        else if (packet2.RequestCommand.ToUpper() == "STOR")
                        {
                            if (ftpControlSession.PendingFileTransfer != null)
                            {
                                ftpControlSession.PendingFileTransfer.Filename = packet2.RequestArgument;
                                ftpControlSession.PendingFileTransfer.FileDirectionIsDataSessionServerToDataSessionClient = new bool?(!ftpControlSession.PendingFileTransfer.DataSessionIsPassive);
                                ftpControlSession.PendingFileTransfer.Details = packet2.RequestCommand + " " + packet2.RequestArgument;
                            }
                            else
                            {
                                base.MainPacketHandler.OnAnomalyDetected("STOR command without a pending ftp data session. Frame: " + packet2.ParentFrame.ToString(), packet2.ParentFrame.Timestamp);
                            }
                        }
                        else if (packet2.RequestCommand.ToUpper() == "RETR")
                        {
                            if (ftpControlSession.PendingFileTransfer != null)
                            {
                                ftpControlSession.PendingFileTransfer.Filename = packet2.RequestArgument;
                                ftpControlSession.PendingFileTransfer.FileDirectionIsDataSessionServerToDataSessionClient = new bool?(ftpControlSession.PendingFileTransfer.DataSessionIsPassive);
                                ftpControlSession.PendingFileTransfer.Details = packet2.RequestCommand + " " + packet2.RequestArgument;
                            }
                            else
                            {
                                base.MainPacketHandler.OnAnomalyDetected("RETR command without a pending ftp data session. Frame: " + packet2.ParentFrame.ToString(), packet2.ParentFrame.Timestamp);
                            }
                        }
                        else if (packet2.RequestCommand.ToUpper() == "SIZE")
                        {
                            ftpControlSession.PendingSizeRequestFileName = packet2.RequestArgument;
                        }
                    }
                }
                else
                {
                    if ((packet2.ResponseCode != 0) && (packet2.ResponseArgument != null))
                    {
                        NameValueCollection values2 = new NameValueCollection();
                        values2.Add(packet2.ResponseCode.ToString(), packet2.ResponseArgument);
                        base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(packet2.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + packet.SourcePort, "TCP " + packet.DestinationPort, values2, packet2.ParentFrame.Timestamp, "FTP Response"));
                        if ((packet2.ResponseCode == 220) && packet2.ResponseArgument.ToLower().Contains("ftp"))
                        {
                            sourceHost.AddFtpServerBanner(packet2.ResponseArgument, packet.SourcePort);
                        }
                    }
                    if ((packet2.ResponseCode == 0xd5) && (ftpControlSession.PendingSizeRequestFileName != null))
                    {
                        int num2;
                        if (int.TryParse(packet2.ResponseArgument, out num2))
                        {
                            ftpControlSession.FileSizes[ftpControlSession.PendingSizeRequestFileName] = num2;
                        }
                        ftpControlSession.PendingSizeRequestFileName = null;
                    }
                    if ((packet2.ResponseCode != 0xe2) && (packet2.ResponseCode == 0xe3))
                    {
                        ushort num3;
                        char[] anyOf = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                        string commaSeparatedIpAndPortString = packet2.ResponseArgument.Substring(packet2.ResponseArgument.IndexOfAny(anyOf));
                        commaSeparatedIpAndPortString = commaSeparatedIpAndPortString.Substring(0, commaSeparatedIpAndPortString.LastIndexOfAny(anyOf) + 1);
                        if (this.TryGetPort(commaSeparatedIpAndPortString, out num3))
                        {
                            ftpControlSession.PendingFileTransfer = new PendingFileTransfer(ftpControlSession.ClientHost, null, ftpControlSession.ServerHost, num3, true, ftpControlSession);
                            if (this.pendingFileTransferList.ContainsKey(ftpControlSession.PendingFileTransfer.GetKey()))
                            {
                                this.pendingFileTransferList.Remove(ftpControlSession.PendingFileTransfer.GetKey());
                            }
                            this.pendingFileTransferList.Add(ftpControlSession.PendingFileTransfer.GetKey(), ftpControlSession.PendingFileTransfer);
                        }
                    }
                    if (((packet2.ResponseCode == 230) && (ftpControlSession.Username != null)) && (ftpControlSession.Password != null))
                    {
                        base.MainPacketHandler.AddCredential(new NetworkCredential(tcpSession.ClientHost, tcpSession.ServerHost, packet2.PacketTypeDescription, ftpControlSession.Username, ftpControlSession.Password, true, packet2.ParentFrame.Timestamp));
                    }
                }
            }
            if ((ftpControlSession != null) && (ftpControlSession.PendingFileTransfer != null))
            {
                flag = true;
                PendingFileTransfer pendingFileTransfer = ftpControlSession.PendingFileTransfer;
                if (pendingFileTransfer.FileTransferSessionEstablished && pendingFileTransfer.FileDirectionIsDataSessionServerToDataSessionClient.HasValue)
                {
                    ushort? dataSessionClientPort = pendingFileTransfer.DataSessionClientPort;
                    int? nullable8 = dataSessionClientPort.HasValue ? new int?(dataSessionClientPort.GetValueOrDefault()) : null;
                    if (nullable8.HasValue)
                    {
                        FileStreamAssembler assembler2 = null;
                        if (pendingFileTransfer.FileDirectionIsDataSessionServerToDataSessionClient.Value)
                        {
                            assembler2 = new FileStreamAssembler(base.MainPacketHandler.FileStreamAssemblerList, pendingFileTransfer.DataSessionServer, pendingFileTransfer.DataSessionServerPort, pendingFileTransfer.DataSessionClient, pendingFileTransfer.DataSessionClientPort.Value, true, FileStreamTypes.FTP, pendingFileTransfer.Filename, "/", pendingFileTransfer.Details, packet.ParentFrame.FrameNumber, packet.ParentFrame.Timestamp);
                        }
                        else
                        {
                            assembler2 = new FileStreamAssembler(base.MainPacketHandler.FileStreamAssemblerList, pendingFileTransfer.DataSessionClient, pendingFileTransfer.DataSessionClientPort.Value, pendingFileTransfer.DataSessionServer, pendingFileTransfer.DataSessionServerPort, true, FileStreamTypes.FTP, pendingFileTransfer.Filename, "/", pendingFileTransfer.Details, packet.ParentFrame.FrameNumber, packet.ParentFrame.Timestamp);
                        }
                        string key = "";
                        if ((assembler2.Filename != null) && (assembler2.FileLocation != null))
                        {
                            key = assembler2.FileLocation + "/" + assembler2.Filename;
                        }
                        if (ftpControlSession.FileSizes.ContainsKey(key))
                        {
                            assembler2.FileContentLength = ftpControlSession.FileSizes[key];
                            assembler2.FileSegmentRemainingBytes = ftpControlSession.FileSizes[key];
                        }
                        else
                        {
                            assembler2.FileContentLength = -1;
                            assembler2.FileSegmentRemainingBytes = -1;
                        }
                        if (assembler2.TryActivate())
                        {
                            base.MainPacketHandler.FileStreamAssemblerList.Add(assembler2);
                        }
                        this.pendingFileTransferList.Remove(pendingFileTransfer.GetKey());
                        ftpControlSession.PendingFileTransfer = null;
                    }
                }
            }
            if (flag)
            {
                return packet.PayloadDataLength;
            }
            return 0;
        }

        public void Reset()
        {
            this.ftpSessionList.Clear();
            this.pendingFileTransferList.Clear();
        }

        private bool TryGetPort(string commaSeparatedIpAndPortString, out ushort portNumber)
        {
            portNumber = 0;
            try
            {
                char[] separator = new char[] { ',', ' ', '\r', '\n' };
                string[] strArray = commaSeparatedIpAndPortString.Split(separator);
                if (strArray.Length < 6)
                {
                    return false;
                }
                ushort num = 0;
                for (int i = 4; i < 6; i++)
                {
                    byte num3;
                    if (byte.TryParse(strArray[i], out num3))
                    {
                        num = (ushort) ((num << 8) + num3);
                    }
                    else
                    {
                        return false;
                    }
                }
                portNumber = num;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.FtpControl;
            }
        }

        private class FtpSession
        {
            private Dictionary<string, int> fileSizes;
            private NetworkHost ftpClient;
            private NetworkHost ftpServer;
            private string password;
            private PacketParser.PacketHandlers.FtpPacketHandler.PendingFileTransfer pendingFileTransfer;
            private string pendingSizeRequestFileName;
            private string username;

            internal FtpSession(NetworkHost ftpClient, NetworkHost ftpServer)
            {
                this.ftpClient = ftpClient;
                this.ftpServer = ftpServer;
                this.username = null;
                this.password = null;
                this.fileSizes = new Dictionary<string, int>();
                this.pendingSizeRequestFileName = null;
            }

            internal NetworkHost ClientHost
            {
                get
                {
                    return this.ftpClient;
                }
            }

            internal Dictionary<string, int> FileSizes
            {
                get
                {
                    return this.fileSizes;
                }
            }

            internal string Password
            {
                get
                {
                    return this.password;
                }
                set
                {
                    this.password = value;
                }
            }

            internal PacketParser.PacketHandlers.FtpPacketHandler.PendingFileTransfer PendingFileTransfer
            {
                get
                {
                    return this.pendingFileTransfer;
                }
                set
                {
                    this.pendingFileTransfer = value;
                }
            }

            internal string PendingSizeRequestFileName
            {
                get
                {
                    return this.pendingSizeRequestFileName;
                }
                set
                {
                    this.pendingSizeRequestFileName = value;
                }
            }

            internal NetworkHost ServerHost
            {
                get
                {
                    return this.ftpServer;
                }
            }

            internal string Username
            {
                get
                {
                    return this.username;
                }
                set
                {
                    this.username = value;
                }
            }
        }

        private class PendingFileTransfer
        {
            private NetworkHost dataSessionClient;
            private ushort? dataSessionClientPort;
            private bool dataSessionIsPassive;
            private NetworkHost dataSessionServer;
            private ushort dataSessionServerPort;
            private string details;
            private bool? fileDirectionIsDataSessionServerToDataSessionClient;
            private string filename;
            private bool fileTransferSessionEstablished;
            private FtpPacketHandler.FtpSession ftpControlSession;

            internal PendingFileTransfer(NetworkHost dataSessionClient, ushort? dataSessionClientPort, NetworkHost dataSessionServer, ushort dataSessionServerPort, bool dataSessionIsPassive, FtpPacketHandler.FtpSession ftpControlSession)
            {
                this.dataSessionClient = dataSessionClient;
                this.dataSessionClientPort = dataSessionClientPort;
                this.dataSessionServer = dataSessionServer;
                this.dataSessionServerPort = dataSessionServerPort;
                this.dataSessionIsPassive = dataSessionIsPassive;
                this.fileDirectionIsDataSessionServerToDataSessionClient = null;
                this.fileTransferSessionEstablished = false;
                this.ftpControlSession = ftpControlSession;
                this.filename = null;
                this.details = "";
            }

            internal string GetKey()
            {
                return GetKey(this);
            }

            internal static string GetKey(FtpPacketHandler.PendingFileTransfer pendingFileTransfer)
            {
                return GetKey(pendingFileTransfer.dataSessionClient, pendingFileTransfer.dataSessionClientPort, pendingFileTransfer.dataSessionServer, new ushort?(pendingFileTransfer.dataSessionServerPort));
            }

            internal static string GetKey(NetworkHost dataSessionClient, ushort? dataSessionClientPort, NetworkHost dataSessionServer, ushort? dataSessionServerPort)
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("Data session client : ");
                builder.Append(dataSessionClient.IPAddress.ToString());
                builder.Append(" TCP/");
                builder.Append(dataSessionClientPort.ToString());
                builder.Append("\nData session server : ");
                builder.Append(dataSessionServer.IPAddress.ToString());
                builder.Append(" TCP/");
                builder.Append(dataSessionServerPort.ToString());
                return builder.ToString();
            }

            public override string ToString()
            {
                return (GetKey(this) + "\nFilename: " + this.filename + "\nDetials: " + this.details);
            }

            internal NetworkHost DataSessionClient
            {
                get
                {
                    return this.dataSessionClient;
                }
            }

            internal ushort? DataSessionClientPort
            {
                get
                {
                    return this.dataSessionClientPort;
                }
                set
                {
                    ushort? nullable = value;
                    int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
                    if (!nullable3.HasValue)
                    {
                        throw new Exception("Only allwed to assign non-null values");
                    }
                    this.dataSessionClientPort = value;
                }
            }

            internal bool DataSessionIsPassive
            {
                get
                {
                    return this.dataSessionIsPassive;
                }
            }

            internal NetworkHost DataSessionServer
            {
                get
                {
                    return this.dataSessionServer;
                }
            }

            internal ushort DataSessionServerPort
            {
                get
                {
                    return this.dataSessionServerPort;
                }
                set
                {
                    this.dataSessionServerPort = value;
                }
            }

            internal string Details
            {
                get
                {
                    return this.details;
                }
                set
                {
                    this.details = value;
                }
            }

            internal bool? FileDirectionIsDataSessionServerToDataSessionClient
            {
                get
                {
                    return this.fileDirectionIsDataSessionServerToDataSessionClient;
                }
                set
                {
                    if (!value.HasValue)
                    {
                        throw new Exception("Only allwed to assign non-null values");
                    }
                    this.fileDirectionIsDataSessionServerToDataSessionClient = value;
                }
            }

            internal string Filename
            {
                get
                {
                    return this.filename;
                }
                set
                {
                    this.filename = value;
                }
            }

            internal bool FileTransferSessionEstablished
            {
                get
                {
                    return this.fileTransferSessionEstablished;
                }
                set
                {
                    if (!value)
                    {
                        throw new Exception("Established can only be set to true!");
                    }
                    this.fileTransferSessionEstablished = value;
                }
            }

            internal FtpPacketHandler.FtpSession FtpControlSession
            {
                get
                {
                    return this.ftpControlSession;
                }
            }
        }
    }
}

