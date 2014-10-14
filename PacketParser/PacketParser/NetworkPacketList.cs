namespace PacketParser
{
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class NetworkPacketList : List<NetworkPacket>
    {
        private int cleartextBytes;
        private int payloadBytes;
        private int totalBytes;

        public new void Add(NetworkPacket packet)
        {
            base.Add(packet);
            this.totalBytes += packet.PacketBytes;
            this.payloadBytes += packet.PayloadBytes;
            this.cleartextBytes += packet.CleartextBytes;
        }

        public new void AddRange(IEnumerable<NetworkPacket> collection)
        {
            foreach (NetworkPacket packet in collection)
            {
                this.Add(packet);
            }
        }

        public NetworkPacketList GetSubset(IPAddress sourceIp, IPAddress destinationIp)
        {
            NetworkPacketList list = new NetworkPacketList();
            foreach (NetworkPacket packet in this)
            {
                if (packet.SourceHost.IPAddress.Equals(sourceIp) && packet.DestinationHost.IPAddress.Equals(destinationIp))
                {
                    list.Add(packet);
                }
            }
            return list;
        }

        public NetworkPacketList GetSubset(IPAddress sourceIp, ushort? sourceTcpPort, IPAddress destinationIp, ushort? destinationTcpPort)
        {
            NetworkPacketList list = new NetworkPacketList();
            foreach (NetworkPacket packet in this)
            {
                if (((packet.SourceHost.IPAddress.Equals(sourceIp) && packet.DestinationHost.IPAddress.Equals(destinationIp)) && (packet.SourceTcpPort == sourceTcpPort)) && (packet.DestinationTcpPort == destinationTcpPort))
                {
                    list.Add(packet);
                }
            }
            return list;
        }

        public ICollection<KeyValuePair<ushort[], NetworkPacketList>> GetSubsetPerTcpPortPair()
        {
            Dictionary<uint, NetworkPacketList> dictionary = new Dictionary<uint, NetworkPacketList>();
            foreach (NetworkPacket packet in this)
            {
                ushort? sourceTcpPort = packet.SourceTcpPort;
                int? nullable3 = sourceTcpPort.HasValue ? new int?(sourceTcpPort.GetValueOrDefault()) : null;
                if (nullable3.HasValue)
                {
                    ushort? destinationTcpPort = packet.DestinationTcpPort;
                    int? nullable6 = destinationTcpPort.HasValue ? new int?(destinationTcpPort.GetValueOrDefault()) : null;
                    if (nullable6.HasValue)
                    {
                        uint key = ByteConverter.ToUInt32(packet.SourceTcpPort.Value, packet.DestinationTcpPort.Value);
                        if (dictionary.ContainsKey(key))
                        {
                            dictionary[key].Add(packet);
                        }
                        else
                        {
                            dictionary.Add(key, new NetworkPacketList());
                            dictionary[key].Add(packet);
                        }
                    }
                }
            }
            List<KeyValuePair<ushort[], NetworkPacketList>> list = new List<KeyValuePair<ushort[], NetworkPacketList>>();
            foreach (uint num2 in dictionary.Keys)
            {
                ushort[] numArray = new ushort[] { (ushort) (num2 >> 0x10), (ushort) (num2 & 0xffff) };
                list.Add(new KeyValuePair<ushort[], NetworkPacketList>(numArray, dictionary[num2]));
            }
            return list;
        }

        public ICollection<KeyValuePair<ushort[], NetworkPacketList>> GetSubsetPerUdpPortPair()
        {
            Dictionary<uint, NetworkPacketList> dictionary = new Dictionary<uint, NetworkPacketList>();
            foreach (NetworkPacket packet in this)
            {
                ushort? sourceUdpPort = packet.SourceUdpPort;
                int? nullable3 = sourceUdpPort.HasValue ? new int?(sourceUdpPort.GetValueOrDefault()) : null;
                if (nullable3.HasValue)
                {
                    ushort? destinationUdpPort = packet.DestinationUdpPort;
                    int? nullable6 = destinationUdpPort.HasValue ? new int?(destinationUdpPort.GetValueOrDefault()) : null;
                    if (nullable6.HasValue)
                    {
                        uint key = ByteConverter.ToUInt32(packet.SourceUdpPort.Value, packet.DestinationUdpPort.Value);
                        if (dictionary.ContainsKey(key))
                        {
                            dictionary[key].Add(packet);
                        }
                        else
                        {
                            dictionary.Add(key, new NetworkPacketList());
                            dictionary[key].Add(packet);
                        }
                    }
                }
            }
            List<KeyValuePair<ushort[], NetworkPacketList>> list = new List<KeyValuePair<ushort[], NetworkPacketList>>();
            foreach (uint num2 in dictionary.Keys)
            {
                ushort[] numArray = new ushort[] { (ushort) (num2 >> 0x10), (ushort) (num2 & 0xffff) };
                list.Add(new KeyValuePair<ushort[], NetworkPacketList>(numArray, dictionary[num2]));
            }
            return list;
        }

        public override string ToString()
        {
            return string.Concat(new object[] { base.Count, " packets (", this.TotalBytes.ToString("n0"), " Bytes), ", this.CleartextProcentage.ToString("p"), " cleartext (", this.CleartextBytes.ToString("n0"), " of ", this.PayloadBytes.ToString("n0"), " Bytes)" });
        }

        public int CleartextBytes
        {
            get
            {
                return this.cleartextBytes;
            }
        }

        public double CleartextProcentage
        {
            get
            {
                if (this.cleartextBytes > 0)
                {
                    return ((1.0 * this.cleartextBytes) / ((double) this.payloadBytes));
                }
                return 0.0;
            }
        }

        public int PayloadBytes
        {
            get
            {
                return this.payloadBytes;
            }
        }

        public int TotalBytes
        {
            get
            {
                return this.totalBytes;
            }
        }
    }
}

