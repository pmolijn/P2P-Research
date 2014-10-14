namespace PacketParser.Events
{
    using PacketParser;
    using System;
    using System.Collections.Specialized;

    public class MessageEventArgs : EventArgs
    {
        public NameValueCollection Attributes;
        public NetworkHost DestinationHost;
        public string From;
        private const int MAX_SUBJECT_LENGTH = 50;
        public string Message;
        public ApplicationLayerProtocol Protocol;
        public NetworkHost SourceHost;
        public int StartFrameNumber;
        public DateTime StartTimestamp;
        public string Subject;
        public string To;

        public MessageEventArgs(ApplicationLayerProtocol protocol, NetworkHost sourceHost, NetworkHost destinationHost, int startFrameNumber, DateTime startTimestamp, string from, string to, string subject, string message, NameValueCollection attributes)
        {
            this.Protocol = protocol;
            this.SourceHost = sourceHost;
            this.DestinationHost = destinationHost;
            this.StartFrameNumber = startFrameNumber;
            this.StartTimestamp = startTimestamp;
            this.From = from;
            this.To = to;
            this.Subject = subject;
            if ((this.Subject != null) && (this.Subject.Length > 50))
            {
                this.Subject = this.Subject.Substring(0, 50) + "...";
            }
            this.Message = message;
            this.Attributes = attributes;
        }
    }
}

