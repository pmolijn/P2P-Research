namespace NetworkWrapper
{
    using System;
    using System.Threading;

    public class WinPCapSniffer : ISniffer
    {
        private PacketReceivedEventArgs.PacketTypes basePacketType;
        private int nPacketsReceived = 0;
        private WinPCapWrapper wpcap = new WinPCapWrapper();

        public static  event PacketReceivedHandler PacketReceived;

        public WinPCapSniffer(WinPCapAdapter adapter)
        {
            if (this.wpcap.IsOpen)
            {
                this.wpcap.Close();
            }
            if (!this.wpcap.Open(adapter.NPFName, 0x10000, 1, 0))
            {
                throw new Exception(this.wpcap.LastError);
            }
            this.wpcap.SetMinToCopy(100);
            WinPCapNative.PacketArrivalEventHandler handler = new WinPCapNative.PacketArrivalEventHandler(this.ReceivePacketListener);
            this.wpcap.PacketArrival += handler;
            if (this.wpcap.DataLink == (int) DataLinkType.WTAP_ENCAP_IEEE_802_11)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IEEE_802_11Packet;
            }
            else if (this.wpcap.DataLink == (int)  DataLinkType.WTAP_ENCAP_ETHERNET)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.Ethernet2Packet;
            }
            else if (this.wpcap.DataLink == (int) DataLinkType.WTAP_ENCAP_IEEE_802_11_WLAN_RADIOTAP)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IEEE_802_11RadiotapPacket;
            }
            else if (this.wpcap.DataLink == (int) DataLinkType.WTAP_ENCAP_RAW_IP)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IPv4Packet;
            }
            else if (this.wpcap.DataLink == (int) DataLinkType.WTAP_ENCAP_RAW_IP_2)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IPv4Packet;
            }
            else if (this.wpcap.DataLink == (int) DataLinkType.WTAP_ENCAP_RAW_IP_3)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IPv4Packet;
            }
            else if (this.wpcap.DataLink == (int) DataLinkType.WTAP_ENCAP_CHDLC)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.CiscoHDLC;
            }
            else if (this.wpcap.DataLink == (int) DataLinkType.WTAP_ENCAP_SLL)
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.LinuxCookedCapture;
            }
            else if (adapter.ToString().ToLower().Contains("airpcap"))
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IEEE_802_11Packet;
            }
            else
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.Ethernet2Packet;
            }
        }

        ~WinPCapSniffer()
        {
            this.wpcap.Close();
        }

        private void ReceivePacketListener(object sender, PcapHeader ph, byte[] data)
        {
            this.nPacketsReceived++;
            PacketReceivedEventArgs e = new PacketReceivedEventArgs(data, ph.TimeStamp, this.basePacketType);
            PacketReceived(this, e);
        }

        public void StartSniffing()
        {
            this.wpcap.StartListen();
        }

        public void StopSniffing()
        {
            this.wpcap.StopListen();
        }

        public PacketReceivedEventArgs.PacketTypes BasePacketType
        {
            get
            {
                return this.basePacketType;
            }
        }

        public enum DataLinkType : uint
        {
            WTAP_ENCAP_APPLE_IP_OVER_IEEE1394 = 0x8a,
            WTAP_ENCAP_ARCNET = 7,
            WTAP_ENCAP_ARCNET_LINUX = 0x81,
            WTAP_ENCAP_ATM_PDUS = 0x7b,
            WTAP_ENCAP_ATM_RFC1483 = 100,
            WTAP_ENCAP_ATM_RFC1483_2 = 11,
            WTAP_ENCAP_ATM_RFC1483_3 = 13,
            WTAP_ENCAP_BACNET_MS_TP = 0xa5,
            WTAP_ENCAP_BLUETOOTH_H4 = 0xbb,
            WTAP_ENCAP_CHDLC = 0x68,
            WTAP_ENCAP_CHDLC_2 = 0x70,
            WTAP_ENCAP_CISCO_IOS = 0x76,
            WTAP_ENCAP_DOCSIS = 0x8f,
            WTAP_ENCAP_ENC = 0x6d,
            WTAP_ENCAP_ETHERNET = 1,
            WTAP_ENCAP_FDDI = 10,
            WTAP_ENCAP_FRELAY = 0x6b,
            WTAP_ENCAP_GPRS_LLC = 0xa9,
            WTAP_ENCAP_HHDLC = 0x79,
            WTAP_ENCAP_HIPPI = 0x6f,
            WTAP_ENCAP_IEEE_802_11 = 0x69,
            WTAP_ENCAP_IEEE_802_11_WLAN_AVS = 0xa3,
            WTAP_ENCAP_IEEE_802_11_WLAN_RADIOTAP = 0x7f,
            WTAP_ENCAP_IEEE802_16_MAC_CPS = 0xbc,
            WTAP_ENCAP_IP_OVER_FC = 0x7a,
            WTAP_ENCAP_IRDA = 0x90,
            WTAP_ENCAP_JUNIPER_ATM1 = 0x89,
            WTAP_ENCAP_JUNIPER_ATM2 = 0x87,
            WTAP_ENCAP_JUNIPER_CHDLC = 0xb5,
            WTAP_ENCAP_JUNIPER_ETHER = 0xb2,
            WTAP_ENCAP_JUNIPER_FRELAY = 180,
            WTAP_ENCAP_JUNIPER_GGSN = 0x85,
            WTAP_ENCAP_JUNIPER_MLFR = 0x83,
            WTAP_ENCAP_JUNIPER_MLPPP = 130,
            WTAP_ENCAP_JUNIPER_PPP = 0xb3,
            WTAP_ENCAP_JUNIPER_PPPOE = 0xa7,
            WTAP_ENCAP_JUNIPER_VP = 0xb7,
            WTAP_ENCAP_LANE_802_3 = 110,
            WTAP_ENCAP_LINUX_ATM_CLIP = 0x6a,
            WTAP_ENCAP_LINUX_ATM_CLIP_2 = 0x10,
            WTAP_ENCAP_LINUX_ATM_CLIP_3 = 0x12,
            WTAP_ENCAP_LINUX_ATM_CLIP_4 = 0x13,
            WTAP_ENCAP_LINUX_LAPD = 0xb1,
            WTAP_ENCAP_LOCALTALK = 0x72,
            WTAP_ENCAP_MTP2 = 140,
            WTAP_ENCAP_MTP2_WITH_PHDR = 0x8b,
            WTAP_ENCAP_MTP3 = 0x8d,
            WTAP_ENCAP_NULL = 0,
            WTAP_ENCAP_NULL_2 = 0x6c,
            WTAP_ENCAP_OLD_PFLOG = 0x11,
            WTAP_ENCAP_PFLOG = 0x75,
            WTAP_ENCAP_PPP = 9,
            WTAP_ENCAP_PPP_2 = 50,
            WTAP_ENCAP_PPP_BSDOS = 0x67,
            WTAP_ENCAP_PRISM_HEADER = 0x77,
            WTAP_ENCAP_RAW_IP = 0x65,
            WTAP_ENCAP_RAW_IP_2 = 12,
            WTAP_ENCAP_RAW_IP_3 = 14,
            WTAP_ENCAP_REDBACK = 0x20,
            WTAP_ENCAP_SLIP = 8,
            WTAP_ENCAP_SLIP_BSDOS = 0x66,
            WTAP_ENCAP_SLL = 0x71,
            WTAP_ENCAP_SYMANTEC = 0x63,
            WTAP_ENCAP_TOKEN_RING = 6,
            WTAP_ENCAP_TZSP = 0x80,
            WTAP_ENCAP_USB = 0xba,
            WTAP_ENCAP_USB_LINUX = 0xbd,
            WTAP_ENCAP_USER0 = 0x93,
            WTAP_ENCAP_USER1 = 0x94,
            WTAP_ENCAP_USER10 = 0x9d,
            WTAP_ENCAP_USER11 = 0x9e,
            WTAP_ENCAP_USER12 = 0x9f,
            WTAP_ENCAP_USER13 = 160,
            WTAP_ENCAP_USER14 = 0xa1,
            WTAP_ENCAP_USER15 = 0xa2,
            WTAP_ENCAP_USER2 = 0x95,
            WTAP_ENCAP_USER3 = 150,
            WTAP_ENCAP_USER4 = 0x97,
            WTAP_ENCAP_USER5 = 0x98,
            WTAP_ENCAP_USER6 = 0x99,
            WTAP_ENCAP_USER7 = 0x9a,
            WTAP_ENCAP_USER8 = 0x9b,
            WTAP_ENCAP_USER9 = 0x9c,
            WTAP_GCOM_SERIAL = 0xad,
            WTAP_GCOM_TIE1 = 0xac
        }
    }
}

