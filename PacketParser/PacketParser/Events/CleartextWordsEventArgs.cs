namespace PacketParser.Events
{
    using System;
    using System.Collections.Generic;

    public class CleartextWordsEventArgs : EventArgs
    {
        public int FrameNumber;
        public DateTime Timestamp;
        public int TotalByteCount;
        public int WordCharCount;
        public IList<string> Words;

        public CleartextWordsEventArgs(IList<string> words, int wordCharCount, int totalByteCount, int frameNumber, DateTime timestamp)
        {
            this.Words = words;
            this.WordCharCount = wordCharCount;
            this.TotalByteCount = totalByteCount;
            this.FrameNumber = frameNumber;
            this.Timestamp = timestamp;
        }
    }
}

