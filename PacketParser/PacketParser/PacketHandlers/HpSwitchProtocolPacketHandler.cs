namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;

    internal class HpSwitchProtocolPacketHandler : AbstractPacketHandler, IPacketHandler
    {
        public HpSwitchProtocolPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        private void ExtractData(ref NetworkHost sourceHost, HpSwitchProtocolPacket.HpSwField hpswField)
        {
            if (hpswField.TypeByte == 1)
            {
                if (!sourceHost.ExtraDetailsList.ContainsKey("HPSW Device Name"))
                {
                    sourceHost.ExtraDetailsList.Add("HPSW Device Name", hpswField.ValueString);
                    sourceHost.HostNameList.Add(hpswField.ValueString);
                }
            }
            else if (hpswField.TypeByte == 2)
            {
                if (!sourceHost.ExtraDetailsList.ContainsKey("HPSW Firmware version"))
                {
                    sourceHost.ExtraDetailsList.Add("HPSW Firmware version", hpswField.ValueString);
                }
            }
            else if (hpswField.TypeByte == 3)
            {
                if (!sourceHost.ExtraDetailsList.ContainsKey("HPSW Config"))
                {
                    sourceHost.ExtraDetailsList.Add("HPSW Config", hpswField.ValueString);
                }
            }
            else if (hpswField.TypeByte == 14)
            {
                sourceHost.MacAddress = new PhysicalAddress(hpswField.ValueBytes);
            }
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(HpSwitchProtocolPacket))
                {
                    foreach (AbstractPacket packet2 in packetList)
                    {
                        if ((packet2.GetType() == typeof(HpSwitchProtocolPacket.HpSwField)) && (((HpSwitchProtocolPacket.HpSwField) packet2).TypeByte == 5))
                        {
                            IPAddress ip = new IPAddress(((HpSwitchProtocolPacket.HpSwField) packet2).ValueBytes);
                            if ((sourceHost == null) || (sourceHost.IPAddress != ip))
                            {
                                if (base.MainPacketHandler.NetworkHostList.ContainsIP(ip))
                                {
                                    sourceHost = base.MainPacketHandler.NetworkHostList.GetNetworkHost(ip);
                                }
                                else
                                {
                                    sourceHost = new NetworkHost(ip);
                                    base.MainPacketHandler.NetworkHostList.Add(sourceHost);
                                }
                            }
                        }
                    }
                    if (sourceHost != null)
                    {
                        foreach (AbstractPacket packet3 in packetList)
                        {
                            if (packet3.GetType() == typeof(HpSwitchProtocolPacket.HpSwField))
                            {
                                this.ExtractData(ref sourceHost, (HpSwitchProtocolPacket.HpSwField) packet3);
                            }
                        }
                    }
                }
            }
        }

        public void Reset()
        {
        }
    }
}

