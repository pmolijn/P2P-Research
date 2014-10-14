namespace PacketParser
{
    using System;

    public class UdpPortProtocolFinder : IPortProtocolFinder
    {
        private static IPortProtocolFinder instance;

        public ApplicationLayerProtocol GetApplicationLayerProtocol(TransportLayerProtocol transport, ushort sourcePort, ushort destinationPort)
        {
            if ((((destinationPort == 0x35) || (sourcePort == 0x35)) || ((destinationPort == 0x14e9) || (sourcePort == 0x14e9))) || ((destinationPort == 0x14eb) || (sourcePort == 0x14eb)))
            {
                return ApplicationLayerProtocol.Dns;
            }
            if (((destinationPort == 0x43) || (destinationPort == 0x44)) || ((sourcePort == 0x43) || (sourcePort == 0x44)))
            {
                return ApplicationLayerProtocol.Dhcp;
            }
            if ((destinationPort == 0x45) || (sourcePort == 0x45))
            {
                return ApplicationLayerProtocol.Tftp;
            }
            if ((destinationPort == 0x89) || (sourcePort == 0x89))
            {
                return ApplicationLayerProtocol.NetBiosNameService;
            }
            if ((destinationPort == 0x8a) || (sourcePort == 0x8a))
            {
                return ApplicationLayerProtocol.NetBiosDatagramService;
            }
            if ((destinationPort == 0x202) || (sourcePort == 0x202))
            {
                return ApplicationLayerProtocol.Syslog;
            }
            if ((destinationPort == 0x76c) || (sourcePort == 0x76c))
            {
                return ApplicationLayerProtocol.Upnp;
            }
            if ((destinationPort != 0x13c4) && (sourcePort != 0x13c4))
            {
                return ApplicationLayerProtocol.Unknown;
            }
            return ApplicationLayerProtocol.Sip;
        }

        public static IPortProtocolFinder Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UdpPortProtocolFinder();
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }
    }
}

