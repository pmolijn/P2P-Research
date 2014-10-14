namespace PacketParser.Events
{
    using PacketParser;
    using System;

    public class SessionEventArgs : EventArgs
    {
        public NetworkHost Client;
        public ushort ClientPort;
        public ApplicationLayerProtocol Protocol;
        public NetworkHost Server;
        public ushort ServerPort;
        public int StartFrameNumber;
        public DateTime StartTimestamp;
        public bool Tcp;

        public SessionEventArgs(ApplicationLayerProtocol protocol, NetworkHost client, NetworkHost server, ushort clientPort, ushort serverPort, bool tcp, int startFrameNumber, DateTime startTimestamp)
        {
            this.Protocol = protocol;
            this.Client = client;
            this.Server = server;
            this.ClientPort = clientPort;
            this.ServerPort = serverPort;
            this.Tcp = tcp;
            this.StartFrameNumber = startFrameNumber;
            this.StartTimestamp = startTimestamp;
        }
    }
}

