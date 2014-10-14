namespace PacketParser
{
    using System;

    public class NetworkServiceMetadata
    {
        private PacketParser.ApplicationLayerProtocol applicationLayerProtocol;
        private TrafficMetadata incomingTraffic;
        private TrafficMetadata outgoingTraffic;
        private NetworkHost serverHost;
        private ushort tcpPort;

        public NetworkServiceMetadata(NetworkHost serverHost, ushort tcpPort)
        {
            this.serverHost = serverHost;
            this.tcpPort = tcpPort;
            this.incomingTraffic = new TrafficMetadata(true);
            this.outgoingTraffic = new TrafficMetadata(false);
        }

        public override string ToString()
        {
            return ("TCP " + this.tcpPort);
        }

        public PacketParser.ApplicationLayerProtocol ApplicationLayerProtocol
        {
            get
            {
                return this.applicationLayerProtocol;
            }
            set
            {
                if (value != PacketParser.ApplicationLayerProtocol.Unknown)
                {
                    this.applicationLayerProtocol = value;
                }
            }
        }

        public TrafficMetadata IncomingTraffic
        {
            get
            {
                return this.incomingTraffic;
            }
        }

        public TrafficMetadata OutgoingTraffic
        {
            get
            {
                return this.outgoingTraffic;
            }
        }

        public ushort TcpPort
        {
            get
            {
                return this.tcpPort;
            }
        }

        public class TrafficMetadata
        {
            private int[] byteCount;
            private int[] dataLengthCount;
            private int[] first256TrueBitsCount;
            private bool incomingTraffic;
            private int packetsCount;

            internal TrafficMetadata(bool incomingTraffic)
            {
                this.incomingTraffic = incomingTraffic;
                this.packetsCount = 0;
                this.byteCount = new int[0x100];
                this.first256TrueBitsCount = new int[0x100];
                this.dataLengthCount = new int[0x21];
            }

            internal void AddTcpPayloadData(byte[] tcpPayloadData)
            {
                if (tcpPayloadData.Length < this.dataLengthCount.Length)
                {
                    this.dataLengthCount[tcpPayloadData.Length]++;
                }
                this.packetsCount++;
                for (int i = 0; i < tcpPayloadData.Length; i++)
                {
                    this.byteCount[tcpPayloadData[i]]++;
                    if (i < 0x20)
                    {
                        for (int j = 0; j < 8; j++)
                        {
                            if (((tcpPayloadData[i] >> (7 - j)) & 1) == 1)
                            {
                                this.first256TrueBitsCount[(i * 8) + j]++;
                            }
                        }
                    }
                }
            }

            public double CalculateEntropy()
            {
                double num = 0.0;
                double[] byteFrequencies = this.GetByteFrequencies();
                for (int i = 0; i < byteFrequencies.Length; i++)
                {
                    if (byteFrequencies[i] > 0.0)
                    {
                        num -= byteFrequencies[i] * Math.Log(byteFrequencies[i], 2.0);
                    }
                }
                return ((num * 100.0) / 8.0);
            }

            private double[] GetByteFrequencies()
            {
                int num = 0;
                double[] numArray = new double[this.byteCount.Length];
                for (int i = 0; i < this.byteCount.Length; i++)
                {
                    num += this.byteCount[i];
                }
                for (int j = 0; j < this.byteCount.Length; j++)
                {
                    numArray[j] = (1.0 * this.byteCount[j]) / ((double) num);
                }
                return numArray;
            }

            public string GetTypicalData()
            {
                string str = "";
                int packetsCount = this.packetsCount;
                for (int i = 0; i < 0x20; i++)
                {
                    packetsCount -= this.dataLengthCount[i];
                    double[] byteFrequencies = this.GetByteFrequencies();
                    double num3 = 0.0;
                    int num4 = 0;
                    for (int j = 0; j < byteFrequencies.Length; j++)
                    {
                        for (int k = 0; k < 8; k++)
                        {
                            if (((j >> (7 - k)) & 1) == 1)
                            {
                                byteFrequencies[j] *= (1.0 * this.first256TrueBitsCount[(i * 8) + k]) / ((double) packetsCount);
                            }
                            else
                            {
                                byteFrequencies[j] *= 1.0 - ((1.0 * this.first256TrueBitsCount[(i * 8) + k]) / ((double) packetsCount));
                            }
                        }
                        if (byteFrequencies[j] > num3)
                        {
                            num3 = byteFrequencies[j];
                            num4 = j;
                        }
                    }
                    str = str + ((char) ((byte) num4));
                }
                return str;
            }
        }
    }
}

