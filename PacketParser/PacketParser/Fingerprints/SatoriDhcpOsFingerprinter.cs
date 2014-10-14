namespace PacketParser.Fingerprints
{
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;

    internal class SatoriDhcpOsFingerprinter : IOsFingerprinter
    {
        private List<DhcpFingerprint> fingerprintList = new List<DhcpFingerprint>();

        internal SatoriDhcpOsFingerprinter(string satoriDhcpXmlFilename)
        {
            FileStream inStream = new FileStream(satoriDhcpXmlFilename, FileMode.Open, FileAccess.Read);
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
                DhcpFingerprint item = new DhcpFingerprint(os, attribute);
                this.fingerprintList.Add(item);
                foreach (XPathNavigator navigator3 in navigator2.Select("dhcp_tests/test"))
                {
                    item.AddTest(navigator3.Clone());
                }
            }
        }

        public bool TryGetOperatingSystems(out IList<string> osList, IEnumerable<AbstractPacket> packetList)
        {
            try
            {
                DhcpPacket dhcpPacket = null;
                IPv4Packet ipPacket = null;
                foreach (AbstractPacket packet3 in packetList)
                {
                    if (packet3.GetType() == typeof(DhcpPacket))
                    {
                        dhcpPacket = (DhcpPacket) packet3;
                    }
                    else if (packet3.GetType() == typeof(IPv4Packet))
                    {
                        ipPacket = (IPv4Packet) packet3;
                    }
                }
                if (dhcpPacket != null)
                {
                    osList = new List<string>();
                    int num = 3;
                    foreach (DhcpFingerprint fingerprint in this.fingerprintList)
                    {
                        int highestMatchWeight = fingerprint.GetHighestMatchWeight(dhcpPacket, ipPacket);
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
                return "Satori DHCP";
            }
        }

        private class DhcpFingerprint
        {
            private string os;
            private string osClass;
            private List<Test> testList;

            internal DhcpFingerprint(string os, string osClass)
            {
                this.os = os;
                this.osClass = osClass;
                this.testList = new List<Test>();
            }

            internal void AddTest(XPathNavigator testNavigator)
            {
                this.testList.Add(new Test(testNavigator.Clone()));
            }

            internal int GetHighestMatchWeight(DhcpPacket dhcpPacket, IPv4Packet ipPacket)
            {
                int weight = -1;
                foreach (Test test in this.testList)
                {
                    if ((test.Weight > weight) && test.Matches(dhcpPacket, ipPacket))
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

            private class Test
            {
                private NameValueCollection attributeList;
                private int weight;

                internal Test(XPathNavigator testXPathNavigator)
                {
                    testXPathNavigator.MoveToFirstAttribute();
                    this.attributeList = new NameValueCollection();
                    do
                    {
                        this.attributeList.Add(testXPathNavigator.Name, testXPathNavigator.Value);
                        if (testXPathNavigator.Name == "weight")
                        {
                            this.weight = Convert.ToInt32(testXPathNavigator.Value);
                        }
                    }
                    while (testXPathNavigator.MoveToNextAttribute());
                }

                internal bool Matches(DhcpPacket dhcpPacket, IPv4Packet ipPacket)
                {
                    foreach (string str in this.attributeList.Keys)
                    {
                        switch (str)
                        {
                            case "weight":
                            case "matchtype":
                                break;

                            case "dhcptype":
                                if ((dhcpPacket.DhcpMessageType == 1) && (this.attributeList[str] != "Discover"))
                                {
                                    return false;
                                }
                                if ((dhcpPacket.DhcpMessageType == 2) && (this.attributeList[str] != "Offer"))
                                {
                                    return false;
                                }
                                if ((dhcpPacket.DhcpMessageType == 3) && (this.attributeList[str] != "Request"))
                                {
                                    return false;
                                }
                                if ((dhcpPacket.DhcpMessageType == 4) && (this.attributeList[str] != "Decline"))
                                {
                                    return false;
                                }
                                if ((dhcpPacket.DhcpMessageType == 5) && (this.attributeList[str] != "ACK"))
                                {
                                    return false;
                                }
                                if ((dhcpPacket.DhcpMessageType == 6) && (this.attributeList[str] != "NAK"))
                                {
                                    return false;
                                }
                                if ((dhcpPacket.DhcpMessageType == 7) && (this.attributeList[str] != "Release"))
                                {
                                    return false;
                                }
                                if ((dhcpPacket.DhcpMessageType == 8) && (this.attributeList[str] != "Inform"))
                                {
                                    return false;
                                }
                                break;

                            case "dhcpoptions":
                            {
                                StringBuilder builder = new StringBuilder();
                                foreach (DhcpPacket.Option option in dhcpPacket.OptionList)
                                {
                                    builder.Append(option.OptionCode);
                                    builder.Append(",");
                                }
                                if (builder.Length < 1)
                                {
                                    return false;
                                }
                                if (builder.ToString(0, builder.Length - 1) != this.attributeList[str])
                                {
                                    return false;
                                }
                                break;
                            }
                            case "dhcpvendorcode":
                            {
                                DhcpPacket.Option option2 = null;
                                foreach (DhcpPacket.Option option3 in dhcpPacket.OptionList)
                                {
                                    if (option3.OptionCode == 60)
                                    {
                                        option2 = option3;
                                    }
                                }
                                if (option2 == null)
                                {
                                    return false;
                                }
                                if (ByteConverter.ReadString(option2.OptionValue) != this.attributeList[str])
                                {
                                    return false;
                                }
                                break;
                            }
                            case "dhcpttl":
                                if (ipPacket == null)
                                {
                                    return false;
                                }
                                if (ipPacket.TimeToLive.ToString() != this.attributeList[str])
                                {
                                    return false;
                                }
                                break;

                            case "dhcpoption51":
                            {
                                uint num;
                                DhcpPacket.Option option4 = null;
                                foreach (DhcpPacket.Option option5 in dhcpPacket.OptionList)
                                {
                                    if (option5.OptionCode == 0x33)
                                    {
                                        option4 = option5;
                                    }
                                }
                                if (option4 == null)
                                {
                                    return false;
                                }
                                if (uint.TryParse(this.attributeList[str], out num) && (ByteConverter.ToUInt32(option4.OptionValue) != num))
                                {
                                    return false;
                                }
                                if ((ByteConverter.ToUInt32(option4.OptionValue) == uint.MaxValue) && (this.attributeList[str] != "infinite"))
                                {
                                    return false;
                                }
                                break;
                            }
                            case "dhcpoption55":
                            {
                                DhcpPacket.Option option6 = null;
                                foreach (DhcpPacket.Option option7 in dhcpPacket.OptionList)
                                {
                                    if (option7.OptionCode == 0x37)
                                    {
                                        option6 = option7;
                                    }
                                }
                                if (option6 == null)
                                {
                                    return false;
                                }
                                StringBuilder builder2 = new StringBuilder();
                                foreach (byte num2 in option6.OptionValue)
                                {
                                    builder2.Append(num2.ToString());
                                    builder2.Append(",");
                                }
                                if (builder2.Length < 1)
                                {
                                    return false;
                                }
                                if (builder2.ToString(0, builder2.Length - 1) != this.attributeList[str])
                                {
                                    return false;
                                }
                                break;
                            }
                            case "dhcpoption57":
                            {
                                DhcpPacket.Option option8 = null;
                                foreach (DhcpPacket.Option option9 in dhcpPacket.OptionList)
                                {
                                    if (option9.OptionCode == 0x39)
                                    {
                                        option8 = option9;
                                    }
                                }
                                if (option8 == null)
                                {
                                    return false;
                                }
                                if (ByteConverter.ToUInt16(option8.OptionValue) != Convert.ToUInt16(this.attributeList[str]))
                                {
                                    return false;
                                }
                                break;
                            }
                            default:
                                if ((str == "ipttl") && (ipPacket.TimeToLive.ToString() != this.attributeList[str]))
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

