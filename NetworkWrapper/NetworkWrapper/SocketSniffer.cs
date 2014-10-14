namespace NetworkWrapper
{
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Threading;

    public class SocketSniffer : ISniffer
    {
        private PacketReceivedEventArgs.PacketTypes basePacketType;
        private byte[] buffer;
        private bool snifferActive;
        private Socket socket;

        public static  event PacketReceivedHandler PacketReceived;

        public SocketSniffer(SocketAdapter adapter)
        {
            this.basePacketType = adapter.BasePacketType;
            this.snifferActive = false;
            this.buffer = new byte[0xffff];
            if (adapter.IP.AddressFamily == AddressFamily.InterNetworkV6)
            {
                this.socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Raw, ProtocolType.Raw);
                this.socket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AcceptConnection, true);
            }
            else
            {
                this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
            }
            IPEndPoint localEP = new IPEndPoint(adapter.IP, 0);
            this.socket.Bind(localEP);
            byte[] buffer2 = new byte[4];
            buffer2[0] = 1;
            byte[] optionInValue = buffer2;
            this.socket.IOControl(IOControlCode.ReceiveAll, optionInValue, null);
        }

        ~SocketSniffer()
        {
            if (this.socket != null)
            {
                this.socket.Close();
            }
        }

        private void ReceivePacketListener(IAsyncResult result)
        {
            int length = this.socket.EndReceive(result);
            try
            {
                byte[] destinationArray = new byte[length];
                Array.Copy(this.buffer, 0, destinationArray, 0, length);
                PacketReceivedEventArgs e = new PacketReceivedEventArgs(destinationArray, DateTime.Now, this.BasePacketType);
                PacketReceived(this, e);
            }
            catch
            {
            }
            if (this.snifferActive)
            {
                this.socket.BeginReceive(this.buffer, 0, this.buffer.Length, SocketFlags.None, new AsyncCallback(this.ReceivePacketListener), null);
            }
        }

        public void StartSniffing()
        {
            this.socket.BeginReceive(this.buffer, 0, this.buffer.Length, SocketFlags.None, new AsyncCallback(this.ReceivePacketListener), null);
            this.snifferActive = true;
        }

        public void StopSniffing()
        {
            this.snifferActive = false;
        }

        public PacketReceivedEventArgs.PacketTypes BasePacketType
        {
            get
            {
                return this.basePacketType;
            }
        }
    }
}

