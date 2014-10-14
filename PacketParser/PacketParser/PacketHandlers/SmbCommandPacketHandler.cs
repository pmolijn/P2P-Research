namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.FileTransfer;
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    internal class SmbCommandPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        private PopularityList<string, SmbSession> smbSessionPopularityList;

        public SmbCommandPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.smbSessionPopularityList = new PopularityList<string, SmbSession>(100);
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            bool flag = false;
            CifsPacket.AbstractSmbCommand smbCommandPacket = null;
            TcpPacket tcpPacket = null;
            foreach (AbstractPacket packet2 in packetList)
            {
                if (packet2.GetType().IsSubclassOf(typeof(CifsPacket.AbstractSmbCommand)))
                {
                    smbCommandPacket = (CifsPacket.AbstractSmbCommand) packet2;
                }
                else if (packet2.GetType() == typeof(TcpPacket))
                {
                    tcpPacket = (TcpPacket) packet2;
                }
            }
            if ((tcpPacket != null) && (smbCommandPacket != null))
            {
                flag = true;
                this.ExtractSmbData(sourceHost, destinationHost, tcpPacket, smbCommandPacket, base.MainPacketHandler);
            }
            if (flag)
            {
                return 0;
            }
            return 0;
        }

        private void ExtractSmbData(NetworkHost sourceHost, NetworkHost destinationHost, TcpPacket tcpPacket, CifsPacket.AbstractSmbCommand smbCommandPacket, PacketHandler mainPacketHandler)
        {
            string str;
            if (smbCommandPacket.ParentCifsPacket.FlagsResponse)
            {
                str = SmbSession.GetSmbSessionId(sourceHost.IPAddress, tcpPacket.SourcePort, destinationHost.IPAddress, tcpPacket.DestinationPort);
            }
            else
            {
                str = SmbSession.GetSmbSessionId(destinationHost.IPAddress, tcpPacket.DestinationPort, sourceHost.IPAddress, tcpPacket.SourcePort);
            }
            if (smbCommandPacket.GetType() == typeof(CifsPacket.NegotiateProtocolRequest))
            {
                CifsPacket.NegotiateProtocolRequest request = (CifsPacket.NegotiateProtocolRequest) smbCommandPacket;
                sourceHost.AcceptedSmbDialectsList = request.DialectList;
            }
            else if (smbCommandPacket.GetType() == typeof(CifsPacket.NegotiateProtocolResponse))
            {
                CifsPacket.NegotiateProtocolResponse response = (CifsPacket.NegotiateProtocolResponse) smbCommandPacket;
                if ((destinationHost.AcceptedSmbDialectsList != null) && (destinationHost.AcceptedSmbDialectsList.Count > response.DialectIndex))
                {
                    sourceHost.PreferredSmbDialect = destinationHost.AcceptedSmbDialectsList[response.DialectIndex];
                }
            }
            else if (smbCommandPacket.GetType() == typeof(CifsPacket.SetupAndXRequest))
            {
                CifsPacket.SetupAndXRequest request2 = (CifsPacket.SetupAndXRequest) smbCommandPacket;
                if ((request2.NativeLanManager != null) && (request2.NativeLanManager.Length > 0))
                {
                    if (sourceHost.ExtraDetailsList.ContainsKey("SMB Native LAN Manager"))
                    {
                        sourceHost.ExtraDetailsList["SMB Native LAN Manager"] = request2.NativeLanManager;
                    }
                    else
                    {
                        sourceHost.ExtraDetailsList.Add("SMB Native LAN Manager", request2.NativeLanManager);
                    }
                }
                if ((request2.NativeOs != null) && (request2.NativeOs.Length > 0))
                {
                    if (sourceHost.ExtraDetailsList.ContainsKey("SMB Native OS"))
                    {
                        sourceHost.ExtraDetailsList["SMB Native OS"] = request2.NativeOs;
                    }
                    else
                    {
                        sourceHost.ExtraDetailsList.Add("SMB Native OS", request2.NativeOs);
                    }
                }
                if ((request2.PrimaryDomain != null) && (request2.PrimaryDomain.Length > 0))
                {
                    sourceHost.AddDomainName(request2.PrimaryDomain);
                }
                if ((request2.AccountName != null) && (request2.AccountName.Length > 0))
                {
                    PacketParser.NetworkCredential credential = new PacketParser.NetworkCredential(sourceHost, destinationHost, smbCommandPacket.PacketTypeDescription, request2.AccountName, request2.ParentFrame.Timestamp);
                    if ((request2.AccountPassword != null) && (request2.AccountPassword.Length > 0))
                    {
                        credential.Password = request2.AccountPassword;
                    }
                    mainPacketHandler.AddCredential(credential);
                }
            }
            else if (smbCommandPacket.GetType() == typeof(CifsPacket.SetupAndXResponse))
            {
                CifsPacket.SetupAndXResponse response2 = (CifsPacket.SetupAndXResponse) smbCommandPacket;
                if ((response2.NativeLanManager != null) && (response2.NativeLanManager.Length > 0))
                {
                    if (sourceHost.ExtraDetailsList.ContainsKey("SMB Native LAN Manager"))
                    {
                        sourceHost.ExtraDetailsList["SMB Native LAN Manager"] = response2.NativeLanManager;
                    }
                    else
                    {
                        sourceHost.ExtraDetailsList.Add("SMB Native LAN Manager", response2.NativeLanManager);
                    }
                }
                if ((response2.NativeOs != null) && (response2.NativeOs.Length > 0))
                {
                    if (sourceHost.ExtraDetailsList.ContainsKey("SMB Native OS"))
                    {
                        sourceHost.ExtraDetailsList["SMB Native OS"] = response2.NativeOs;
                    }
                    else
                    {
                        sourceHost.ExtraDetailsList.Add("SMB Native OS", response2.NativeOs);
                    }
                }
            }
            else if (smbCommandPacket.GetType() == typeof(CifsPacket.NTCreateAndXRequest))
            {
                string filename;
                string str3;
                CifsPacket.NTCreateAndXRequest request3 = (CifsPacket.NTCreateAndXRequest) smbCommandPacket;
                if (request3.Filename.EndsWith("\0"))
                {
                    filename = request3.Filename.Remove(request3.Filename.Length - 1);
                }
                else
                {
                    filename = request3.Filename;
                }
                if (filename.Contains(Path.DirectorySeparatorChar.ToString()))
                {
                    str3 = filename.Substring(0, filename.LastIndexOf(Path.DirectorySeparatorChar.ToString()));
                    filename = filename.Substring(filename.LastIndexOf(Path.DirectorySeparatorChar.ToString()) + 1);
                }
                else
                {
                    str3 = Path.DirectorySeparatorChar.ToString();
                }
                try
                {
                    SmbSession session;
                    if (this.smbSessionPopularityList.ContainsKey(str))
                    {
                        session = this.smbSessionPopularityList[str];
                    }
                    else
                    {
                        session = new SmbSession(destinationHost.IPAddress, tcpPacket.DestinationPort, sourceHost.IPAddress, tcpPacket.SourcePort);
                        this.smbSessionPopularityList.Add(str, session);
                    }
                    FileStreamAssembler assembler = new FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, tcpPacket != null, FileStreamTypes.SMB, filename, str3, request3.Filename, smbCommandPacket.ParentFrame.FrameNumber, smbCommandPacket.ParentFrame.Timestamp);
                    session.AddFileStreamAssembler(assembler, request3.ParentCifsPacket.TreeId, request3.ParentCifsPacket.MultiplexId, request3.ParentCifsPacket.ProcessId);
                }
                catch (Exception exception)
                {
                    base.MainPacketHandler.OnAnomalyDetected("Error creating assembler for SMB file transfer: " + exception.Message);
                }
            }
            else if (!smbCommandPacket.ParentCifsPacket.FlagsResponse && this.smbSessionPopularityList.ContainsKey(str))
            {
                if ((smbCommandPacket.GetType() != typeof(CifsPacket.CloseRequest)) || !this.smbSessionPopularityList.ContainsKey(str))
                {
                    if ((smbCommandPacket.GetType() == typeof(CifsPacket.ReadAndXRequest)) && this.smbSessionPopularityList.ContainsKey(str))
                    {
                        SmbSession session3 = this.smbSessionPopularityList[str];
                        CifsPacket.ReadAndXRequest request5 = (CifsPacket.ReadAndXRequest) smbCommandPacket;
                        ushort fileId = request5.FileId;
                        session3.Touch(request5.ParentCifsPacket.TreeId, request5.ParentCifsPacket.MultiplexId, request5.ParentCifsPacket.ProcessId, fileId);
                    }
                }
                else
                {
                    SmbSession session2 = this.smbSessionPopularityList[str];
                    CifsPacket.CloseRequest request4 = (CifsPacket.CloseRequest) smbCommandPacket;
                    ushort num = request4.FileId;
                    if (session2.ContainsFileId(request4.ParentCifsPacket.TreeId, request4.ParentCifsPacket.MultiplexId, request4.ParentCifsPacket.ProcessId, num))
                    {
                        FileStreamAssembler assembler2 = session2.GetFileStreamAssembler(request4.ParentCifsPacket.TreeId, request4.ParentCifsPacket.MultiplexId, request4.ParentCifsPacket.ProcessId, num);
                        session2.RemoveFileStreamAssembler(request4.ParentCifsPacket.TreeId, request4.ParentCifsPacket.MultiplexId, request4.ParentCifsPacket.ProcessId, num, false);
                        if (mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(assembler2))
                        {
                            mainPacketHandler.FileStreamAssemblerList.Remove(assembler2, true);
                        }
                        else
                        {
                            assembler2.Clear();
                        }
                    }
                }
            }
            else if (smbCommandPacket.ParentCifsPacket.FlagsResponse && this.smbSessionPopularityList.ContainsKey(str))
            {
                SmbSession session4 = this.smbSessionPopularityList[str];
                if (smbCommandPacket.GetType() == typeof(CifsPacket.NTCreateAndXResponse))
                {
                    CifsPacket.NTCreateAndXResponse response3 = (CifsPacket.NTCreateAndXResponse) smbCommandPacket;
                    ushort num3 = response3.FileId;
                    int endOfFile = (int) response3.EndOfFile;
                    FileStreamAssembler assembler3 = session4.GetLastReferencedFileStreamAssembler(response3.ParentCifsPacket.TreeId, response3.ParentCifsPacket.MultiplexId, response3.ParentCifsPacket.ProcessId);
                    session4.RemoveLastReferencedAssembler(response3.ParentCifsPacket.TreeId, response3.ParentCifsPacket.MultiplexId, response3.ParentCifsPacket.ProcessId);
                    if (assembler3 != null)
                    {
                        assembler3.ExtendedFileId = "Id" + num3.ToString("X4");
                        session4.AddFileStreamAssembler(assembler3, response3.ParentCifsPacket.TreeId, response3.ParentCifsPacket.MultiplexId, response3.ParentCifsPacket.ProcessId, response3.FileId);
                        assembler3.FileContentLength = endOfFile;
                    }
                }
                else if (smbCommandPacket.GetType() == typeof(CifsPacket.ReadAndXResponse))
                {
                    CifsPacket.ReadAndXResponse response4 = (CifsPacket.ReadAndXResponse) smbCommandPacket;
                    FileStreamAssembler assembler4 = session4.GetLastReferencedFileStreamAssembler(response4.ParentCifsPacket.TreeId, response4.ParentCifsPacket.MultiplexId, response4.ParentCifsPacket.ProcessId);
                    if (assembler4 == null)
                    {
                        base.MainPacketHandler.OnAnomalyDetected("Unable to find assembler for " + smbCommandPacket.ToString());
                    }
                    else if (assembler4 != null)
                    {
                        assembler4.FileSegmentRemainingBytes += response4.DataLength;
                        if (!assembler4.IsActive)
                        {
                            if (!assembler4.TryActivate())
                            {
                                if (!response4.ParentCifsPacket.ParentFrame.QuickParse)
                                {
                                    response4.ParentCifsPacket.ParentFrame.Errors.Add(new Frame.Error(response4.ParentCifsPacket.ParentFrame, response4.PacketStartIndex, response4.PacketEndIndex, "Unable to activate file stream assembler for " + assembler4.FileLocation + "/" + assembler4.Filename));
                                }
                            }
                            else if (assembler4.IsActive)
                            {
                                assembler4.AddData(response4.GetFileData(), tcpPacket.SequenceNumber);
                            }
                        }
                    }
                }
            }
        }

        public void Reset()
        {
            this.smbSessionPopularityList.Clear();
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.NetBiosSessionService;
            }
        }

        internal class SmbSession
        {
            private IPAddress clientIP;
            private ushort clientTcpPort;
            private PopularityList<ushort, FileStreamAssembler> fileIdAssemblerList;
            private SortedList<uint, ushort> lastReferencedFileIdPerPidMux;
            private SortedList<uint, ushort> lastReferencedFileIdPerTreeMux;
            private IPAddress serverIP;
            private ushort serverTcpPort;

            internal SmbSession(IPAddress serverIP, ushort serverTcpPort, IPAddress clientIP, ushort clientTcpPort)
            {
                this.serverIP = serverIP;
                this.serverTcpPort = serverTcpPort;
                this.clientIP = clientIP;
                this.clientTcpPort = clientTcpPort;
                this.lastReferencedFileIdPerTreeMux = new SortedList<uint, ushort>();
                this.lastReferencedFileIdPerPidMux = new SortedList<uint, ushort>();
                this.fileIdAssemblerList = new PopularityList<ushort, FileStreamAssembler>(100);
            }

            internal void AddFileStreamAssembler(FileStreamAssembler assembler, ushort treeId, ushort muxId, ushort processId)
            {
                this.AddFileStreamAssembler(assembler, treeId, muxId, processId, 0);
            }

            internal void AddFileStreamAssembler(FileStreamAssembler assembler, ushort treeId, ushort muxId, ushort processId, ushort fileId)
            {
                this.lastReferencedFileIdPerTreeMux[ByteConverter.ToUInt32(treeId, muxId)] = fileId;
                this.lastReferencedFileIdPerPidMux[ByteConverter.ToUInt32(processId, muxId)] = fileId;
                if (this.fileIdAssemblerList.ContainsKey(fileId))
                {
                    this.fileIdAssemblerList.Remove(fileId);
                }
                this.fileIdAssemblerList.Add(fileId, assembler);
            }

            internal bool ContainsFileId(ushort treeId, ushort muxId, ushort processId, ushort fileId)
            {
                this.Touch(treeId, muxId, processId, fileId);
                return this.fileIdAssemblerList.ContainsKey(fileId);
            }

            internal FileStreamAssembler GetFileStreamAssembler(ushort treeId, ushort muxId, ushort processId, ushort fileId)
            {
                this.Touch(treeId, muxId, processId, fileId);
                if (this.fileIdAssemblerList.ContainsKey(fileId))
                {
                    return this.fileIdAssemblerList[fileId];
                }
                return null;
            }

            internal string GetId()
            {
                return GetSmbSessionId(this.serverIP, this.serverTcpPort, this.clientIP, this.clientTcpPort);
            }

            internal FileStreamAssembler GetLastReferencedFileStreamAssembler(ushort treeId, ushort muxId, ushort processId)
            {
                if (this.lastReferencedFileIdPerPidMux.ContainsKey(ByteConverter.ToUInt32(processId, muxId)))
                {
                    return this.GetFileStreamAssembler(treeId, muxId, processId, this.lastReferencedFileIdPerPidMux[ByteConverter.ToUInt32(processId, muxId)]);
                }
                if (this.lastReferencedFileIdPerTreeMux.ContainsKey(ByteConverter.ToUInt32(treeId, muxId)))
                {
                    return this.GetFileStreamAssembler(treeId, muxId, processId, this.lastReferencedFileIdPerTreeMux[ByteConverter.ToUInt32(treeId, muxId)]);
                }
                return null;
            }

            internal static string GetSmbSessionId(IPAddress serverIP, ushort serverTcpPort, IPAddress clientIP, ushort clientTcpPort)
            {
                return (serverIP.ToString() + ":" + serverTcpPort.ToString("X4") + "-" + clientIP.ToString() + ":" + clientTcpPort.ToString("X4"));
            }

            internal void RemoveFileStreamAssembler(ushort treeId, ushort muxId, ushort processId, ushort fileId)
            {
                this.RemoveFileStreamAssembler(treeId, muxId, processId, fileId, false);
            }

            internal void RemoveFileStreamAssembler(ushort treeId, ushort muxId, ushort processId, ushort fileId, bool closeAssembler)
            {
                if (this.fileIdAssemblerList.ContainsKey(fileId))
                {
                    FileStreamAssembler assembler = this.GetFileStreamAssembler(treeId, muxId, processId, fileId);
                    this.fileIdAssemblerList.Remove(fileId);
                    if (closeAssembler)
                    {
                        assembler.Clear();
                    }
                }
            }

            internal void RemoveLastReferencedAssembler(ushort treeId, ushort muxId, ushort processId)
            {
                ushort num;
                if (this.lastReferencedFileIdPerPidMux.ContainsKey(ByteConverter.ToUInt32(processId, muxId)))
                {
                    num = this.lastReferencedFileIdPerPidMux[ByteConverter.ToUInt32(processId, muxId)];
                }
                else if (this.lastReferencedFileIdPerTreeMux.ContainsKey(ByteConverter.ToUInt32(treeId, muxId)))
                {
                    num = this.lastReferencedFileIdPerTreeMux[ByteConverter.ToUInt32(treeId, muxId)];
                }
                else
                {
                    num = 0;
                }
                if (this.fileIdAssemblerList.ContainsKey(num))
                {
                    this.RemoveFileStreamAssembler(treeId, muxId, processId, num);
                }
            }

            internal void Touch(ushort treeId, ushort muxId, ushort processId, ushort fileId)
            {
                if (this.fileIdAssemblerList.ContainsKey(fileId))
                {
                    this.lastReferencedFileIdPerTreeMux[ByteConverter.ToUInt32(treeId, muxId)] = fileId;
                    this.lastReferencedFileIdPerPidMux[ByteConverter.ToUInt32(processId, muxId)] = fileId;
                }
            }
        }
    }
}

