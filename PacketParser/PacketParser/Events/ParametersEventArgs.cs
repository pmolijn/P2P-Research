namespace PacketParser.Events
{
    using PacketParser;
    using System;
    using System.Collections.Specialized;

    public class ParametersEventArgs : EventArgs
    {
        public NetworkHost DestinationHost;
        public string DestinationPort;
        public string Details;
        public int FrameNumber;
        public NameValueCollection Parameters;
        public NetworkHost SourceHost;
        public string SourcePort;
        public DateTime Timestamp;

        public ParametersEventArgs(int frameNumber, NetworkHost sourceHost, NetworkHost destinationHost, string sourcePort, string destinationPort, NameValueCollection parameters, DateTime timestamp, string details)
        {
            this.FrameNumber = frameNumber;
            this.SourceHost = sourceHost;
            this.DestinationHost = destinationHost;
            this.SourcePort = sourcePort;
            this.DestinationPort = destinationPort;
            this.Parameters = parameters;
            this.Timestamp = timestamp;
            this.Details = details;
        }
    }
}

