namespace PacketParser.Events
{
    using PacketParser;
    using System;

    public class KeywordEventArgs
    {
        public NetworkHost DestinationHost;
        public string DestinationPort;
        public PacketParser.Frame Frame;
        public int KeywordIndex;
        public int KeywordLength;
        public NetworkHost SourceHost;
        public string SourcePort;

        public KeywordEventArgs(PacketParser.Frame frame, int keywordIndex, int keywordLength, NetworkHost sourceHost, NetworkHost destinationHost, string sourcePort, string destinationPort)
        {
            this.Frame = frame;
            this.KeywordIndex = keywordIndex;
            this.KeywordLength = keywordLength;
            this.SourceHost = sourceHost;
            this.DestinationHost = destinationHost;
            this.SourcePort = sourcePort;
            this.DestinationPort = destinationPort;
        }
    }
}

