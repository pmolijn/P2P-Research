namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Net;
    using System.Text.RegularExpressions;

    internal class DhcpPacketHandler : AbstractPacketHandler, IPacketHandler
    {
        private SortedList<string, IPAddress> previousIpList;

        public DhcpPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.previousIpList = new SortedList<string, IPAddress>();
        }

        private void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, DhcpPacket dhcpPacket)
        {
            if ((dhcpPacket.OpCode == DhcpPacket.OpCodeValue.BootRequest) && ((sourceHost.MacAddress == null) || (dhcpPacket.ClientMacAddress != sourceHost.MacAddress)))
            {
                sourceHost.MacAddress = dhcpPacket.ClientMacAddress;
            }
            else if ((dhcpPacket.OpCode == DhcpPacket.OpCodeValue.BootReply) && ((destinationHost.MacAddress == null) || (dhcpPacket.ClientMacAddress != destinationHost.MacAddress)))
            {
                destinationHost.MacAddress = dhcpPacket.ClientMacAddress;
            }
            if (((dhcpPacket.OpCode == DhcpPacket.OpCodeValue.BootReply) && (dhcpPacket.GatewayIpAddress != null)) && ((dhcpPacket.GatewayIpAddress != IPAddress.None) && (dhcpPacket.GatewayIpAddress.Address > 0L)))
            {
                destinationHost.ExtraDetailsList["Default Gateway"] = dhcpPacket.GatewayIpAddress.ToString();
            }
            NameValueCollection parameters = new NameValueCollection();
            foreach (DhcpPacket.Option option in dhcpPacket.OptionList)
            {
                if (option.OptionCode == 12)
                {
                    string hostname = ByteConverter.ReadString(option.OptionValue);
                    sourceHost.AddHostName(hostname);
                    parameters.Add("DHCP Option 12 Hostname", hostname);
                }
                else if (option.OptionCode == 15)
                {
                    string domainName = ByteConverter.ReadString(option.OptionValue);
                    sourceHost.AddDomainName(domainName);
                    parameters.Add("DHCP Option 15 Domain", domainName);
                }
                else if (option.OptionCode == 50)
                {
                    if (dhcpPacket.DhcpMessageType == 3)
                    {
                        IPAddress ip = new IPAddress(option.OptionValue);
                        if (sourceHost.IPAddress != ip)
                        {
                            if (!base.MainPacketHandler.NetworkHostList.ContainsIP(ip))
                            {
                                NetworkHost host = new NetworkHost(ip) {
                                    MacAddress = sourceHost.MacAddress
                                };
                                base.MainPacketHandler.NetworkHostList.Add(host);
                                sourceHost = host;
                            }
                            else
                            {
                                sourceHost = base.MainPacketHandler.NetworkHostList.GetNetworkHost(ip);
                                if ((dhcpPacket.OpCode == DhcpPacket.OpCodeValue.BootRequest) && ((sourceHost.MacAddress == null) || (dhcpPacket.ClientMacAddress != sourceHost.MacAddress)))
                                {
                                    sourceHost.MacAddress = dhcpPacket.ClientMacAddress;
                                }
                            }
                        }
                        if ((sourceHost.MacAddress != null) && this.previousIpList.ContainsKey(sourceHost.MacAddress.ToString()))
                        {
                            sourceHost.ExtraDetailsList["Previous IP"] = this.previousIpList[sourceHost.MacAddress.ToString()].ToString();
                            this.previousIpList.Remove(sourceHost.MacAddress.ToString());
                        }
                    }
                    else if (dhcpPacket.DhcpMessageType == 1)
                    {
                        IPAddress address2 = new IPAddress(option.OptionValue);
                        this.previousIpList[sourceHost.MacAddress.ToString()] = address2;
                    }
                }
                else if (option.OptionCode == 60)
                {
                    string vendorCode = ByteConverter.ReadString(option.OptionValue);
                    sourceHost.AddDhcpVendorCode(vendorCode);
                    parameters.Add("DHCP Option 60 Vendor Code", vendorCode);
                }
                else if (option.OptionCode == 0x51)
                {
                    string str4 = ByteConverter.ReadString(option.OptionValue, 3, option.OptionValue.Length - 3);
                    sourceHost.AddHostName(str4);
                    parameters.Add("DHCP Option 81 Domain", str4);
                }
                else if (option.OptionCode == 0x7d)
                {
                    parameters.Add("DHCP Option 125 Enterprise Number", ByteConverter.ToUInt32(option.OptionValue, 0).ToString());
                    byte lenght = option.OptionValue[4];
                    if ((lenght > 0) && (option.OptionValue.Length >= (5 + lenght)))
                    {
                        string str5 = ByteConverter.ReadString(option.OptionValue, 5, lenght);
                        parameters.Add("DHCP Option 125 Data", str5);
                    }
                }
                else
                {
                    string input = ByteConverter.ReadString(option.OptionValue);
                    if (!Regex.IsMatch(input, @"[^\u0020-\u007E]"))
                    {
                        parameters.Add("DHCP Option " + option.OptionCode.ToString(), input);
                    }
                }
            }
            if (parameters.Count > 0)
            {
                string sourcePort = "UNKNOWN";
                string destinationPort = "UNKNOWN";
                foreach (AbstractPacket packet in dhcpPacket.ParentFrame.PacketList)
                {
                    if (packet.GetType() == typeof(UdpPacket))
                    {
                        UdpPacket packet2 = (UdpPacket) packet;
                        sourcePort = "UDP " + packet2.SourcePort;
                        destinationPort = "UDP " + packet2.DestinationPort;
                        break;
                    }
                }
                ParametersEventArgs pe = new ParametersEventArgs(dhcpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, sourcePort, destinationPort, parameters, dhcpPacket.ParentFrame.Timestamp, "DHCP Option");
                base.MainPacketHandler.OnParametersDetected(pe);
            }
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(DhcpPacket))
                {
                    this.ExtractData(ref sourceHost, destinationHost, (DhcpPacket) packet);
                }
            }
        }

        public void Reset()
        {
            this.previousIpList.Clear();
        }
    }
}

