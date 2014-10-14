namespace PacketParser.Mime
{
    using System;
    using System.IO;

    internal class ByteArrayStream : Stream
    {
        private byte[] data;
        private long index;

        public ByteArrayStream(byte[] data, long startIndex)
        {
            this.data = data;
            this.index = startIndex;
        }

        public override void Flush()
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.index >= this.data.Length)
            {
                return 0;
            }
            if (count <= 0)
            {
                return 0;
            }
            if (this.data.Length < (this.index + count))
            {
                count = this.data.Length - ((int) this.index);
            }
            if (buffer.Length < (offset + count))
            {
                count = buffer.Length - offset;
            }
            Array.Copy(this.data, this.index, buffer, (long) offset, (long) count);
            this.index += count;
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void SetLength(long value)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return false;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return (long) this.data.Length;
            }
        }

        public override long Position
        {
            get
            {
                return this.index;
            }
            set
            {
                this.index = value;
            }
        }
    }
}

