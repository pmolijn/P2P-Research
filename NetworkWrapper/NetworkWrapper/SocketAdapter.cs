namespace NetworkWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Net.Sockets;

    public class SocketAdapter : IAdapter
    {
        private PacketReceivedEventArgs.PacketTypes basePacketType;
        private IPAddress ip;
        private NetworkInterface nic;

        private SocketAdapter(NetworkInterface nic, IPAddress ip)
        {
            this.nic = nic;
            this.ip = ip;
            if (nic.Supports(NetworkInterfaceComponent.IPv4) && (ip.AddressFamily == AddressFamily.InterNetwork))
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IPv4Packet;
            }
            else if (nic.Supports(NetworkInterfaceComponent.IPv6) && (ip.AddressFamily == AddressFamily.InterNetworkV6))
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IPv6Packet;
            }
            else
            {
                this.basePacketType = PacketReceivedEventArgs.PacketTypes.IPv4Packet;
            }
        }

        public static List<IAdapter> GetAdapters()
        {
            NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            List<IAdapter> list = new List<IAdapter>(allNetworkInterfaces.Length);
            foreach (NetworkInterface interface2 in allNetworkInterfaces)
            {
                foreach (UnicastIPAddressInformation information in interface2.GetIPProperties().UnicastAddresses)
                {
                    if ((information.Address != null) && !information.Address.IsIPv6LinkLocal)
                    {
                        list.Add(new SocketAdapter(interface2, information.Address));
                    }
                }
            }
            return list;
        }

        public override string ToString()
        {
            return ("Socket: " + this.nic.Description + " (" + this.ip.ToString() + ")");
        }

        public PacketReceivedEventArgs.PacketTypes BasePacketType
        {
            get
            {
                return this.basePacketType;
            }
        }

        public IPAddress IP
        {
            get
            {
                return this.ip;
            }
        }
    }
}

