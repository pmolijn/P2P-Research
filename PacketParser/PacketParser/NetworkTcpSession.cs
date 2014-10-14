namespace PacketParser
{
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class NetworkTcpSession : IComparable
    {
        private NetworkHost clientHost;
        private ushort clientTcpPort;
        private uint clientToServerFinPacketSequenceNumber;
        private TcpDataStream clientToServerTcpDataStream;
        private bool finPacketReceived;
        private DateTime latestPacketTimestamp;
        private ISessionProtocolFinder protocolFinder;
        private bool? requiredNextTcpDataStreamIsClientToServer;
        private NetworkHost serverHost;
        private ushort serverTcpPort;
        private uint serverToClientFinPacketSequenceNumber;
        private TcpDataStream serverToClientTcpDataStream;
        private bool sessionClosed;
        private bool sessionEstablished;
        private int startFrameNumber;
        private bool synAckPacketReceived;
        private bool synPacketReceived;
        private DateTime synPacketTimestamp;

        internal NetworkTcpSession(NetworkHost sourceHost, NetworkHost destinationHost, TcpPacket tcpPacket, ISessionProtocolFinderFactory protocolFinderFactory)
        {
            this.requiredNextTcpDataStreamIsClientToServer = null;
            this.synPacketTimestamp = tcpPacket.ParentFrame.Timestamp;
            this.synPacketReceived = true;
            this.synAckPacketReceived = true;
            this.finPacketReceived = false;
            this.sessionEstablished = false;
            this.sessionClosed = false;
            this.startFrameNumber = tcpPacket.ParentFrame.FrameNumber;
            this.clientToServerTcpDataStream = null;
            this.serverToClientTcpDataStream = null;
            List<ApplicationLayerProtocol> list = new List<ApplicationLayerProtocol>(TcpPortProtocolFinder.GetProbableApplicationLayerProtocols(tcpPacket.SourcePort, tcpPacket.DestinationPort));
            List<ApplicationLayerProtocol> list2 = new List<ApplicationLayerProtocol>(TcpPortProtocolFinder.GetProbableApplicationLayerProtocols(tcpPacket.DestinationPort, tcpPacket.SourcePort));
            if (list.Count > 0)
            {
                this.clientHost = destinationHost;
                this.serverHost = sourceHost;
                this.clientTcpPort = tcpPacket.DestinationPort;
                this.serverTcpPort = tcpPacket.SourcePort;
                this.SetEstablished(tcpPacket.AcknowledgmentNumber, tcpPacket.SequenceNumber);
            }
            else if (list2.Count > 0)
            {
                this.clientHost = sourceHost;
                this.serverHost = destinationHost;
                this.clientTcpPort = tcpPacket.SourcePort;
                this.serverTcpPort = tcpPacket.DestinationPort;
                this.SetEstablished(tcpPacket.SequenceNumber, tcpPacket.AcknowledgmentNumber);
            }
            else if (tcpPacket.SourcePort < tcpPacket.DestinationPort)
            {
                this.clientHost = destinationHost;
                this.serverHost = sourceHost;
                this.clientTcpPort = tcpPacket.DestinationPort;
                this.serverTcpPort = tcpPacket.SourcePort;
                this.SetEstablished(tcpPacket.AcknowledgmentNumber, tcpPacket.SequenceNumber);
            }
            else
            {
                this.clientHost = sourceHost;
                this.serverHost = destinationHost;
                this.clientTcpPort = tcpPacket.SourcePort;
                this.serverTcpPort = tcpPacket.DestinationPort;
                this.SetEstablished(tcpPacket.SequenceNumber, tcpPacket.AcknowledgmentNumber);
            }
            this.protocolFinder = protocolFinderFactory.CreateProtocolFinder(this.clientHost, this.serverHost, this.clientTcpPort, this.serverTcpPort, true, this.startFrameNumber, this.synPacketTimestamp);
        }

        internal NetworkTcpSession(TcpPacket tcpSynPacket, NetworkHost clientHost, NetworkHost serverHost, ISessionProtocolFinderFactory protocolFinderFactory)
        {
            this.requiredNextTcpDataStreamIsClientToServer = null;
            if (!tcpSynPacket.FlagBits.Synchronize)
            {
                throw new Exception("SYN flag not set on TCP packet");
            }
            this.synPacketTimestamp = tcpSynPacket.ParentFrame.Timestamp;
            this.clientHost = clientHost;
            this.serverHost = serverHost;
            this.clientTcpPort = tcpSynPacket.SourcePort;
            this.serverTcpPort = tcpSynPacket.DestinationPort;
            this.synPacketReceived = false;
            this.synAckPacketReceived = false;
            this.finPacketReceived = false;
            this.clientToServerFinPacketSequenceNumber = uint.MaxValue;
            this.serverToClientFinPacketSequenceNumber = uint.MaxValue;
            this.sessionEstablished = false;
            this.sessionClosed = false;
            this.startFrameNumber = tcpSynPacket.ParentFrame.FrameNumber;
            this.clientToServerTcpDataStream = null;
            this.serverToClientTcpDataStream = null;
            this.protocolFinder = protocolFinderFactory.CreateProtocolFinder(this.clientHost, this.serverHost, this.clientTcpPort, this.serverTcpPort, true, this.startFrameNumber, this.synPacketTimestamp);
        }

        internal void Close()
        {
            this.sessionClosed = true;
            if (this.protocolFinder.ConfirmedApplicationLayerProtocol == ApplicationLayerProtocol.Unknown)
            {
                this.protocolFinder.ConfirmedApplicationLayerProtocol = ApplicationLayerProtocol.Unknown;
            }
        }

        public int CompareTo(NetworkTcpSession session)
        {
            if (this.clientHost.CompareTo(session.clientHost) != 0)
            {
                return this.clientHost.CompareTo(session.clientHost);
            }
            if (this.serverHost.CompareTo(session.serverHost) != 0)
            {
                return this.serverHost.CompareTo(session.serverHost);
            }
            if (this.clientTcpPort != session.clientTcpPort)
            {
                return (this.clientTcpPort - session.clientTcpPort);
            }
            if (this.serverTcpPort != session.serverTcpPort)
            {
                return (this.serverTcpPort - session.serverTcpPort);
            }
            if (this.SessionStartTimestamp.CompareTo(session.SessionStartTimestamp) != 0)
            {
                return this.SessionStartTimestamp.CompareTo(session.SessionStartTimestamp);
            }
            return 0;
        }

        public int CompareTo(object obj)
        {
            NetworkTcpSession session = (NetworkTcpSession) obj;
            return this.CompareTo(session);
        }

        public override int GetHashCode()
        {
            return GetHashCode(this.clientHost, this.serverHost, this.clientTcpPort, this.serverTcpPort);
        }

        public static int GetHashCode(NetworkHost clientHost, NetworkHost serverHost, ushort clientTcpPort, ushort serverTcpPort)
        {
            int num = clientHost.IPAddress.GetHashCode() ^ clientTcpPort;
            int num2 = serverHost.IPAddress.GetHashCode() ^ serverTcpPort;
            return ((num ^ (num2 << 0x10)) ^ (num2 >> 0x10));
        }

        internal void RemoveData(TcpDataStream.VirtualTcpData virtualTcpData, NetworkHost sourceHost, ushort sourceTcpPort)
        {
            this.RemoveData(virtualTcpData.FirstPacketSequenceNumber, virtualTcpData.ByteCount, sourceHost, sourceTcpPort);
        }

        internal void RemoveData(uint firstSequenceNumber, int bytesToRemove, NetworkHost sourceHost, ushort sourceTcpPort)
        {
            if ((sourceHost == this.serverHost) && (sourceTcpPort == this.serverTcpPort))
            {
                this.ServerToClientTcpDataStream.RemoveData(firstSequenceNumber, bytesToRemove);
            }
            else
            {
                if ((sourceHost != this.clientHost) || (sourceTcpPort != this.clientTcpPort))
                {
                    throw new Exception("NetworkHost is not part of the NetworkTcpSession");
                }
                this.ClientToServerTcpDataStream.RemoveData(firstSequenceNumber, bytesToRemove);
            }
        }

        private void SetEstablished(uint clientInitialSequenceNumber, uint serverInitialSequenceNumber)
        {
            this.sessionEstablished = true;
            if (this.clientToServerTcpDataStream == null)
            {
                this.clientToServerTcpDataStream = new TcpDataStream(clientInitialSequenceNumber, this.clientTcpPort, this.serverTcpPort, this);
            }
            else
            {
                this.clientToServerTcpDataStream.InitialTcpSequenceNumber = clientInitialSequenceNumber;
            }
            if (this.serverToClientTcpDataStream == null)
            {
                this.serverToClientTcpDataStream = new TcpDataStream(serverInitialSequenceNumber, this.serverTcpPort, this.clientTcpPort, this);
            }
            else
            {
                this.serverToClientTcpDataStream.InitialTcpSequenceNumber = serverInitialSequenceNumber;
            }
            this.ServerHost.IncomingSessionList.Add(this);
            this.ClientHost.OutgoingSessionList.Add(this);
        }

        public override string ToString()
        {
            return string.Concat(new object[] { "Server: ", this.serverHost.ToString(), " TCP ", this.serverTcpPort, " (", this.serverToClientTcpDataStream.TotalByteCount, " data bytes sent), Client: ", this.clientHost.ToString(), " TCP ", this.ClientTcpPort, " (", this.clientToServerTcpDataStream.TotalByteCount, " data bytes sent), Session start: ", this.synPacketTimestamp, ", Session end: ", this.latestPacketTimestamp });
        }

        internal bool TryAddPacket(TcpPacket tcpPacket, NetworkHost sourceHost, NetworkHost destinationHost)
        {
            if (this.sessionClosed)
            {
                return false;
            }
            if ((sourceHost == this.clientHost) && (tcpPacket.SourcePort == this.clientTcpPort))
            {
                if (destinationHost != this.serverHost)
                {
                    return false;
                }
                if (tcpPacket.SourcePort != this.clientTcpPort)
                {
                    return false;
                }
                if (tcpPacket.DestinationPort != this.serverTcpPort)
                {
                    return false;
                }
            }
            else
            {
                if ((sourceHost != this.serverHost) || (tcpPacket.SourcePort != this.serverTcpPort))
                {
                    return false;
                }
                if (destinationHost != this.clientHost)
                {
                    return false;
                }
                if (tcpPacket.SourcePort != this.serverTcpPort)
                {
                    return false;
                }
                if (tcpPacket.DestinationPort != this.clientTcpPort)
                {
                    return false;
                }
            }
            this.latestPacketTimestamp = tcpPacket.ParentFrame.Timestamp;
            if (!this.synPacketReceived)
            {
                if (!tcpPacket.FlagBits.Synchronize || (sourceHost != this.clientHost))
                {
                    return false;
                }
                this.synPacketReceived = true;
            }
            else if (!this.synAckPacketReceived)
            {
                if ((!tcpPacket.FlagBits.Synchronize || !tcpPacket.FlagBits.Acknowledgement) || (sourceHost != this.serverHost))
                {
                    return false;
                }
                this.synAckPacketReceived = true;
            }
            else if (!this.sessionEstablished)
            {
                if (!tcpPacket.FlagBits.Acknowledgement || (sourceHost != this.clientHost))
                {
                    return false;
                }
                this.SetEstablished(tcpPacket.SequenceNumber, tcpPacket.AcknowledgmentNumber);
            }
            if (tcpPacket.PayloadDataLength > 0)
            {
                this.protocolFinder.AddPacket(tcpPacket, sourceHost, destinationHost);
                try
                {
                    byte[] tcpPacketPayloadData = tcpPacket.GetTcpPacketPayloadData();
                    NetworkServiceMetadata metadata = null;
                    if (!this.serverHost.NetworkServiceMetadataList.ContainsKey(this.serverTcpPort))
                    {
                        metadata = new NetworkServiceMetadata(this.ServerHost, this.serverTcpPort);
                        this.serverHost.NetworkServiceMetadataList.Add(this.serverTcpPort, metadata);
                    }
                    else
                    {
                        metadata = this.serverHost.NetworkServiceMetadataList[this.serverTcpPort];
                    }
                    if ((sourceHost == this.serverHost) && (tcpPacket.SourcePort == this.serverTcpPort))
                    {
                        metadata.OutgoingTraffic.AddTcpPayloadData(tcpPacketPayloadData);
                        if (this.serverToClientTcpDataStream == null)
                        {
                            this.serverToClientTcpDataStream = new TcpDataStream(tcpPacket.SequenceNumber, this.serverTcpPort, this.clientTcpPort, this);
                        }
                        if (!this.requiredNextTcpDataStreamIsClientToServer.HasValue && (this.serverToClientTcpDataStream.TotalByteCount == 0))
                        {
                            this.requiredNextTcpDataStreamIsClientToServer = false;
                        }
                        this.serverToClientTcpDataStream.AddTcpData(tcpPacket.SequenceNumber, tcpPacketPayloadData);
                    }
                    else
                    {
                        metadata.IncomingTraffic.AddTcpPayloadData(tcpPacketPayloadData);
                        if (this.clientToServerTcpDataStream == null)
                        {
                            this.clientToServerTcpDataStream = new TcpDataStream(tcpPacket.SequenceNumber, this.clientTcpPort, this.serverTcpPort, this);
                        }
                        if (!this.requiredNextTcpDataStreamIsClientToServer.HasValue && (this.clientToServerTcpDataStream.TotalByteCount == 0))
                        {
                            this.requiredNextTcpDataStreamIsClientToServer = true;
                        }
                        this.clientToServerTcpDataStream.AddTcpData(tcpPacket.SequenceNumber, tcpPacketPayloadData);
                    }
                }
                catch (Exception exception)
                {
                    if (!tcpPacket.ParentFrame.QuickParse)
                    {
                        tcpPacket.ParentFrame.Errors.Add(new Frame.Error(tcpPacket.ParentFrame, tcpPacket.PacketStartIndex, tcpPacket.PacketEndIndex, exception.Message));
                    }
                    return false;
                }
            }
            if (tcpPacket.FlagBits.Reset)
            {
                this.Close();
            }
            else if (tcpPacket.FlagBits.Fin)
            {
                if (!this.finPacketReceived)
                {
                    this.finPacketReceived = true;
                    if ((sourceHost == this.serverHost) && (tcpPacket.SourcePort == this.serverTcpPort))
                    {
                        this.serverToClientFinPacketSequenceNumber = tcpPacket.SequenceNumber;
                    }
                    else
                    {
                        this.clientToServerFinPacketSequenceNumber = tcpPacket.SequenceNumber;
                    }
                }
                else if (tcpPacket.FlagBits.Acknowledgement)
                {
                    this.Close();
                }
            }
            return true;
        }

        public NetworkHost ClientHost
        {
            get
            {
                return this.clientHost;
            }
        }

        public ushort ClientTcpPort
        {
            get
            {
                return this.clientTcpPort;
            }
        }

        public TcpDataStream ClientToServerTcpDataStream
        {
            get
            {
                return this.clientToServerTcpDataStream;
            }
        }

        public bool FinPacketReceived
        {
            get
            {
                if (!this.finPacketReceived)
                {
                    return false;
                }
                return ((((this.clientToServerTcpDataStream == null) || (this.serverToClientTcpDataStream == null)) || (this.clientToServerTcpDataStream == null)) || ((this.clientToServerFinPacketSequenceNumber <= this.clientToServerTcpDataStream.ExpectedTcpSequenceNumber) && (this.serverToClientFinPacketSequenceNumber < this.serverToClientTcpDataStream.ExpectedTcpSequenceNumber)));
            }
        }

        public ISessionProtocolFinder ProtocolFinder
        {
            get
            {
                return this.protocolFinder;
            }
        }

        public TcpDataStream RequiredNextTcpDataStream
        {
            get
            {
                if (this.requiredNextTcpDataStreamIsClientToServer == true)
                {
                    return this.clientToServerTcpDataStream;
                }
                if (this.requiredNextTcpDataStreamIsClientToServer == false)
                {
                    return this.serverToClientTcpDataStream;
                }
                return null;
            }
            set
            {
                if (value == null)
                {
                    this.requiredNextTcpDataStreamIsClientToServer = null;
                }
            }
        }

        public NetworkHost ServerHost
        {
            get
            {
                return this.serverHost;
            }
        }

        public ushort ServerTcpPort
        {
            get
            {
                return this.serverTcpPort;
            }
        }

        public TcpDataStream ServerToClientTcpDataStream
        {
            get
            {
                return this.serverToClientTcpDataStream;
            }
        }

        public bool SessionClosed
        {
            get
            {
                return this.sessionClosed;
            }
        }

        public DateTime SessionEndTimestamp
        {
            get
            {
                return this.latestPacketTimestamp;
            }
        }

        public bool SessionEstablished
        {
            get
            {
                return this.sessionEstablished;
            }
        }

        public DateTime SessionStartTimestamp
        {
            get
            {
                return this.synPacketTimestamp;
            }
        }

        public bool SynAckPacketReceived
        {
            get
            {
                return this.synAckPacketReceived;
            }
        }

        public bool SynPacketReceived
        {
            get
            {
                return this.synPacketReceived;
            }
        }

        public class TcpDataStream
        {
            private SortedList<uint, byte[]> dataList;
            private int dataListMaxSize;
            private ushort destinationPort;
            private uint expectedTcpSequenceNumber;
            private uint initialTcpSequenceNumber;
            private NetworkTcpSession session;
            private ushort sourcePort;
            private int totalByteCount;
            private VirtualTcpData virtualTcpData;

            internal TcpDataStream(uint initialTcpSequenceNumber, ushort sourcePort, ushort destinationPort, NetworkTcpSession session)
            {
                this.initialTcpSequenceNumber = initialTcpSequenceNumber;
                this.expectedTcpSequenceNumber = initialTcpSequenceNumber;
                this.sourcePort = sourcePort;
                this.destinationPort = destinationPort;
                this.dataList = new SortedList<uint, byte[]>();
                this.dataListMaxSize = 0x40;
                this.totalByteCount = 0;
                this.virtualTcpData = null;
                this.session = session;
            }

            internal void AddTcpData(uint tcpSequenceNumber, byte[] tcpSegmentData)
            {
                if (tcpSegmentData.Length > 0)
                {
                    if (((this.expectedTcpSequenceNumber - tcpSequenceNumber) > 0) && ((this.expectedTcpSequenceNumber - tcpSequenceNumber) < tcpSegmentData.Length))
                    {
                        uint num = this.expectedTcpSequenceNumber - tcpSequenceNumber;
                        byte[] destinationArray = new byte[tcpSegmentData.Length - num];
                        Array.Copy(tcpSegmentData, (long) num, destinationArray, 0L, (long) destinationArray.Length);
                        tcpSegmentData = destinationArray;
                        tcpSequenceNumber += num;
                    }
                    if ((((this.expectedTcpSequenceNumber - tcpSequenceNumber) <= 0) && ((tcpSequenceNumber - this.expectedTcpSequenceNumber) < 0xf4240)) && !this.dataList.ContainsKey(tcpSequenceNumber))
                    {
                        IList<uint> keys = this.dataList.Keys;
                        for (int i = keys.Count - 1; i >= 0; i--)
                        {
                            if (keys[i] < tcpSequenceNumber)
                            {
                                break;
                            }
                            if ((keys[i]) < (tcpSequenceNumber + tcpSegmentData.Length))
                            {
                                uint num3 = keys[i] - tcpSequenceNumber;
                                byte[] buffer2 = new byte[num3];
                                Array.Copy(tcpSegmentData, 0L, buffer2, 0L, (long) num3);
                                tcpSegmentData = buffer2;
                            }
                        }
                        this.dataList.Add(tcpSequenceNumber, tcpSegmentData);
                        this.totalByteCount += tcpSegmentData.Length;
                        if (this.expectedTcpSequenceNumber == tcpSequenceNumber)
                        {
                            this.expectedTcpSequenceNumber += (uint) tcpSegmentData.Length;
                            while (this.dataList.ContainsKey(this.expectedTcpSequenceNumber))
                            {
                                this.expectedTcpSequenceNumber += (uint) this.dataList[this.expectedTcpSequenceNumber].Length;
                            }
                        }
                        while (this.dataList.Count > this.dataListMaxSize)
                        {
                            this.dataList.RemoveAt(0);
                            this.virtualTcpData = null;
                        }
                    }
                }
            }

            internal int CountBytesToRead()
            {
                if (this.dataList.Count < 1)
                {
                    return 0;
                }
                return (int) (this.expectedTcpSequenceNumber - this.dataList.Keys[0]);
            }

            internal int CountPacketsToRead()
            {
                if (this.dataList.Count == 0)
                {
                    return 0;
                }
                int num = 0;
                uint num2 = this.dataList.Keys[0];
                foreach (KeyValuePair<uint, byte[]> pair in this.dataList)
                {
                    if (pair.Key != num2)
                    {
                        return num;
                    }
                    num++;
                    num2 += (uint) pair.Value.Length;
                }
                return num;
            }

            internal VirtualTcpData GetNextVirtualTcpData()
            {
                if (this.virtualTcpData == null)
                {
                    if (((this.dataList.Count > 0) && (this.CountBytesToRead() > 0)) && (this.CountPacketsToRead() > 0))
                    {
                        this.virtualTcpData = new VirtualTcpData(this, this.sourcePort, this.destinationPort);
                        return this.virtualTcpData;
                    }
                    return null;
                }
                if (this.virtualTcpData.TryAppendNextPacket())
                {
                    return this.virtualTcpData;
                }
                return null;
            }

            internal IEnumerable<byte[]> GetSegments()
            {
                byte[] iteratorVariable1;
                if (this.dataList.Count < 1)
                {
                    goto Label_00E8;
                }
                uint key = this.dataList.Keys[0];
            Label_PostSwitchInIterator:;
                if ((key < this.expectedTcpSequenceNumber) && this.dataList.TryGetValue(key, out iteratorVariable1))
                {
                    this.dataList.Remove(this.dataList.Keys[0]);
                    yield return iteratorVariable1;
                    key = this.dataList.Keys[0];
                    goto Label_PostSwitchInIterator;
                }
            Label_00E8:;
            }

            internal bool HasMissingSegments()
            {
                return (this.totalByteCount < (this.expectedTcpSequenceNumber - this.initialTcpSequenceNumber));
            }

            internal void RemoveData(VirtualTcpData data)
            {
                this.RemoveData(data.FirstPacketSequenceNumber, data.ByteCount);
            }

            internal void RemoveData(uint firstSequenceNumber, int bytesToRemove)
            {
                if (this.dataList.Keys[0] != firstSequenceNumber)
                {
                    throw new Exception(string.Concat(new object[] { "The data (first data sequence number: ", this.dataList.Keys[0], ") is not equal to ", firstSequenceNumber }));
                }
                while ((this.dataList.Count > 0) && (((this.dataList.Keys[0]) + this.dataList.Values[0].Length) <= (firstSequenceNumber + bytesToRemove)))
                {
                    this.dataList.RemoveAt(0);
                }
                if ((this.dataList.Count > 0) && ((this.dataList.Keys[0]) < (firstSequenceNumber + bytesToRemove)))
                {
                    uint key = firstSequenceNumber + ((uint) bytesToRemove);
                    byte[] sourceArray = this.dataList.Values[0];
                    byte[] destinationArray = new byte[(( this.dataList.Keys[0]) + sourceArray.Length) - key];
                    Array.Copy(sourceArray, sourceArray.Length - destinationArray.Length, destinationArray, 0, destinationArray.Length);
                    this.dataList.RemoveAt(0);
                    this.dataList.Add(key, destinationArray);
                }
                this.virtualTcpData = null;
            }

            public int DataSegmentBufferCount
            {
                get
                {
                    return this.dataList.Count;
                }
            }

            public int DataSegmentBufferMaxSize
            {
                get
                {
                    return this.dataListMaxSize;
                }
            }

            internal uint ExpectedTcpSequenceNumber
            {
                get
                {
                    return this.expectedTcpSequenceNumber;
                }
            }

            internal uint InitialTcpSequenceNumber
            {
                set
                {
                    this.initialTcpSequenceNumber = value;
                }
            }

            public int TotalByteCount
            {
                get
                {
                    return this.totalByteCount;
                }
            }


            internal class VirtualTcpData
            {
                private ushort destinationPort;
                private int nPackets;
                private ushort sourcePort;
                private NetworkTcpSession.TcpDataStream tcpDataStream;

                internal VirtualTcpData(NetworkTcpSession.TcpDataStream tcpDataStream, ushort sourcePort, ushort destinationPort)
                {
                    this.tcpDataStream = tcpDataStream;
                    this.sourcePort = sourcePort;
                    this.destinationPort = destinationPort;
                    this.nPackets = 1;
                }

                internal byte[] GetBytes(bool prependTcpHeader)
                {
                    List<byte> list;
                    if (prependTcpHeader)
                    {
                        list = new List<byte>(this.GetTcpHeader());
                    }
                    else
                    {
                        list = new List<byte>();
                    }
                    int count = list.Count;
                    int num2 = 0;
                    for (uint i = this.tcpDataStream.dataList.Keys[0]; num2 < this.nPackets; i = (uint) ((this.tcpDataStream.dataList.Keys[0] + list.Count) - count))
                    {
                        list.AddRange(this.tcpDataStream.dataList[i]);
                        num2++;
                    }
                    return list.ToArray();
                }

                private byte[] GetTcpHeader()
                {
                    byte[] array = new byte[20];
                    ByteConverter.ToByteArray(this.sourcePort, array, 0);
                    ByteConverter.ToByteArray(this.destinationPort, array, 2);
                    ByteConverter.ToByteArray(this.tcpDataStream.dataList.Keys[0], array, 4);
                    array[12] = 80;
                    array[13] = 0x18;
                    array[14] = 0xff;
                    array[15] = 0xff;
                    return array;
                }

                internal bool TryAppendNextPacket()
                {
                    int num = 6;
                    if (this.tcpDataStream.session.protocolFinder.ConfirmedApplicationLayerProtocol == ApplicationLayerProtocol.NetBiosSessionService)
                    {
                        num = 50;
                    }
                    else if (this.tcpDataStream.session.protocolFinder.ConfirmedApplicationLayerProtocol == ApplicationLayerProtocol.Http)
                    {
                        num = 0x20;
                    }
                    if (((this.tcpDataStream.CountBytesToRead() > this.ByteCount) && (this.tcpDataStream.CountPacketsToRead() > this.nPackets)) && (this.nPackets < num))
                    {
                        this.nPackets++;
                        return true;
                    }
                    return false;
                }

                internal int ByteCount
                {
                    get
                    {
                        return (int) ((this.tcpDataStream.dataList.Keys[this.nPackets - 1] + this.tcpDataStream.dataList.Values[this.nPackets - 1].Length) - this.tcpDataStream.dataList.Keys[0]);
                    }
                }

                internal uint FirstPacketSequenceNumber
                {
                    get
                    {
                        return this.tcpDataStream.dataList.Keys[0];
                    }
                }

                internal int PacketCount
                {
                    get
                    {
                        return this.nPackets;
                    }
                }
            }
        }
    }
}

