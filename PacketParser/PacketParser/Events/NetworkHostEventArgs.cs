namespace PacketParser.Events
{
    using PacketParser;
    using System;

    public class NetworkHostEventArgs : EventArgs
    {
        public NetworkHost Host;

        public NetworkHostEventArgs(NetworkHost host)
        {
            this.Host = host;
        }
    }
}

