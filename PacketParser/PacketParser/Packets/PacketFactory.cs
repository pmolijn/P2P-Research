namespace PacketParser.Packets
{
    using PacketParser;
    using pcapFileIO;
    using System;
    using System.Runtime.InteropServices;

    public class PacketFactory
    {
        public static AbstractPacket GetPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex)
        {
            if (parentFrame.Data.Length <= packetEndIndex)
            {
                return null;
            }
            if (packetEndIndex < packetStartIndex)
            {
                return null;
            }
            return GetPacket(parentFrame, packetStartIndex, packetEndIndex, typeof(RawPacket));
        }

        public static AbstractPacket GetPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex, Type packetType)
        {
            if (packetType == typeof(RawPacket))
            {
                return new RawPacket(parentFrame, packetStartIndex, packetEndIndex);
            }
            Type[] types = new Type[] { typeof(Frame), typeof(int), typeof(int) };
            object[] parameters = new object[] { parentFrame, packetStartIndex, packetEndIndex };
            return (AbstractPacket) packetType.GetConstructor(types).Invoke(parameters);
        }

        public static Type GetPacketType(pcapFrame.DataLinkTypeEnum dataLinkType)
        {
            Type type = typeof(Ethernet2Packet);
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_ETHERNET)
            {
                return typeof(Ethernet2Packet);
            }
            if ((dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_IEEE_802_11) || (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_IEEE_802_11_WLAN_AVS))
            {
                return typeof(IEEE_802_11Packet);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_IEEE_802_11_WLAN_RADIOTAP)
            {
                return typeof(IEEE_802_11RadiotapPacket);
            }
            if (((dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP) || (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP_2)) || ((dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP_3) || (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP4)))
            {
                return typeof(IPv4Packet);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP6)
            {
                return typeof(IPv6Packet);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_CHDLC)
            {
                return typeof(CiscoHdlcPacket);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_SLL)
            {
                return typeof(LinuxCookedCapture);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_PRISM_HEADER)
            {
                return typeof(PrismCaptureHeaderPacket);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_PPI)
            {
                return typeof(PpiPacket);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_PPP)
            {
                return typeof(PointToPointPacket);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_NULL)
            {
                return typeof(NullLoopbackPacket);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_ERF)
            {
                type = typeof(ErfFrame);
            }
            return type;
        }

        public static bool TryGetPacket(out AbstractPacket packet, pcapFrame.DataLinkTypeEnum dataLinkType, Frame parentFrame, int startIndex, int endIndex)
        {
            return TryGetPacket(out packet, GetPacketType(dataLinkType), parentFrame, startIndex, endIndex);
        }

        public static bool TryGetPacket(out AbstractPacket packet, Type packetType, Frame parentFrame, int startIndex, int endIndex)
        {
            packet = null;
            try
            {
                if (packetType.Equals(typeof(Ethernet2Packet)))
                {
                    packet = new Ethernet2Packet(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(IPv4Packet)))
                {
                    packet = new IPv4Packet(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(IPv6Packet)))
                {
                    packet = new IPv6Packet(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(TcpPacket)))
                {
                    packet = new TcpPacket(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(IEEE_802_11Packet)))
                {
                    packet = new IEEE_802_11Packet(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(IEEE_802_11RadiotapPacket)))
                {
                    packet = new IEEE_802_11RadiotapPacket(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(CiscoHdlcPacket)))
                {
                    packet = new CiscoHdlcPacket(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(LinuxCookedCapture)))
                {
                    packet = new LinuxCookedCapture(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(PrismCaptureHeaderPacket)))
                {
                    packet = new PrismCaptureHeaderPacket(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(PpiPacket)))
                {
                    packet = new PpiPacket(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(PointToPointPacket)))
                {
                    packet = new PointToPointPacket(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(NullLoopbackPacket)))
                {
                    packet = new NullLoopbackPacket(parentFrame, startIndex, endIndex);
                }
                else if (packetType.Equals(typeof(ErfFrame)))
                {
                    packet = new ErfFrame(parentFrame, startIndex, endIndex);
                }
                if (packet == null)
                {
                    return false;
                }
                return true;
            }
            catch (Exception)
            {
                packet = new RawPacket(parentFrame, startIndex, endIndex);
                return false;
            }
        }
    }
}

