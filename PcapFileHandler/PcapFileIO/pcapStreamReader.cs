using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace pcapFileIO
{
    public class pcapStreamReader : IDisposable, IpcapStreamReader
    {
        private BackgroundWorker backgroundFileReader;
        private int dequeuedByteCount;
        private int enqueuedByteCount;
        public const int MAX_FRAME_SIZE = 0x20000;
        private Queue<pcapFrame> packetQueue;
        private int packetQueueSize;
        private IpcapParser pcapParser;
        public static IpcapParserFactory PcapParserFactory = new pcapFileIO.pcapParserFactory();
        private Stream pcapStream;
        private StreamIsClosed streamIsClosed;
        private long streamLength;
        protected StreamReadCompletedCallback streamReadCompletedCallback;

        public pcapStreamReader(Stream pcapStream) : this(pcapStream, 0x3e8, null)
        {
        }

        public pcapStreamReader(Stream pcapStream, int packetQueueSize, StreamReadCompletedCallback streamReadCompletedCallback) : this(pcapStream, packetQueueSize, streamReadCompletedCallback, true)
        {
        }

        public pcapStreamReader(Stream pcapStream, int packetQueueSize, StreamReadCompletedCallback streamReadCompletedCallback, bool startBackgroundWorkers) : this(pcapStream, packetQueueSize, streamReadCompletedCallback, startBackgroundWorkers, 0x7fffffffffffffffL)
        {
        }

        public pcapStreamReader(Stream pcapStream, int packetQueueSize, StreamReadCompletedCallback streamReadCompletedCallback, bool startBackgroundWorkers, long streamMaxLength)
        {
            this.pcapStream = pcapStream;
            this.streamLength = streamMaxLength;
            this.packetQueueSize = packetQueueSize;
            this.streamReadCompletedCallback = streamReadCompletedCallback;
            this.pcapParser = PcapParserFactory.CreatePCAPParser(this);
            this.backgroundFileReader = new BackgroundWorker();
            this.packetQueue = new Queue<pcapFrame>(this.packetQueueSize);
            this.enqueuedByteCount = 0;
            this.dequeuedByteCount = 0;
            if (startBackgroundWorkers)
            {
                this.StartBackgroundWorkers();
            }
        }

        public void AbortFileRead()
        {
            this.backgroundFileReader.CancelAsync();
            this.packetQueue.Clear();
        }

        public bool AbortReadingPcapStream()
        {
            return ((this.backgroundFileReader.CancellationPending || this.EndOfStream()) || ((this.streamIsClosed != null) && this.streamIsClosed()));
        }

        private void backgroundFileReader_DoWork(object sender, DoWorkEventArgs e)
        {
            DateTime minValue = DateTime.MinValue;
            DateTime lastFrameTimestamp = DateTime.MinValue;
            int framesCount = 0;
            try
            {
                while (!this.backgroundFileReader.CancellationPending && !this.EndOfStream())
                {
                    if (this.packetQueue.Count < this.packetQueueSize)
                    {
                        pcapFrame item = this.pcapParser.ReadPcapPacketBlocking();
                        if (minValue == DateTime.MinValue)
                        {
                            minValue = item.Timestamp;
                        }
                        lastFrameTimestamp = item.Timestamp;
                        framesCount++;
                        lock (this.packetQueue)
                        {
                            this.packetQueue.Enqueue(item);
                        }
                        this.enqueuedByteCount += item.Data.Length;
                    }
                    else
                    {
                        Thread.Sleep(20);
                    }
                }
            }
            catch (EndOfStreamException)
            {
                this.pcapStream = null;
            }
            catch (Exception exception)
            {
                this.pcapStream = null;
                e.Cancel = true;
                e.Result = exception.Message;
                this.AbortFileRead();
            }
            if (((this.streamReadCompletedCallback != null) && (minValue != DateTime.MinValue)) && (lastFrameTimestamp != DateTime.MinValue))
            {
                this.streamReadCompletedCallback(framesCount, minValue, lastFrameTimestamp);
            }
        }

        private void backgroundFileReader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
        }

        public byte[] BlockingRead(int bytesToRead)
        {
            byte[] buffer = new byte[bytesToRead];
            this.BlockingRead(buffer, 0, bytesToRead);
            return buffer;
        }

        public int BlockingRead(byte[] buffer, int offset, int count)
        {
            int num = this.pcapStream.Read(buffer, offset, count);
            int millisecondsTimeout = 20;
            while (num < count)
            {
                if (this.AbortReadingPcapStream())
                {
                    throw new EndOfStreamException("Done reading");
                }
                if (millisecondsTimeout++ > 200)
                {
                    throw new Exception("Stream reading timed out...");
                }
                Thread.Sleep(millisecondsTimeout);
                num += this.pcapStream.Read(buffer, num, count - num);
            }
            return num;
        }

        public void Dispose()
        {
            if (this.backgroundFileReader != null)
            {
                this.backgroundFileReader.CancelAsync();
            }
            if (this.pcapStream != null)
            {
                this.pcapStream.Close();
                this.pcapStream = null;
            }
        }

        private bool EndOfStream()
        {
            if (this.pcapStream == null)
            {
                return true;
            }
            if (!this.pcapStream.CanRead)
            {
                return true;
            }
            if (this.streamLength == 0x7fffffffffffffffL)
            {
                return false;
            }
            if (this.pcapStream.CanSeek)
            {
                return (this.pcapStream.Position >= this.streamLength);
            }
            try
            {
                return (this.pcapStream.Position >= this.streamLength);
            }
            catch
            {
                return false;
            }
        }

        ~pcapStreamReader()
        {
            if (this.pcapStream != null)
            {
                this.pcapStream.Close();
                this.pcapStream = null;
            }
            this.streamReadCompletedCallback = null;
        }

        public IEnumerable<pcapFrame> PacketEnumerator()
        {
            return this.PacketEnumerator(null, null);
        }

        public IEnumerable<pcapFrame> PacketEnumerator(EmptyDelegate waitFunction, StreamReadCompletedCallback captureCompleteCallback)
        {
            int millisecondsTimeout = 20;
        Label_PostSwitchInIterator:;
            while (!this.backgroundFileReader.CancellationPending && ((this.backgroundFileReader.IsBusy || !this.EndOfStream()) || (this.packetQueue.Count > 0)))
            {
                if (this.packetQueue.Count > 0)
                {
                    pcapFrame iteratorVariable1;
                    millisecondsTimeout = 20;
                    lock (this.packetQueue)
                    {
                        iteratorVariable1 = this.packetQueue.Dequeue();
                    }
                    this.dequeuedByteCount += iteratorVariable1.Data.Length;
                    yield return iteratorVariable1;
                    goto Label_PostSwitchInIterator;
                }
                if (millisecondsTimeout++ > 350)
                {
                    break;
                }
                if (waitFunction != null)
                {
                    waitFunction();
                }
                else
                {
                    Thread.Sleep(millisecondsTimeout);
                }
            }
        }

        public void StartBackgroundWorkers()
        {
            this.backgroundFileReader.DoWork += new DoWorkEventHandler(this.backgroundFileReader_DoWork);
            this.backgroundFileReader.WorkerSupportsCancellation = true;
            this.backgroundFileReader.RunWorkerCompleted += new RunWorkerCompletedEventHandler(this.backgroundFileReader_RunWorkerCompleted);
            this.backgroundFileReader.RunWorkerAsync();
        }

        public void ThreadStart()
        {
            try
            {
                this.backgroundFileReader_DoWork(this, new DoWorkEventArgs(null));
            }
            catch (ThreadAbortException)
            {
                this.AbortFileRead();
            }
        }

        public IList<pcapFrame.DataLinkTypeEnum> FileDataLinkType
        {
            get
            {
                return this.pcapParser.DataLinkTypes;
            }
        }

        public int PacketBytesInQueue
        {
            get
            {
                return (this.enqueuedByteCount - this.dequeuedByteCount);
            }
        }

        public List<KeyValuePair<string, string>> PcapParserMetadata
        {
            get
            {
                return this.pcapParser.Metadata;
            }
        }

        public long Position
        {
            get
            {
                return this.pcapStream.Position;
            }
        }

        public StreamIsClosed StreamIsClosedFunction
        {
            set
            {
                this.streamIsClosed = value;
            }
        }


        public delegate bool AbortReadingDelegate();

        public delegate void EmptyDelegate();

        public delegate bool StreamIsClosed();

        public delegate void StreamReadCompletedCallback(int framesCount, DateTime firstFrameTimestamp, DateTime lastFrameTimestamp);
    }
}

