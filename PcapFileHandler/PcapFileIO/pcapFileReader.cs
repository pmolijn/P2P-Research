using System;
using System.IO;
using System.Runtime.CompilerServices;


namespace pcapFileIO
{
    public class pcapFileReader : pcapStreamReader
    {
        private CaseFileLoadedCallback caseFileLoadedCallback = null;
        private string filename;
        private FileStream fileStream;

        public pcapFileReader(string filename) : this(filename, 0x3e8, null)
        {
        }

        public pcapFileReader(string filename, int packetQueueSize, CaseFileLoadedCallback readCompleteCallback) : this(filename, packetQueueSize, readCompleteCallback, true)
        {
        }

        public pcapFileReader(string filename, int packetQueueSize, CaseFileLoadedCallback readCompleteCallback, bool startBackgroundWorkers) : this(filename, new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.Read, 0x40000, FileOptions.SequentialScan), packetQueueSize, readCompleteCallback, startBackgroundWorkers)
        {
        }

        private pcapFileReader(string filename, FileStream fileStream, int packetQueueSize, CaseFileLoadedCallback readCompleteCallback, bool startBackgroundWorkers) : base(fileStream, packetQueueSize, null, startBackgroundWorkers, fileStream.Length)
        {
            this.filename = filename;
            this.fileStream = fileStream;
            base.streamReadCompletedCallback = new pcapStreamReader.StreamReadCompletedCallback(this.StreamReadCompletedCallbackHandler);
        }

        public void StreamReadCompletedCallbackHandler(int framesCount, DateTime fistFrameTimestamp, DateTime lastFrameTimestamp)
        {
            if (this.caseFileLoadedCallback != null)
            {
                this.caseFileLoadedCallback(this.filename, framesCount, fistFrameTimestamp, lastFrameTimestamp);
            }
        }

        public string Filename
        {
            get
            {
                return this.filename;
            }
        }

        public int PercentRead
        {
            get
            {
                return (int) (((this.fileStream.Position - base.PacketBytesInQueue) * 100L) / this.fileStream.Length);
            }
        }

        public new long Position
        {
            get
            {
                return this.fileStream.Position;
            }
            set
            {
                this.fileStream.Position = value;
            }
        }

        public delegate void CaseFileLoadedCallback(string filePathAndName, int framesCount, DateTime firstFrameTimestamp, DateTime lastFrameTimestamp);
    }
}

