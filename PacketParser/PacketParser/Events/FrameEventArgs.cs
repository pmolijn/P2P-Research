namespace PacketParser.Events
{
    using PacketParser;
    using System;

    public class FrameEventArgs : EventArgs
    {
        public PacketParser.Frame Frame;

        public FrameEventArgs(PacketParser.Frame frame)
        {
            this.Frame = frame;
        }
    }
}

