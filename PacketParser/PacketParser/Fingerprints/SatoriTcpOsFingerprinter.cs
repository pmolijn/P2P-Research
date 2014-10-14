namespace PacketParser.Fingerprints
{
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Xml;
    using System.Xml.XPath;

    internal class SatoriTcpOsFingerprinter : IOsFingerprinter
    {
        private List<TcpFingerprint> fingerprintList = new List<TcpFingerprint>();

        internal SatoriTcpOsFingerprinter(string satoriTcpXmlFilename)
        {
            FileStream inStream = new FileStream(satoriTcpXmlFilename, FileMode.Open, FileAccess.Read);
            XmlDocument document = new XmlDocument();
            document.Load(inStream);
            foreach (XPathNavigator navigator2 in document.DocumentElement.FirstChild.CreateNavigator().Select("fingerprint"))
            {
                string attribute = navigator2.GetAttribute("os_class", "");
                string os = navigator2.GetAttribute("os_name", "");
                if ((os == null) || (os.Length == 0))
                {
                    os = navigator2.GetAttribute("name", "");
                }
                TcpFingerprint item = new TcpFingerprint(os, attribute);
                this.fingerprintList.Add(item);
                foreach (XPathNavigator navigator3 in navigator2.Select("tcp_tests/test"))
                {
                    item.AddTest(navigator3.Clone());
                }
            }
        }

        public bool TryGetOperatingSystems(out IList<string> osList, IEnumerable<AbstractPacket> packetList)
        {
            try
            {
                TcpPacket tcpPacket = null;
                IPv4Packet ipPacket = null;
                foreach (AbstractPacket packet3 in packetList)
                {
                    if (packet3.GetType() == typeof(TcpPacket))
                    {
                        tcpPacket = (TcpPacket) packet3;
                    }
                    else if (packet3.GetType() == typeof(IPv4Packet))
                    {
                        ipPacket = (IPv4Packet) packet3;
                    }
                }
                if (tcpPacket != null)
                {
                    osList = new List<string>();
                    int num = 3;
                    foreach (TcpFingerprint fingerprint in this.fingerprintList)
                    {
                        int highestMatchWeight = fingerprint.GetHighestMatchWeight(tcpPacket, ipPacket);
                        if (highestMatchWeight > num)
                        {
                            num = highestMatchWeight;
                            osList.Clear();
                            osList.Add(fingerprint.ToString());
                        }
                        else if (highestMatchWeight == num)
                        {
                            osList.Add(fingerprint.ToString());
                        }
                    }
                    if (osList.Count > 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
            }
            osList = null;
            return false;
        }

        public string Name
        {
            get
            {
                return "Satori TCP";
            }
        }

        private class TcpFingerprint
        {
            private string os;
            private string osClass;
            private List<Test> testList;

            internal TcpFingerprint(string os, string osClass)
            {
                this.os = os;
                this.osClass = osClass;
                this.testList = new List<Test>();
            }

            internal void AddTest(XPathNavigator testNavigator)
            {
                this.testList.Add(new Test(testNavigator.Clone(), this.osClass, this.os));
            }

            internal int GetHighestMatchWeight(TcpPacket tcpPacket, IPv4Packet ipPacket)
            {
                int weight = -1;
                foreach (Test test in this.testList)
                {
                    if (((test.Weight >= 3) && (test.Weight > weight)) && test.Matches(tcpPacket, ipPacket))
                    {
                        weight = test.Weight;
                    }
                }
                return weight;
            }

            public override string ToString()
            {
                if (((this.os != null) && (this.os.Length > 0)) && ((this.osClass != null) && (this.osClass.Length > 0)))
                {
                    return (this.osClass + " - " + this.os);
                }
                if ((this.os != null) && (this.os.Length > 0))
                {
                    return this.os;
                }
                if ((this.osClass != null) && (this.osClass.Length > 0))
                {
                    return this.osClass;
                }
                return base.ToString();
            }

            private class Test : AbstractTtlDistanceCalculator
            {
                private NameValueCollection attributeList;
                private P0fOsFingerprintCollection.P0fFingerprint p0fFingerprint;
                private int weight;

                internal Test(XPathNavigator testXPathNavigator, string osClass, string osDetails)
                {
                    testXPathNavigator.MoveToFirstAttribute();
                    this.attributeList = new NameValueCollection();
                    this.p0fFingerprint = null;
                    do
                    {
                        this.attributeList.Add(testXPathNavigator.Name, testXPathNavigator.Value);
                        if (testXPathNavigator.Name == "weight")
                        {
                            this.weight = Convert.ToInt32(testXPathNavigator.Value);
                        }
                        if (testXPathNavigator.Name == "tcpsig")
                        {
                            this.p0fFingerprint = new P0fOsFingerprintCollection.P0fFingerprint(testXPathNavigator.Value + ":" + osClass + ":" + osDetails);
                        }
                    }
                    while (testXPathNavigator.MoveToNextAttribute());
                }

                internal bool Matches(TcpPacket tcpPacket, IPv4Packet ipPacket)
                {
                    foreach (string str in this.attributeList.Keys)
                    {
                        switch (str)
                        {
                            case "tcpflag":
                                if (tcpPacket.FlagBits.Synchronize && !this.attributeList[str].Contains("S"))
                                {
                                    return false;
                                }
                                if (!tcpPacket.FlagBits.Synchronize && this.attributeList[str].Contains("S"))
                                {
                                    return false;
                                }
                                if (tcpPacket.FlagBits.Acknowledgement && !this.attributeList[str].Contains("A"))
                                {
                                    return false;
                                }
                                if (!tcpPacket.FlagBits.Acknowledgement && this.attributeList[str].Contains("A"))
                                {
                                    return false;
                                }
                                if (tcpPacket.FlagBits.Fin && !this.attributeList[str].Contains("F"))
                                {
                                    return false;
                                }
                                if (!tcpPacket.FlagBits.Fin && this.attributeList[str].Contains("F"))
                                {
                                    return false;
                                }
                                break;

                            case "tcpsig":
                                if (this.p0fFingerprint == null)
                                {
                                    return false;
                                }
                                if (!this.p0fFingerprint.Matches(ipPacket, tcpPacket, base.GetOriginalTimeToLive(ipPacket.TimeToLive)))
                                {
                                    return false;
                                }
                                break;
                        }
                    }
                    return true;
                }

                internal int Weight
                {
                    get
                    {
                        return this.weight;
                    }
                }
            }
        }
    }
}

