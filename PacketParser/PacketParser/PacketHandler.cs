namespace PacketParser
{
    using NetworkWrapper;
    using PacketParser.CleartextDictionary;
    using PacketParser.Events;
    using PacketParser.FileTransfer;
    using PacketParser.Fingerprints;
    using PacketParser.Mime;
    using PacketParser.PacketHandlers;
    using PacketParser.Packets;
    using PacketParser.Utils;
    using pcapFileIO;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;

    public class PacketHandler
    {
        private int cleartextSearchModeSelectedIndex;
        private SortedList<string, PacketParser.NetworkCredential> credentialList;
        private WordDictionary dictionary;
        private PacketParser.FileTransfer.FileStreamAssemblerList fileStreamAssemblerList;
        private Thread frameQueueConsumerThread;
        private Queue<Frame> framesToParseQueue;
        internal static PopularityList<string, List<IPv4Packet>> Ipv4Fragments = new PopularityList<string, List<IPv4Packet>>(0x400);
        private byte[][] keywordList;
        private int? lastBufferUsagePercent;
        private int nBytesReceived;
        private PacketParser.NetworkHostList networkHostList;
        private PopularityList<int, NetworkTcpSession> networkTcpSessionList;
        private int nFramesReceived;
        private List<IPacketHandler> nonIpPacketHandlerList;
        private List<IOsFingerprinter> osFingerprintCollectionList;
        private string outputDirectory;
        private List<IPacketHandler> packetHandlerList;
        private Thread packetQueueConsumerThread;
        private ISessionProtocolFinderFactory protocolFinderFactory;
        public const int RECEIVED_PACKETS_QUEUE_MAX_SIZE = 0x3e80;
        private LatestFramesQueue receivedFramesQueue;
        private Queue<PacketReceivedEventArgs> receivedPacketsQueue;
        private List<ReconstructedFile> reconstructedFileList;
        private List<ITcpSessionPacketHandler> tcpSessionPacketHandlerList;

        public event AnomalyEventHandler AnomalyDetected;

        public event BufferUsageEventHandler BufferUsageChanged;

        public event CleartextWordsEventHandler CleartextWordsDetected;

        public event CredentialEventHandler CredentialDetected;

        public event DnsRecordEventHandler DnsRecordDetected;

        public event FileEventHandler FileReconstructed;

        public event FrameEventHandler FrameDetected;

        public event KeywordEventHandler KeywordDetected;

        public event MessageEventHandler MessageDetected;

        public event NetworkHostEventHandler NetworkHostDetected;

        public event ParameterEventHandler ParametersDetected;

        public event SessionEventHandler SessionDetected;

        public PacketHandler(string applicationExecutablePath, string outputPath)
        {
            ThreadStart start = null;
            ThreadStart start2 = null;
            this.protocolFinderFactory = new PortProtocolFinderFactory(this);
            this.networkHostList = new PacketParser.NetworkHostList();
            this.nFramesReceived = 0;
            this.nBytesReceived = 0;
            this.receivedFramesQueue = new LatestFramesQueue(0x100);
            this.dictionary = new WordDictionary();
            this.lastBufferUsagePercent = null;
            this.receivedPacketsQueue = new Queue<PacketReceivedEventArgs>(0x3e80);
            this.framesToParseQueue = new Queue<Frame>(0x3e80);
            if (start == null)
            {
                start = () => this.CreateFramesFromPacketsInPacketQueue();
            }
            this.packetQueueConsumerThread = new Thread(start);
            if (start2 == null)
            {
                start2 = () => this.ParseFramesInFrameQueue();
            }
            this.frameQueueConsumerThread = new Thread(start2);
            string str = Path.GetDirectoryName(applicationExecutablePath) + Path.DirectorySeparatorChar;
            if (!outputPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
            {
                outputPath = outputPath + Path.DirectorySeparatorChar.ToString();
            }
            this.outputDirectory = Path.GetDirectoryName(outputPath) + Path.DirectorySeparatorChar;
            this.osFingerprintCollectionList = new List<IOsFingerprinter>();
            try
            {
                this.osFingerprintCollectionList.Add(new EttarcapOsFingerprintCollection(string.Concat(new object[] { str, "Fingerprints", Path.DirectorySeparatorChar, "etter.finger.os" })));
            }
            catch (FileNotFoundException)
            {
            }
            this.osFingerprintCollectionList.Add(new P0fOsFingerprintCollection(string.Concat(new object[] { str, "Fingerprints", Path.DirectorySeparatorChar, "p0f.fp" }), string.Concat(new object[] { str, Path.DirectorySeparatorChar, "Fingerprints", Path.DirectorySeparatorChar, "p0fa.fp" })));
            this.osFingerprintCollectionList.Add(new SatoriDhcpOsFingerprinter(string.Concat(new object[] { str, "Fingerprints", Path.DirectorySeparatorChar, "dhcp.xml" })));
            this.osFingerprintCollectionList.Add(new SatoriTcpOsFingerprinter(string.Concat(new object[] { str, "Fingerprints", Path.DirectorySeparatorChar, "tcp.xml" })));
            this.networkTcpSessionList = new PopularityList<int, NetworkTcpSession>(200);
            this.networkTcpSessionList.PopularityLost += new PopularityList<int, NetworkTcpSession>.PopularityLostEventHandler(this.networkTcpSessionList_PopularityLost);
            this.fileStreamAssemblerList = new PacketParser.FileTransfer.FileStreamAssemblerList(this, 100, this.outputDirectory + "AssembledFiles" + Path.DirectorySeparatorChar);
            this.reconstructedFileList = new List<ReconstructedFile>();
            this.credentialList = new SortedList<string, PacketParser.NetworkCredential>();
            this.nonIpPacketHandlerList = new List<IPacketHandler>();
            this.packetHandlerList = new List<IPacketHandler>();
            this.tcpSessionPacketHandlerList = new List<ITcpSessionPacketHandler>();
            this.nonIpPacketHandlerList.Add(new HpSwitchProtocolPacketHandler(this));
            this.packetHandlerList.Add(new DnsPacketHandler(this));
            this.packetHandlerList.Add(new TftpPacketHandler(this));
            this.packetHandlerList.Add(new NetBiosDatagramServicePacketHandler(this));
            this.packetHandlerList.Add(new NetBiosNameServicePacketHandler(this));
            this.packetHandlerList.Add(new UpnpPacketHandler(this));
            this.packetHandlerList.Add(new DhcpPacketHandler(this));
            this.packetHandlerList.Add(new SipPacketHandler(this));
            this.packetHandlerList.Add(new SyslogPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new FtpPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new SmtpPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new HttpPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new SmbCommandPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new NetBiosSessionServicePacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new NtlmSspPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new TlsRecordPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new TabularDataStreamPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new SpotifyKeyExchangePacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new SshPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new IrcPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new OscarFileTransferPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new OscarPacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new IEC_104_PacketHandler(this));
            this.tcpSessionPacketHandlerList.Add(new UnusedTcpSessionProtocolsHandler(this));
            this.keywordList = new byte[0][];
        }

        public void AbortBackgroundThreads()
        {
            this.packetQueueConsumerThread.Abort();
            this.frameQueueConsumerThread.Abort();
        }

        internal void AddCredential(PacketParser.NetworkCredential credential)
        {
            if (!this.credentialList.ContainsKey(credential.Key))
            {
                this.credentialList.Add(credential.Key, credential);
                if (credential.Password != null)
                {
                    this.OnCredentialDetected(new CredentialEventArgs(credential));
                }
            }
        }

        public void AddFrameToFrameParsingQueue(Frame frame)
        {
            if (frame != null)
            {
                lock (this.framesToParseQueue)
                {
                    this.framesToParseQueue.Enqueue(frame);
                }
            }
        }

        private void AddNetworkTcpSessionToPool(NetworkTcpSession session)
        {
            int hashCode = session.GetHashCode();
            if (this.networkTcpSessionList.ContainsKey(hashCode))
            {
                this.networkTcpSessionList[hashCode] = session;
            }
            else
            {
                this.networkTcpSessionList.Add(hashCode, session);
            }
        }

        internal void AddReconstructedFile(ReconstructedFile file)
        {
            this.reconstructedFileList.Add(file);
            this.OnFileReconstructed(new FileEventArgs(file));
        }

        private void CheckFrameCleartext(Frame frame)
        {
            int wordCharCount = 0;
            int totalByteCount = 0;
            IEnumerable<string> cleartextWords = null;
            if (this.cleartextSearchModeSelectedIndex == 0)
            {
                cleartextWords = this.GetCleartextWords(frame.Data);
                totalByteCount = frame.Data.Length;
            }
            else if (this.cleartextSearchModeSelectedIndex == 1)
            {
                foreach (AbstractPacket packet in frame.PacketList)
                {
                    if (packet.GetType() == typeof(TcpPacket))
                    {
                        TcpPacket packet2 = (TcpPacket) packet;
                        cleartextWords = this.GetCleartextWords(packet2.ParentFrame.Data, packet2.PacketStartIndex + packet2.DataOffsetByteCount, packet2.PacketEndIndex);
                        totalByteCount = ((packet2.PacketEndIndex - packet2.DataOffsetByteCount) - packet2.PacketStartIndex) + 1;
                    }
                    else if (packet.GetType() == typeof(UdpPacket))
                    {
                        UdpPacket packet3 = (UdpPacket) packet;
                        cleartextWords = this.GetCleartextWords(packet3.ParentFrame.Data, packet3.PacketStartIndex + packet3.DataOffsetByteCount, packet3.PacketEndIndex);
                        totalByteCount = ((packet3.PacketEndIndex - packet3.DataOffsetByteCount) - packet3.PacketStartIndex) + 1;
                    }
                }
            }
            else if (this.cleartextSearchModeSelectedIndex == 2)
            {
                foreach (AbstractPacket packet4 in frame.PacketList)
                {
                    if (packet4.GetType() == typeof(RawPacket))
                    {
                        cleartextWords = this.GetCleartextWords(packet4);
                        totalByteCount = packet4.PacketByteCount;
                    }
                }
            }
            if ((totalByteCount > 0) && (cleartextWords != null))
            {
                List<string> words = new List<string>();
                foreach (string str in cleartextWords)
                {
                    wordCharCount += str.Length;
                    words.Add(str);
                }
                if (words.Count > 0)
                {
                    this.OnCleartextWordsDetected(new CleartextWordsEventArgs(words, wordCharCount, totalByteCount, frame.FrameNumber, frame.Timestamp));
                }
            }
        }

        internal void CreateFramesFromPacketsInPacketQueue()
        {
            while (true)
            {
                while ((this.receivedPacketsQueue.Count > 0) && (this.framesToParseQueue.Count < 0x3e80))
                {
                    PacketReceivedEventArgs args;
                    lock (this.receivedPacketsQueue)
                    {
                        args = this.receivedPacketsQueue.Dequeue();
                    }
                    this.UpdateBufferUsagePercent();
                    Frame frame = this.GetFrame(args);
                    this.AddFrameToFrameParsingQueue(frame);
                }
                Thread.Sleep(50);
            }
        }

        private void ExtractArpData(Ethernet2Packet ethernet2Packet, ArpPacket arpPacket)
        {
            if ((arpPacket.SenderIPAddress != null) && (ethernet2Packet != null))
            {
                this.ExtractArpData(ethernet2Packet.SourceMACAddress, arpPacket);
            }
        }

        private void ExtractArpData(IEEE_802_11Packet wlanPacket, ArpPacket arpPacket)
        {
            if ((arpPacket.SenderIPAddress != null) && (wlanPacket != null))
            {
                this.ExtractArpData(wlanPacket.SourceMAC, arpPacket);
            }
        }

        private void ExtractArpData(PhysicalAddress sourceMAC, ArpPacket arpPacket)
        {
            if (sourceMAC != null)
            {
                if (arpPacket.SenderHardwareAddress.Equals(sourceMAC))
                {
                    NetworkHost host = null;
                    if (!this.networkHostList.ContainsIP(arpPacket.SenderIPAddress))
                    {
                        host = new NetworkHost(arpPacket.SenderIPAddress) {
                            MacAddress = arpPacket.SenderHardwareAddress
                        };
                        this.networkHostList.Add(host);
                        this.OnNetworkHostDetected(new NetworkHostEventArgs(host));
                    }
                    if (host != null)
                    {
                        host.AddQueriedIP(arpPacket.TargetIPAddress);
                    }
                }
                else
                {
                    this.OnAnomalyDetected(string.Concat(new object[] { "Different source MAC addresses in Ethernet and ARP packet: Ethernet MAC=", sourceMAC, ", ARP MAC=", arpPacket.SenderHardwareAddress, ", ARP IP=", arpPacket.SenderIPAddress, " (frame: ", arpPacket.ParentFrame.ToString(), ")" }), arpPacket.ParentFrame.Timestamp);
                }
            }
        }

        internal void ExtractMultipartFormData(IEnumerable<MultipartPart> formMultipartData, NetworkHost sourceHost, NetworkHost destinationHost, DateTime timestamp, int frameNumber, string sourcePort, string destinationPort, ApplicationLayerProtocol applicationLayerProtocol)
        {
            this.ExtractMultipartFormData(formMultipartData, sourceHost, destinationHost, timestamp, frameNumber, sourcePort, destinationPort, applicationLayerProtocol, null);
        }

        internal void ExtractMultipartFormData(IEnumerable<MultipartPart> formMultipartData, NetworkHost sourceHost, NetworkHost destinationHost, DateTime timestamp, int frameNumber, string sourcePort, string destinationPort, ApplicationLayerProtocol applicationLayerProtocol, NameValueCollection cookieParams)
        {
            NameValueCollection parameters = new NameValueCollection();
            foreach (MultipartPart part in formMultipartData)
            {
                if ((part.Attributes != null) && (part.Attributes.Count > 0))
                {
                    if ((part.Data != null) && (part.Data.Length > 0))
                    {
                        string name = part.Attributes["name"];
                        foreach (string str2 in part.Attributes)
                        {
                            if (str2 == "name")
                            {
                                name = part.Attributes["name"];
                            }
                            else
                            {
                                parameters.Add(str2, part.Attributes[str2]);
                            }
                        }
                        int lenght = 250;
                        string str3 = ByteConverter.ReadString(part.Data, 0, lenght).Trim();
                        if ((name != null) && (name.Length > 0))
                        {
                            if ((str3 != null) && (str3.Length > 0))
                            {
                                parameters.Add(name, str3);
                            }
                            else
                            {
                                parameters.Add("name", name);
                            }
                        }
                    }
                    else
                    {
                        parameters.Add(part.Attributes);
                    }
                }
            }
            if (parameters.Count > 0)
            {
                this.OnParametersDetected(new ParametersEventArgs(frameNumber, sourceHost, destinationHost, sourcePort, destinationPort, parameters, timestamp, "HTTP POST"));
            }
            PacketParser.NetworkCredential credential = PacketParser.NetworkCredential.GetNetworkCredential(parameters, sourceHost, destinationHost, "HTTP POST", timestamp);
            if (((credential != null) && (credential.Username != null)) && (credential.Password != null))
            {
                this.AddCredential(credential);
            }
            if (cookieParams != null)
            {
                foreach (string str4 in cookieParams.Keys)
                {
                    if (parameters[str4] == null)
                    {
                        parameters[str4] = cookieParams[str4];
                    }
                }
            }
            MessageEventArgs me = this.GetMessageEventArgs(applicationLayerProtocol, sourceHost, destinationHost, frameNumber, timestamp, parameters);
            if (me != null)
            {
                this.MessageDetected(this, me);
            }
        }

        private void ExtractTcpSessionData(NetworkHost sourceHost, NetworkHost destinationHost, NetworkTcpSession networkTcpSession, Frame receivedFrame, TcpPacket tcpPacket)
        {
            NetworkTcpSession.TcpDataStream clientToServerTcpDataStream = null;
            if (((networkTcpSession.ClientHost == sourceHost) && (networkTcpSession.ClientTcpPort == tcpPacket.SourcePort)) && (networkTcpSession.ClientToServerTcpDataStream != null))
            {
                clientToServerTcpDataStream = networkTcpSession.ClientToServerTcpDataStream;
            }
            else if (((networkTcpSession.ServerHost == sourceHost) && (networkTcpSession.ServerTcpPort == tcpPacket.SourcePort)) && (networkTcpSession.ServerToClientTcpDataStream != null))
            {
                clientToServerTcpDataStream = networkTcpSession.ServerToClientTcpDataStream;
            }
            else if (((networkTcpSession.ClientHost != sourceHost) || (networkTcpSession.ClientTcpPort != tcpPacket.SourcePort)) && ((networkTcpSession.ServerHost != sourceHost) || (networkTcpSession.ServerTcpPort != tcpPacket.SourcePort)))
            {
                throw new Exception("Wrong TCP Session received");
            }
            if ((clientToServerTcpDataStream != null) && (tcpPacket.PayloadDataLength > 0))
            {
                for (NetworkTcpSession.TcpDataStream.VirtualTcpData data = clientToServerTcpDataStream.GetNextVirtualTcpData(); (data != null) && (clientToServerTcpDataStream.CountBytesToRead() > 0); data = clientToServerTcpDataStream.GetNextVirtualTcpData())
                {
                    if (this.fileStreamAssemblerList.ContainsAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true, true))
                    {
                        FileStreamAssembler assembler = this.fileStreamAssemblerList.GetAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true);
                        if (((assembler.FileContentLength == -1) && (assembler.FileSegmentRemainingBytes == -1)) && tcpPacket.FlagBits.Fin)
                        {
                            assembler.SetRemainingBytesInFile(data.GetBytes(false).Length);
                            assembler.FileSegmentRemainingBytes = data.GetBytes(false).Length;
                        }
                        if (((assembler.FileStreamType == FileStreamTypes.HttpGetChunked) || (assembler.FileStreamType == FileStreamTypes.HttpPostMimeMultipartFormData)) || (((assembler.FileStreamType == FileStreamTypes.OscarFileTransfer) || (assembler.FileSegmentRemainingBytes >= data.ByteCount)) || ((assembler.FileContentLength == -1) && (assembler.FileSegmentRemainingBytes == -1))))
                        {
                            assembler.AddData(data.GetBytes(false), data.FirstPacketSequenceNumber);
                        }
                        clientToServerTcpDataStream.RemoveData(data);
                    }
                    else if ((networkTcpSession.RequiredNextTcpDataStream == null) || (networkTcpSession.RequiredNextTcpDataStream == clientToServerTcpDataStream))
                    {
                        if (networkTcpSession.RequiredNextTcpDataStream != null)
                        {
                            networkTcpSession.RequiredNextTcpDataStream = null;
                        }
                        byte[] bytes = data.GetBytes(true);
                        Frame frame = new Frame(receivedFrame.Timestamp, bytes, typeof(TcpPacket), receivedFrame.FrameNumber, false, false, bytes.Length);
                        List<AbstractPacket> packetList = new List<AbstractPacket>();
                        if (frame.BasePacket != null)
                        {
                            packetList.AddRange(((TcpPacket) frame.BasePacket).GetSubPackets(true, networkTcpSession.ProtocolFinder, clientToServerTcpDataStream == networkTcpSession.ClientToServerTcpDataStream));
                        }
                        int bytesToRemove = 0;
                        foreach (ITcpSessionPacketHandler handler in this.tcpSessionPacketHandlerList)
                        {
                            bytesToRemove += handler.ExtractData(networkTcpSession, sourceHost, destinationHost, packetList);
                        }
                        if (bytesToRemove >= data.ByteCount)
                        {
                            networkTcpSession.RemoveData(data, sourceHost, tcpPacket.SourcePort);
                        }
                        else if (bytesToRemove > 0)
                        {
                            networkTcpSession.RemoveData(data.FirstPacketSequenceNumber, bytesToRemove, sourceHost, tcpPacket.SourcePort);
                        }
                    }
                }
            }
            else
            {
                foreach (ITcpSessionPacketHandler handler2 in this.tcpSessionPacketHandlerList)
                {
                    handler2.ExtractData(networkTcpSession, sourceHost, destinationHost, receivedFrame.PacketList);
                }
            }
            if ((networkTcpSession.FinPacketReceived || networkTcpSession.SessionClosed) && this.fileStreamAssemblerList.ContainsAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true, true))
            {
                using (FileStreamAssembler assembler2 = this.fileStreamAssemblerList.GetAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true))
                {
                    if ((assembler2.IsActive && (assembler2.FileSegmentRemainingBytes <= 0)) && (assembler2.AssembledByteCount > 0))
                    {
                        assembler2.FinishAssembling();
                    }
                    else
                    {
                        this.fileStreamAssemblerList.Remove(assembler2, true);
                    }
                }
            }
        }

        private IEnumerable<string> GetCleartextWords(AbstractPacket packet)
        {
            return this.GetCleartextWords(packet.ParentFrame.Data, packet.PacketStartIndex, packet.PacketEndIndex);
        }

        private IEnumerable<string> GetCleartextWords(byte[] data)
        {
            return this.GetCleartextWords(data, 0, data.Length - 1);
        }

        private IEnumerable<string> GetCleartextWords(byte[] data, int startIndex, int endIndex)
        {
            StringBuilder iteratorVariable0 = null;
            for (int i = startIndex; i <= endIndex; i++)
            {
                if (!this.dictionary.IsLetter(data[i]))
                {
                    if (iteratorVariable0 != null)
                    {
                        if (this.dictionary.HasWord(iteratorVariable0.ToString()))
                        {
                            yield return iteratorVariable0.ToString();
                        }
                        iteratorVariable0 = null;
                    }
                }
                else if (iteratorVariable0 == null)
                {
                    iteratorVariable0 = new StringBuilder(Convert.ToString((char) data[i]));
                }
                else
                {
                    iteratorVariable0.Append((char) data[i]);
                }
            }
            if ((iteratorVariable0 != null) && this.dictionary.HasWord(iteratorVariable0.ToString()))
            {
                yield return iteratorVariable0.ToString();
            }
        }

        public IList<PacketParser.NetworkCredential> GetCredentials()
        {
            return this.credentialList.Values;
        }

        internal Frame GetFrame(PacketReceivedEventArgs packet)
        {
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.Ethernet2Packet)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(Ethernet2Packet), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.IPv4Packet)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(IPv4Packet), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.IPv6Packet)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(IPv6Packet), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.IEEE_802_11Packet)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(IEEE_802_11Packet), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.IEEE_802_11RadiotapPacket)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(IEEE_802_11RadiotapPacket), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.CiscoHDLC)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(CiscoHdlcPacket), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.LinuxCookedCapture)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(LinuxCookedCapture), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.PrismCaptureHeader)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(PrismCaptureHeaderPacket), ++this.nFramesReceived);
            }
            if (packet.PacketType == PacketReceivedEventArgs.PacketTypes.NullLoopback)
            {
                return new Frame(packet.Timestamp, packet.Data, typeof(NullLoopbackPacket), ++this.nFramesReceived);
            }
            return null;
        }

        public Frame GetFrame(DateTime timestamp, byte[] data, pcapFrame.DataLinkTypeEnum dataLinkType)
        {
            return new Frame(timestamp, data, PacketFactory.GetPacketType(dataLinkType), ++this.nFramesReceived);
        }

        private MessageEventArgs GetMessageEventArgs(ApplicationLayerProtocol applicationLayerProtocol, NetworkHost sourceHost, NetworkHost destinationHost, int frameNumber, DateTime timestamp, NameValueCollection parameters)
        {
            string from = null;
            string to = null;
            string subject = null;
            string message = null;
            string[] strArray = new string[] { "from", "From", "fFrom", "profile_id", "username", "guest_id", "author", "email", "anonName", "rawOpenId" };
            string[] strArray2 = new string[] { "to", "To", "req0_to", "fTo", "ids", "send_to", "emails[0]" };
            string[] strArray3 = new string[] { "subject", "Subj", "Subject", "fSubject" };
            string[] strArray4 = new string[] { "req0_text", "body", "Body", "message", "Message", "text", "Text", "fMessageBody", "status", "PlainBody", "RichBody", "comment", "postBody" };
            for (int i = 0; (i < strArray.Length) && ((from == null) || (from.Length == 0)); i++)
            {
                from = parameters[strArray[i]];
            }
            for (int j = 0; (j < strArray2.Length) && ((to == null) || (to.Length == 0)); j++)
            {
                to = parameters[strArray2[j]];
            }
            for (int k = 0; (k < strArray3.Length) && ((subject == null) || (subject.Length == 0)); k++)
            {
                subject = parameters[strArray3[k]];
            }
            for (int m = 0; (m < strArray4.Length) && ((message == null) || (message.Length == 0)); m++)
            {
                message = parameters[strArray4[m]];
            }
            if (((subject == null) && (message != null)) && (message.Length > 0))
            {
                subject = message;
            }
            if (((subject == null) || (subject.Length <= 0)) || ((from == null) && (to == null)))
            {
                return null;
            }
            return new MessageEventArgs(applicationLayerProtocol, sourceHost, destinationHost, frameNumber, timestamp, from, to, subject, message, parameters);
        }

        private NetworkTcpSession GetNetworkTcpSession(TcpPacket tcpPacket, NetworkHost sourceHost, NetworkHost destinationHost)
        {
            if (tcpPacket.FlagBits.Synchronize)
            {
                if (!tcpPacket.FlagBits.Acknowledgement)
                {
                    NetworkTcpSession session = new NetworkTcpSession(tcpPacket, sourceHost, destinationHost, this.protocolFinderFactory);
                    this.AddNetworkTcpSessionToPool(session);
                    return session;
                }
                int num = NetworkTcpSession.GetHashCode(destinationHost, sourceHost, tcpPacket.DestinationPort, tcpPacket.SourcePort);
                if (this.networkTcpSessionList.ContainsKey(num))
                {
                    NetworkTcpSession session2 = this.networkTcpSessionList[num];
                    if (session2.SynPacketReceived && !session2.SynAckPacketReceived)
                    {
                        return session2;
                    }
                }
                return null;
            }
            int key = NetworkTcpSession.GetHashCode(sourceHost, destinationHost, tcpPacket.SourcePort, tcpPacket.DestinationPort);
            int num3 = NetworkTcpSession.GetHashCode(destinationHost, sourceHost, tcpPacket.DestinationPort, tcpPacket.SourcePort);
            if (this.networkTcpSessionList.ContainsKey(key))
            {
                NetworkTcpSession session3 = this.networkTcpSessionList[key];
                if (session3.SynAckPacketReceived)
                {
                    return session3;
                }
                return null;
            }
            if (this.networkTcpSessionList.ContainsKey(num3))
            {
                NetworkTcpSession session4 = this.networkTcpSessionList[num3];
                if (session4.SynAckPacketReceived)
                {
                    return session4;
                }
                return null;
            }
            NetworkTcpSession session5 = new NetworkTcpSession(sourceHost, destinationHost, tcpPacket, this.protocolFinderFactory);
            this.AddNetworkTcpSessionToPool(session5);
            return session5;
        }

        private void networkTcpSessionList_PopularityLost(int key, NetworkTcpSession value)
        {
            value.Close();
        }

        internal virtual void OnAnomalyDetected(AnomalyEventArgs ae)
        {
            if (this.AnomalyDetected != null)
            {
                this.AnomalyDetected(this, ae);
            }
        }

        internal virtual void OnAnomalyDetected(string anomalyMessage)
        {
            this.OnAnomalyDetected(anomalyMessage, DateTime.Now);
        }

        internal virtual void OnAnomalyDetected(string anomalyMessage, DateTime anomalyTimestamp)
        {
            this.OnAnomalyDetected(new AnomalyEventArgs(anomalyMessage, anomalyTimestamp));
        }

        internal virtual void OnBufferUsageChanged(BufferUsageEventArgs be)
        {
            if (this.BufferUsageChanged != null)
            {
                this.BufferUsageChanged(this, be);
            }
        }

        internal virtual void OnCleartextWordsDetected(CleartextWordsEventArgs ce)
        {
            if (this.CleartextWordsDetected != null)
            {
                this.CleartextWordsDetected(this, ce);
            }
        }

        internal virtual void OnCredentialDetected(CredentialEventArgs ce)
        {
            if (this.CredentialDetected != null)
            {
                this.CredentialDetected(this, ce);
            }
        }

        internal virtual void OnDnsRecordDetected(DnsRecordEventArgs de)
        {
            if (this.DnsRecordDetected != null)
            {
                this.DnsRecordDetected(this, de);
            }
        }

        internal virtual void OnFileReconstructed(FileEventArgs fe)
        {
            if (this.FileReconstructed != null)
            {
                this.FileReconstructed(this, fe);
            }
        }

        internal virtual void OnFrameDetected(FrameEventArgs fe)
        {
            if (this.FrameDetected != null)
            {
                this.FrameDetected(this, fe);
            }
        }

        internal virtual void OnKeywordDetected(KeywordEventArgs ke)
        {
            if (this.KeywordDetected != null)
            {
                this.KeywordDetected(this, ke);
            }
        }

        internal virtual void OnMessageDetected(MessageEventArgs me)
        {
            if (this.MessageDetected != null)
            {
                this.MessageDetected(this, me);
            }
        }

        internal virtual void OnNetworkHostDetected(NetworkHostEventArgs he)
        {
            if (this.NetworkHostDetected != null)
            {
                this.NetworkHostDetected(this, he);
            }
        }

        internal virtual void OnParametersDetected(ParametersEventArgs pe)
        {
            if (this.ParametersDetected != null)
            {
                this.ParametersDetected(this, pe);
            }
        }

        internal virtual void OnSessionDetected(SessionEventArgs se)
        {
            if (this.SessionDetected != null)
            {
                this.SessionDetected(this, se);
            }
        }

        internal void ParseFrame(Frame receivedFrame)
        {
            if (receivedFrame != null)
            {
                this.OnFrameDetected(new FrameEventArgs(receivedFrame));
                this.nBytesReceived += receivedFrame.Data.Length;
                this.receivedFramesQueue.Enqueue(receivedFrame);
                Ethernet2Packet packet = null;
                IEEE_802_11Packet packet2 = null;
                ArpPacket arpPacket = null;
                IPv4Packet packet4 = null;
                IPv6Packet packet5 = null;
                TcpPacket tcpPacket = null;
                UdpPacket udpPacket = null;
                foreach (AbstractPacket packet8 in receivedFrame.PacketList)
                {
                    if (packet8.GetType() == typeof(IPv4Packet))
                    {
                        packet4 = (IPv4Packet) packet8;
                    }
                    else if (packet8.GetType() == typeof(IPv6Packet))
                    {
                        packet5 = (IPv6Packet) packet8;
                    }
                    else if (packet8.GetType() == typeof(TcpPacket))
                    {
                        tcpPacket = (TcpPacket) packet8;
                    }
                    else if (packet8.GetType() == typeof(UdpPacket))
                    {
                        udpPacket = (UdpPacket) packet8;
                    }
                    else if (packet8.GetType() == typeof(Ethernet2Packet))
                    {
                        packet = (Ethernet2Packet) packet8;
                    }
                    else if (packet8.GetType() == typeof(IEEE_802_11Packet))
                    {
                        packet2 = (IEEE_802_11Packet) packet8;
                    }
                    else if (packet8.GetType() == typeof(ArpPacket))
                    {
                        arpPacket = (ArpPacket) packet8;
                    }
                    else if (packet8.GetType() == typeof(RawPacket))
                    {
                        RawPacket packet1 = (RawPacket) packet8;
                    }
                }
                if ((packet != null) && (arpPacket != null))
                {
                    this.ExtractArpData(packet, arpPacket);
                }
                NetworkPacket packet9 = null;
                if ((packet4 == null) && (packet5 == null))
                {
                    foreach (IPacketHandler handler in this.nonIpPacketHandlerList)
                    {
                        try
                        {
                            NetworkHost sourceHost = new NetworkHost(IPAddress.None);
                            handler.ExtractData(ref sourceHost, null, receivedFrame.PacketList);
                        }
                        catch (Exception exception)
                        {
                            this.OnAnomalyDetected("Error applying " + handler.ToString() + " packet handler to frame " + receivedFrame.ToString() + ": " + exception.Message, receivedFrame.Timestamp);
                        }
                    }
                }
                else if ((packet4 != null) || (packet5 != null))
                {
                    byte hopLimit;
                    IPAddress sourceIPAddress;
                    IPAddress destinationIPAddress;
                    AbstractPacket packet10;
                    NetworkHost networkHost;
                    NetworkHost host3;
                    if (packet5 != null)
                    {
                        hopLimit = packet5.HopLimit;
                        sourceIPAddress = packet5.SourceIPAddress;
                        destinationIPAddress = packet5.DestinationIPAddress;
                        packet10 = packet5;
                    }
                    else
                    {
                        hopLimit = packet4.TimeToLive;
                        sourceIPAddress = packet4.SourceIPAddress;
                        destinationIPAddress = packet4.DestinationIPAddress;
                        packet10 = packet4;
                    }
                    if (this.networkHostList.ContainsIP(sourceIPAddress))
                    {
                        networkHost = this.networkHostList.GetNetworkHost(sourceIPAddress);
                    }
                    else
                    {
                        networkHost = new NetworkHost(sourceIPAddress);
                        this.networkHostList.Add(networkHost);
                        this.OnNetworkHostDetected(new NetworkHostEventArgs(networkHost));
                    }
                    if (this.networkHostList.ContainsIP(destinationIPAddress))
                    {
                        host3 = this.networkHostList.GetNetworkHost(destinationIPAddress);
                    }
                    else
                    {
                        host3 = new NetworkHost(destinationIPAddress);
                        this.networkHostList.Add(host3);
                        this.OnNetworkHostDetected(new NetworkHostEventArgs(host3));
                    }
                    packet9 = new NetworkPacket(networkHost, host3, packet10);
                    if (packet != null)
                    {
                        if (networkHost.MacAddress != packet.SourceMACAddress)
                        {
                            if (((networkHost.MacAddress != null) && (packet.SourceMACAddress != null)) && ((networkHost.MacAddress.ToString() != packet.SourceMACAddress.ToString()) && !networkHost.IsRecentMacAddress(packet.SourceMACAddress)))
                            {
                                this.OnAnomalyDetected(string.Concat(new object[] { "Ethernet MAC has changed, possible ARP spoofing! IP ", networkHost.IPAddress.ToString(), ", MAC ", networkHost.MacAddress.ToString(), " -> ", packet.SourceMACAddress.ToString(), " (frame ", receivedFrame.FrameNumber, ")" }), receivedFrame.Timestamp);
                            }
                            networkHost.MacAddress = packet.SourceMACAddress;
                        }
                        if (host3.MacAddress != packet.DestinationMACAddress)
                        {
                            if (((host3.MacAddress != null) && (packet.DestinationMACAddress != null)) && ((host3.MacAddress.ToString() != packet.DestinationMACAddress.ToString()) && !host3.IsRecentMacAddress(packet.DestinationMACAddress)))
                            {
                                this.OnAnomalyDetected(string.Concat(new object[] { "Ethernet MAC has changed, possible ARP spoofing! IP ", host3.IPAddress.ToString(), ", MAC ", host3.MacAddress.ToString(), " -> ", packet.DestinationMACAddress.ToString(), " (frame ", receivedFrame.FrameNumber, ")" }), receivedFrame.Timestamp);
                            }
                            host3.MacAddress = packet.DestinationMACAddress;
                        }
                    }
                    else if (packet2 != null)
                    {
                        networkHost.MacAddress = packet2.SourceMAC;
                        host3.MacAddress = packet2.DestinationMAC;
                    }
                    if (tcpPacket != null)
                    {
                        packet9.SetTcpData(tcpPacket);
                        NetworkTcpSession networkTcpSession = this.GetNetworkTcpSession(tcpPacket, networkHost, host3);
                        if ((networkTcpSession != null) && !networkTcpSession.TryAddPacket(tcpPacket, networkHost, host3))
                        {
                            networkTcpSession = null;
                        }
                        if (networkTcpSession != null)
                        {
                            this.ExtractTcpSessionData(networkHost, host3, networkTcpSession, receivedFrame, tcpPacket);
                        }
                    }
                    else if (udpPacket != null)
                    {
                        packet9.SetUdpData(udpPacket);
                    }
                    networkHost.AddTtl(hopLimit);
                    if (networkHost.TtlDistance == 0xff)
                    {
                        foreach (IOsFingerprinter fingerprinter in this.osFingerprintCollectionList)
                        {
                            if (typeof(ITtlDistanceCalculator).IsAssignableFrom(fingerprinter.GetType()))
                            {
                                networkHost.AddProbableTtlDistance(((ITtlDistanceCalculator) fingerprinter).GetTtlDistance(hopLimit));
                            }
                        }
                    }
                    foreach (IPacketHandler handler2 in this.packetHandlerList)
                    {
                        try
                        {
                            handler2.ExtractData(ref networkHost, host3, receivedFrame.PacketList);
                        }
                        catch (Exception exception2)
                        {
                            this.OnAnomalyDetected("Error applying " + handler2.ToString() + " packet handler to frame " + receivedFrame.ToString() + ": " + exception2.Message, receivedFrame.Timestamp);
                        }
                    }
                    foreach (IOsFingerprinter fingerprinter2 in this.osFingerprintCollectionList)
                    {
                        IList<string> list;
                        if ((fingerprinter2.TryGetOperatingSystems(out list, receivedFrame.PacketList) && (list != null)) && (list.Count > 0))
                        {
                            byte num2;
                            foreach (string str in list)
                            {
                                networkHost.AddProbableOs(fingerprinter2.Name, str, 1.0 / ((double) list.Count));
                            }
                            if (typeof(ITtlDistanceCalculator).IsAssignableFrom(fingerprinter2.GetType()) && ((ITtlDistanceCalculator) fingerprinter2).TryGetTtlDistance(out num2, receivedFrame.PacketList))
                            {
                                networkHost.AddProbableTtlDistance(num2);
                            }
                        }
                    }
                }
                if (packet9 != null)
                {
                    packet9.SourceHost.SentPackets.Add(packet9);
                    packet9.DestinationHost.ReceivedPackets.Add(packet9);
                }
                this.CheckFrameCleartext(receivedFrame);
                foreach (byte[] buffer in this.keywordList)
                {
                    int index = receivedFrame.IndexOf(buffer);
                    if (index >= 0)
                    {
                        if (packet9 != null)
                        {
                            ushort? sourceTcpPort = packet9.SourceTcpPort;
                            int? nullable3 = sourceTcpPort.HasValue ? new int?(sourceTcpPort.GetValueOrDefault()) : null;
                            if (nullable3.HasValue)
                            {
                                ushort? destinationTcpPort = packet9.DestinationTcpPort;
                                int? nullable6 = destinationTcpPort.HasValue ? new int?(destinationTcpPort.GetValueOrDefault()) : null;
                                if (nullable6.HasValue)
                                {
                                    this.OnKeywordDetected(new KeywordEventArgs(receivedFrame, index, buffer.Length, packet9.SourceHost, packet9.DestinationHost, "TCP " + packet9.SourceTcpPort.ToString(), "TCP " + packet9.DestinationTcpPort.ToString()));
                                    continue;
                                }
                            }
                            ushort? sourceUdpPort = packet9.SourceUdpPort;
                            int? nullable11 = sourceUdpPort.HasValue ? new int?(sourceUdpPort.GetValueOrDefault()) : null;
                            if (nullable11.HasValue)
                            {
                                ushort? destinationUdpPort = packet9.DestinationUdpPort;
                                int? nullable14 = destinationUdpPort.HasValue ? new int?(destinationUdpPort.GetValueOrDefault()) : null;
                                if (nullable14.HasValue)
                                {
                                    this.OnKeywordDetected(new KeywordEventArgs(receivedFrame, index, buffer.Length, packet9.SourceHost, packet9.DestinationHost, "UDP " + packet9.SourceUdpPort.ToString(), "UDP " + packet9.DestinationUdpPort.ToString()));
                                    continue;
                                }
                            }
                            this.OnKeywordDetected(new KeywordEventArgs(receivedFrame, index, buffer.Length, packet9.SourceHost, packet9.DestinationHost, "", ""));
                        }
                        else
                        {
                            this.OnKeywordDetected(new KeywordEventArgs(receivedFrame, index, buffer.Length, null, null, "", ""));
                        }
                    }
                }
            }
        }

        internal void ParseFramesInFrameQueue()
        {
            while (true)
            {
                while (this.framesToParseQueue.Count > 0)
                {
                    Frame frame;
                    lock (this.framesToParseQueue)
                    {
                        frame = this.framesToParseQueue.Dequeue();
                    }
                    this.UpdateBufferUsagePercent();
                    this.ParseFrame(frame);
                }
                Thread.Sleep(50);
            }
        }

        public void ResetCapturedData()
        {
            lock (this.receivedPacketsQueue)
            {
                lock (this.networkHostList)
                {
                    this.networkHostList.Clear();
                }
                this.nFramesReceived = 0;
                this.nBytesReceived = 0;
                lock (this.receivedFramesQueue)
                {
                    this.receivedFramesQueue.Clear();
                }
                this.fileStreamAssemblerList.ClearAll();
                this.networkTcpSessionList.Clear();
                lock (Ipv4Fragments)
                {
                    Ipv4Fragments.Clear();
                }
                lock (this.reconstructedFileList)
                {
                    this.reconstructedFileList.Clear();
                }
                lock (this.credentialList)
                {
                    this.credentialList.Clear();
                }
                this.lastBufferUsagePercent = null;
                foreach (IPacketHandler handler in this.packetHandlerList)
                {
                    handler.Reset();
                }
                foreach (ITcpSessionPacketHandler handler2 in this.tcpSessionPacketHandlerList)
                {
                    handler2.Reset();
                }
                this.receivedPacketsQueue.Clear();
            }
        }

        public void StartBackgroundThreads()
        {
            this.packetQueueConsumerThread.Start();
            this.frameQueueConsumerThread.Start();
        }

        public bool TryEnqueueReceivedPacket(object sender, PacketReceivedEventArgs packet)
        {
            if (this.receivedPacketsQueue.Count < 0x3e80)
            {
                lock (this.receivedPacketsQueue)
                {
                    this.receivedPacketsQueue.Enqueue(packet);
                }
                this.OnBufferUsageChanged(new BufferUsageEventArgs((this.receivedPacketsQueue.Count * 100) / 0x3e80));
                return true;
            }
            this.OnAnomalyDetected("Packet dropped");
            return false;
        }

        private void UpdateBufferUsagePercent()
        {
            int num = Math.Max(this.receivedPacketsQueue.Count, this.framesToParseQueue.Count / 2);
            int num2 = (num * 100) / 0x3e80;
            if (!this.lastBufferUsagePercent.HasValue || (num2 != this.lastBufferUsagePercent.Value))
            {
                this.lastBufferUsagePercent = new int?(num2);
                this.OnBufferUsageChanged(new BufferUsageEventArgs((num * 100) / 0x3e80));
            }
        }

        public int CleartextSearchModeSelectedIndex
        {
            set
            {
                this.cleartextSearchModeSelectedIndex = value;
            }
        }

        public WordDictionary Dictionary
        {
            set
            {
                this.dictionary = value;
            }
        }

        public PacketParser.FileTransfer.FileStreamAssemblerList FileStreamAssemblerList
        {
            get
            {
                return this.fileStreamAssemblerList;
            }
        }

        public int FramesInQueue
        {
            get
            {
                return this.framesToParseQueue.Count;
            }
        }

        public byte[][] KeywordList
        {
            set
            {
                this.keywordList = value;
            }
        }

        public PacketParser.NetworkHostList NetworkHostList
        {
            get
            {
                return this.networkHostList;
            }
        }

        public List<IOsFingerprinter> OsFingerprintCollectionList
        {
            get
            {
                return this.osFingerprintCollectionList;
            }
        }

        public string OutputDirectory
        {
            get
            {
                return this.outputDirectory;
            }
        }

        public int PacketsInQueue
        {
            get
            {
                return this.receivedPacketsQueue.Count;
            }
        }

        public ISessionProtocolFinderFactory ProtocolFinderFactory
        {
            get
            {
                return this.protocolFinderFactory;
            }
            set
            {
                this.protocolFinderFactory = value;
            }
        }

        public List<ReconstructedFile> ReconstructedFileList
        {
            get
            {
                return this.reconstructedFileList;
            }
        }

    }
}

