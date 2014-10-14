namespace PacketParser
{
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Net;

    public class NetworkHostList
    {
        private SortedDictionary<uint, NetworkHost> networkHostDictionary = new SortedDictionary<uint, NetworkHost>();

        internal NetworkHostList()
        {
        }

        internal void Add(NetworkHost host)
        {
            this.networkHostDictionary.Add(ByteConverter.ToUInt32(host.IPAddress), host);
        }

        internal void Clear()
        {
            this.networkHostDictionary.Clear();
        }

        internal bool ContainsIP(IPAddress ip)
        {
            uint key = ByteConverter.ToUInt32(ip);
            return this.networkHostDictionary.ContainsKey(key);
        }

        internal NetworkHost GetNetworkHost(IPAddress ip)
        {
            uint key = ByteConverter.ToUInt32(ip);
            if (this.networkHostDictionary.ContainsKey(key))
            {
                return this.networkHostDictionary[key];
            }
            return null;
        }

        public int Count
        {
            get
            {
                return this.networkHostDictionary.Count;
            }
        }

        public ICollection<NetworkHost> Hosts
        {
            get
            {
                return this.networkHostDictionary.Values;
            }
        }
    }
}

