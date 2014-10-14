namespace PacketParser.Events
{
    using System;

    public class AnomalyEventArgs : EventArgs
    {
        public string Message;
        public DateTime Timestamp;

        public AnomalyEventArgs(string anomalyMessage, DateTime anomalyTimestamp)
        {
            this.Message = anomalyMessage;
            this.Timestamp = anomalyTimestamp;
        }
    }
}

