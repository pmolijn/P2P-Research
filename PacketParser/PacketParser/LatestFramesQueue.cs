namespace PacketParser
{
    using System;
    using System.Collections.Generic;

    internal class LatestFramesQueue : Queue<Frame>
    {
        private int maxSize;

        public LatestFramesQueue(int maxNoFrames)
        {
            this.maxSize = maxNoFrames;
        }

        public new void Enqueue(Frame frame)
        {
            base.Enqueue(frame);
            if (base.Count > this.maxSize)
            {
                base.Dequeue();
            }
        }

        public int MaxSize
        {
            get
            {
                return this.maxSize;
            }
        }
    }
}

