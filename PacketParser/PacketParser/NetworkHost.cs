namespace PacketParser
{
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Text;

    public class NetworkHost : IComparable
    {
        private List<string> acceptedSmbDialectsList;
        private List<string> dhcpVendorCodeList;
        private List<string> domainNameList;
        private SortedList<string, string> extraDetailsList;
        private List<string> ftpServerBannerList;
        private List<string> hostNameList;
        private List<string> httpServerBannerList;
        private List<string> httpUserAgentBannerList;
        private List<NetworkTcpSession> incomingSessionList;
        private System.Net.IPAddress ipAddress;
        private PhysicalAddress macAddress;
        private SortedList<ushort, NetworkServiceMetadata> networkServiceMetadataList;
        private List<ushort> openTcpPortList;
        private SortedList<string, SortedList<string, double>> operatingSystemCounterList;
        private List<NetworkTcpSession> outgoingSessionList;
        private string preferredSmbDialect;
        private List<string> queriedDnsNameList;
        private List<System.Net.IPAddress> queriedIpList;
        private List<string> queriedNetBiosNameList;
        private NetworkPacketList receivedPackets;
        private PopularityList<string, PhysicalAddress> recentMacAdresses;
        private NetworkPacketList sentPackets;
        private SortedList<byte, int> ttlCount;
        private SortedList<byte, int> ttlDistanceCount;
        private SortedList<string, string> universalPlugAndPlayFieldList;

        internal NetworkHost(System.Net.IPAddress ipAddress)
        {
            this.ipAddress = ipAddress;
            this.macAddress = null;
            this.recentMacAdresses = new PopularityList<string, PhysicalAddress>(0xff);
            this.ttlCount = new SortedList<byte, int>();
            this.ttlDistanceCount = new SortedList<byte, int>();
            this.operatingSystemCounterList = new SortedList<string, SortedList<string, double>>();
            this.hostNameList = new List<string>();
            this.domainNameList = new List<string>();
            this.openTcpPortList = new List<ushort>();
            this.networkServiceMetadataList = new SortedList<ushort, NetworkServiceMetadata>();
            this.sentPackets = new NetworkPacketList();
            this.receivedPackets = new NetworkPacketList();
            this.incomingSessionList = new List<NetworkTcpSession>();
            this.outgoingSessionList = new List<NetworkTcpSession>();
            this.queriedIpList = new List<System.Net.IPAddress>();
            this.queriedNetBiosNameList = new List<string>();
            this.queriedDnsNameList = new List<string>();
            this.httpUserAgentBannerList = new List<string>();
            this.httpServerBannerList = new List<string>();
            this.ftpServerBannerList = new List<string>();
            this.dhcpVendorCodeList = new List<string>();
            this.extraDetailsList = new SortedList<string, string>();
            this.universalPlugAndPlayFieldList = null;
            this.acceptedSmbDialectsList = null;
            this.preferredSmbDialect = null;
        }

        internal void AddDhcpVendorCode(string vendorCode)
        {
            if (!this.dhcpVendorCodeList.Contains(vendorCode))
            {
                this.dhcpVendorCodeList.Add(vendorCode);
            }
        }

        internal void AddDomainName(string domainName)
        {
            if (!this.domainNameList.Contains(domainName))
            {
                this.domainNameList.Add(domainName);
            }
        }

        internal void AddFtpServerBanner(string banner, ushort serverTcpPort)
        {
            if (!this.ftpServerBannerList.Contains(string.Concat(new object[] { "TCP ", serverTcpPort, " : ", banner })))
            {
                this.ftpServerBannerList.Add(string.Concat(new object[] { "TCP ", serverTcpPort, " : ", banner }));
            }
        }

        internal void AddHostName(string hostname)
        {
            if (!this.hostNameList.Contains(hostname))
            {
                this.hostNameList.Add(hostname);
            }
        }

        internal void AddHttpServerBanner(string banner, ushort serverTcpPort)
        {
            if (!this.httpServerBannerList.Contains(string.Concat(new object[] { "TCP ", serverTcpPort, " : ", banner })))
            {
                this.httpServerBannerList.Add(string.Concat(new object[] { "TCP ", serverTcpPort, " : ", banner }));
            }
        }

        internal void AddHttpUserAgentBanner(string banner)
        {
            if (!this.httpUserAgentBannerList.Contains(banner))
            {
                this.httpUserAgentBannerList.Add(banner);
            }
        }

        internal void AddNumberedExtraDetail(string name, string value)
        {
            for (int i = 1; i < 100; i++)
            {
                if (this.ExtraDetailsList.ContainsKey(name + " " + i))
                {
                    if (this.ExtraDetailsList[name + " " + i].Equals(value, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return;
                    }
                }
                else
                {
                    this.ExtraDetailsList.Add(name + " " + i, value);
                    return;
                }
            }
        }

        internal void AddOpenTcpPort(ushort port)
        {
            this.openTcpPortList.Add(port);
        }

        internal void AddProbableOs(string fingerprinterName, string operatingSystem, double probability)
        {
            if (!this.operatingSystemCounterList.ContainsKey(fingerprinterName))
            {
                this.operatingSystemCounterList.Add(fingerprinterName, new SortedList<string, double>());
            }
            SortedList<string, double> list = this.operatingSystemCounterList[fingerprinterName];
            if (list.ContainsKey(operatingSystem))
            {
                SortedList<string, double> list2;
                string str;
                (list2 = list)[str = operatingSystem] = list2[str] + probability;
            }
            else
            {
                list.Add(operatingSystem, probability);
            }
        }

        internal void AddProbableTtlDistance(byte ttlDistance)
        {
            if (this.ttlDistanceCount.ContainsKey(ttlDistance))
            {
                SortedList<byte, int> list;
                byte num;
                (list = this.ttlDistanceCount)[num = ttlDistance] = list[num] + 1;
            }
            else
            {
                this.ttlDistanceCount.Add(ttlDistance, 1);
            }
        }

        internal void AddQueriedDnsName(string dnsName)
        {
            if (!this.queriedDnsNameList.Contains(dnsName))
            {
                this.queriedDnsNameList.Add(dnsName);
            }
        }

        internal void AddQueriedIP(System.Net.IPAddress ip)
        {
            if (!this.queriedIpList.Contains(ip))
            {
                this.queriedIpList.Add(ip);
            }
        }

        internal void AddQueriedNetBiosName(string netBiosName)
        {
            if (!this.queriedNetBiosNameList.Contains(netBiosName))
            {
                this.queriedNetBiosNameList.Add(netBiosName);
            }
        }

        internal void AddTtl(byte ttl)
        {
            if (this.ttlCount.ContainsKey(ttl))
            {
                SortedList<byte, int> list;
                byte num;
                (list = this.ttlCount)[num = ttl] = list[num] + 1;
            }
            else
            {
                this.ttlCount.Add(ttl, 1);
            }
        }

        public int CompareTo(NetworkHost host)
        {
            if (!this.IPAddress.Equals(host.IPAddress))
            {
                byte[] addressBytes = this.IPAddress.GetAddressBytes();
                byte[] buffer2 = host.IPAddress.GetAddressBytes();
                if (addressBytes.Length != buffer2.Length)
                {
                    return (addressBytes.Length - buffer2.Length);
                }
                for (int i = 0; (i < addressBytes.Length) && (i < buffer2.Length); i++)
                {
                    if (addressBytes[i] != buffer2[i])
                    {
                        return (addressBytes[i] - buffer2[i]);
                    }
                }
            }
            return 0;
        }

        public int CompareTo(object obj)
        {
            NetworkHost host = (NetworkHost) obj;
            return this.CompareTo(host);
        }

        public override int GetHashCode()
        {
            return this.ipAddress.GetHashCode();
        }

        public string GetOsDetails(string osCounterName)
        {
            SortedList<string, double> list = this.operatingSystemCounterList[osCounterName];
            if (list.Count == 0)
            {
                return "";
            }
            StringBuilder builder = new StringBuilder("");
            double num = 0.0;
            foreach (string str in list.Keys)
            {
                num += list[str];
            }
            if (num == 0.0)
            {
                return "";
            }
            string[] array = new string[list.Count];
            double[] numArray = new double[list.Count];
            list.Keys.CopyTo(array, 0);
            list.Values.CopyTo(numArray, 0);
            Array.Sort<double, string>(numArray, array);
            for (int i = array.Length - 1; i >= 0; i--)
            {
                builder.Append(array[i] + " (" + ((numArray[i] / num)).ToString("p") + ") ");
            }
            return builder.ToString();
        }

        public SortedList<NetworkHost, NetworkPacketList> GetReceivedPacketListsPerSourceHost()
        {
            SortedList<NetworkHost, NetworkPacketList> list = new SortedList<NetworkHost, NetworkPacketList>();
            foreach (NetworkPacket packet in this.ReceivedPackets)
            {
                if (!list.ContainsKey(packet.SourceHost))
                {
                    list.Add(packet.SourceHost, new NetworkPacketList());
                }
                list[packet.SourceHost].Add(packet);
            }
            return list;
        }

        public SortedList<NetworkHost, NetworkPacketList> GetSentPacketListsPerDestinationHost()
        {
            SortedList<NetworkHost, NetworkPacketList> list = new SortedList<NetworkHost, NetworkPacketList>();
            foreach (NetworkPacket packet in this.sentPackets)
            {
                if (!list.ContainsKey(packet.DestinationHost))
                {
                    list.Add(packet.DestinationHost, new NetworkPacketList());
                }
                list[packet.DestinationHost].Add(packet);
            }
            return list;
        }

        internal bool IsRecentMacAddress(PhysicalAddress macAddress)
        {
            return this.recentMacAdresses.ContainsKey(macAddress.ToString());
        }

        internal bool TcpPortIsOpen(ushort port)
        {
            return this.openTcpPortList.Contains(port);
        }

        public override string ToString()
        {
            string str = this.ipAddress.ToString();
            foreach (string str2 in this.hostNameList)
            {
                str = str + " [" + str2 + "]";
            }
            if (this.operatingSystemCounterList.Count > 0)
            {
                object obj2 = str;
                str = string.Concat(new object[] { obj2, " (", this.OS, ")" });
            }
            return str;
        }

        public List<string> AcceptedSmbDialectsList
        {
            get
            {
                return this.acceptedSmbDialectsList;
            }
            set
            {
                this.acceptedSmbDialectsList = value;
            }
        }

        public SortedList<string, string> ExtraDetailsList
        {
            get
            {
                return this.extraDetailsList;
            }
        }

        public NameValueCollection HostDetailCollection
        {
            get
            {
                NameValueCollection values = new NameValueCollection();
                if (this.queriedIpList.Count > 0)
                {
                    new StringBuilder();
                    foreach (System.Net.IPAddress address in this.queriedIpList)
                    {
                        values.Add("Queried IP Addresses", address.ToString());
                    }
                }
                if (this.queriedNetBiosNameList.Count > 0)
                {
                    new StringBuilder();
                    foreach (string str in this.queriedNetBiosNameList)
                    {
                        values.Add("Queried NetBIOS names", str);
                    }
                }
                if (this.queriedDnsNameList.Count > 0)
                {
                    new StringBuilder();
                    foreach (string str2 in this.queriedDnsNameList)
                    {
                        values.Add("Queried DNS names", str2);
                    }
                }
                for (int i = 0; i < this.domainNameList.Count; i++)
                {
                    values.Add("Domain Name " + (i + 1), this.domainNameList[i]);
                }
                for (int j = 0; j < this.httpUserAgentBannerList.Count; j++)
                {
                    values.Add("Web Browser User-Agent " + (j + 1), this.httpUserAgentBannerList[j]);
                }
                for (int k = 0; k < this.httpServerBannerList.Count; k++)
                {
                    values.Add("Web Server Banner " + (k + 1), this.httpServerBannerList[k]);
                }
                for (int m = 0; m < this.ftpServerBannerList.Count; m++)
                {
                    values.Add("FTP Server Banner " + (m + 1), this.ftpServerBannerList[m]);
                }
                for (int n = 0; n < this.dhcpVendorCodeList.Count; n++)
                {
                    values.Add("DHCP Vendor Code " + (n + 1), this.dhcpVendorCodeList[n]);
                }
                if (this.universalPlugAndPlayFieldList != null)
                {
                    foreach (string str3 in this.universalPlugAndPlayFieldList.Values)
                    {
                        if (str3.Contains(":"))
                        {
                            values.Add("UPnP field : " + str3.Substring(0, str3.LastIndexOf(':')), str3.Substring(str3.LastIndexOf(':') + 1));
                        }
                        else
                        {
                            values.Add("UPnP field", str3);
                        }
                    }
                }
                if (this.acceptedSmbDialectsList != null)
                {
                    foreach (string str4 in this.acceptedSmbDialectsList)
                    {
                        values.Add("Accepted SMB dialects", str4);
                    }
                }
                if (this.preferredSmbDialect != null)
                {
                    values.Add("Preferred SMB dialect", this.preferredSmbDialect);
                }
                foreach (KeyValuePair<string, string> pair in this.extraDetailsList)
                {
                    values.Add(pair.Key, pair.Value);
                }
                return values;
            }
        }

        public string HostName
        {
            get
            {
                StringBuilder builder = new StringBuilder("");
                foreach (string str in this.hostNameList)
                {
                    builder.Append(str + ", ");
                }
                if (builder.Length >= 2)
                {
                    builder.Remove(builder.Length - 2, 2);
                }
                return builder.ToString();
            }
        }

        public List<string> HostNameList
        {
            get
            {
                return this.hostNameList;
            }
        }

        public List<NetworkTcpSession> IncomingSessionList
        {
            get
            {
                return this.incomingSessionList;
            }
        }

        public System.Net.IPAddress IPAddress
        {
            get
            {
                return this.ipAddress;
            }
        }

        public bool IpIsBroadcast
        {
            get
            {
                if (this.sentPackets.Count == 0)
                {
                    byte[] addressBytes = this.ipAddress.GetAddressBytes();
                    byte num = 0x3f;
                    if ((addressBytes[addressBytes.Length - 1] & num) == num)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public bool IpIsMulticast
        {
            get
            {
                byte[] addressBytes = this.ipAddress.GetAddressBytes();
                return (((addressBytes.Length == 4) && (addressBytes[0] >= 0xe0)) && (addressBytes[0] <= 0xef));
            }
        }

        public bool IpIsReserved
        {
            get
            {
                return IpAddressUtil.IsIanaReserved(this.ipAddress);
            }
        }

        public PhysicalAddress MacAddress
        {
            get
            {
                return this.macAddress;
            }
            set
            {
                this.macAddress = value;
                if (value != null)
                {
                    this.recentMacAdresses.Add(value.ToString(), value);
                }
            }
        }

        public SortedList<ushort, NetworkServiceMetadata> NetworkServiceMetadataList
        {
            get
            {
                return this.networkServiceMetadataList;
            }
        }

        public ushort[] OpenTcpPorts
        {
            get
            {
                return this.openTcpPortList.ToArray();
            }
        }

        public OperatingSystemID OS
        {
            get
            {
                Dictionary<OperatingSystemID, double> dictionary = new Dictionary<OperatingSystemID, double>();
                foreach (SortedList<string, double> list in this.operatingSystemCounterList.Values)
                {
                    foreach (string str in list.Keys)
                    {
                        if (str.ToLower().Contains("windows"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.Windows))
                            {
                                dictionary.Add(OperatingSystemID.Windows, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary2;
                                (dictionary2 = dictionary)[OperatingSystemID.Windows] = dictionary2[OperatingSystemID.Windows] + list[str];
                            }
                        }
                        else if (str.ToLower().Contains("linux"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.Linux))
                            {
                                dictionary.Add(OperatingSystemID.Linux, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary3;
                                (dictionary3 = dictionary)[OperatingSystemID.Linux] = dictionary3[OperatingSystemID.Linux] + list[str];
                            }
                        }
                        else if (str.ToLower().Contains("unix"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.UNIX))
                            {
                                dictionary.Add(OperatingSystemID.UNIX, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary4;
                                (dictionary4 = dictionary)[OperatingSystemID.UNIX] = dictionary4[OperatingSystemID.UNIX] + list[str];
                            }
                        }
                        else if (str.ToLower().Contains("freebsd") || str.ToLower().Contains("free bsd"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.FreeBSD))
                            {
                                dictionary.Add(OperatingSystemID.FreeBSD, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary5;
                                (dictionary5 = dictionary)[OperatingSystemID.FreeBSD] = dictionary5[OperatingSystemID.FreeBSD] + list[str];
                            }
                        }
                        else if (str.ToLower().Contains("netbsd") || str.ToLower().Contains("net bsd"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.NetBSD))
                            {
                                dictionary.Add(OperatingSystemID.NetBSD, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary6;
                                (dictionary6 = dictionary)[OperatingSystemID.NetBSD] = dictionary6[OperatingSystemID.NetBSD] + list[str];
                            }
                        }
                        else if (str.ToLower().Contains("solaris"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.Solaris))
                            {
                                dictionary.Add(OperatingSystemID.Solaris, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary7;
                                (dictionary7 = dictionary)[OperatingSystemID.Solaris] = dictionary7[OperatingSystemID.Solaris] + list[str];
                            }
                        }
                        else if ((str.ToLower().Contains("macos") || str.ToLower().Contains("mac os")) || str.Contains("iOS"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.MacOS))
                            {
                                dictionary.Add(OperatingSystemID.MacOS, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary8;
                                (dictionary8 = dictionary)[OperatingSystemID.MacOS] = dictionary8[OperatingSystemID.MacOS] + list[str];
                            }
                        }
                        else if (str.ToLower().Contains("cisco") || str.Contains("IOS"))
                        {
                            if (!dictionary.ContainsKey(OperatingSystemID.Cisco))
                            {
                                dictionary.Add(OperatingSystemID.Cisco, list[str]);
                            }
                            else
                            {
                                Dictionary<OperatingSystemID, double> dictionary9;
                                (dictionary9 = dictionary)[OperatingSystemID.Cisco] = dictionary9[OperatingSystemID.Cisco] + list[str];
                            }
                        }
                        else if (!dictionary.ContainsKey(OperatingSystemID.Other))
                        {
                            dictionary.Add(OperatingSystemID.Other, list[str]);
                        }
                        else
                        {
                            Dictionary<OperatingSystemID, double> dictionary10;
                            (dictionary10 = dictionary)[OperatingSystemID.Other] = dictionary10[OperatingSystemID.Other] + list[str];
                        }
                    }
                }
                OperatingSystemID unknown = OperatingSystemID.Unknown;
                double num = 0.0;
                foreach (OperatingSystemID mid2 in dictionary.Keys)
                {
                    if (dictionary[mid2] > num)
                    {
                        unknown = mid2;
                        num = dictionary[mid2];
                    }
                }
                return unknown;
            }
        }

        public IList<string> OsCounterNames
        {
            get
            {
                return this.operatingSystemCounterList.Keys;
            }
        }

        public List<NetworkTcpSession> OutgoingSessionList
        {
            get
            {
                return this.outgoingSessionList;
            }
        }

        public string PreferredSmbDialect
        {
            get
            {
                return this.preferredSmbDialect;
            }
            set
            {
                this.preferredSmbDialect = value;
            }
        }

        public NetworkPacketList ReceivedPackets
        {
            get
            {
                return this.receivedPackets;
            }
        }

        public NetworkPacketList SentPackets
        {
            get
            {
                return this.sentPackets;
            }
        }

        public byte Ttl
        {
            get
            {
                if (this.ttlCount.Count == 0)
                {
                    return 0;
                }
                int num = 0;
                byte num2 = 0;
                foreach (byte num3 in this.ttlCount.Keys)
                {
                    int num4 = this.ttlCount[num3];
                    if (num4 > num)
                    {
                        num2 = num3;
                        num = num4;
                    }
                }
                return num2;
            }
        }

        public byte TtlDistance
        {
            get
            {
                if (this.ttlDistanceCount.Count == 0)
                {
                    return 0xff;
                }
                int num = 0;
                byte num2 = 0;
                foreach (byte num3 in this.ttlDistanceCount.Keys)
                {
                    int num4 = this.ttlDistanceCount[num3];
                    if (num4 >= num)
                    {
                        num2 = num3;
                        num = num4;
                    }
                }
                return num2;
            }
        }

        public SortedList<string, string> UniversalPlugAndPlayFieldList
        {
            get
            {
                return this.universalPlugAndPlayFieldList;
            }
            set
            {
                this.universalPlugAndPlayFieldList = value;
            }
        }

        public class HostNameComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return string.Compare(x.HostName, y.HostName);
            }
        }

        public class MacAddressComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                string strA = "";
                if (x.MacAddress != null)
                {
                    strA = x.MacAddress.ToString();
                }
                string strB = "";
                if (y.MacAddress != null)
                {
                    strB = y.MacAddress.ToString();
                }
                return string.Compare(strA, strB);
            }
        }

        public class OpenTcpPortsCountComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return (y.OpenTcpPorts.Length - x.OpenTcpPorts.Length);
            }
        }

        public class OperatingSystemComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return string.Compare(x.OS.ToString(), y.OS.ToString());
            }
        }

        public enum OperatingSystemID
        {
            Windows,
            Linux,
            UNIX,
            FreeBSD,
            NetBSD,
            Solaris,
            MacOS,
            Cisco,
            Other,
            Unknown
        }

        public class ReceivedBytesComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return (y.ReceivedPackets.TotalBytes - x.ReceivedPackets.TotalBytes);
            }
        }

        public class ReceivedPacketsComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return (y.ReceivedPackets.Count - x.ReceivedPackets.Count);
            }
        }

        public class SentBytesComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return (y.SentPackets.TotalBytes - x.SentPackets.TotalBytes);
            }
        }

        public class SentPacketsComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return (y.SentPackets.Count - x.SentPackets.Count);
            }
        }

        public class TimeToLiveDistanceComparer : IComparer<NetworkHost>
        {
            public int Compare(NetworkHost x, NetworkHost y)
            {
                return (x.TtlDistance - y.TtlDistance);
            }
        }
    }
}

