namespace PacketParser.Events
{
    using System;

    public class BufferUsageEventArgs : EventArgs
    {
        public int BufferUsagePercent;

        public BufferUsageEventArgs(int bufferUsagePercent)
        {
            this.BufferUsagePercent = bufferUsagePercent;
        }
    }
}

