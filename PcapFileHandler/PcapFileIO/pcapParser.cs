using System;
using System.Collections.Generic;
using System.IO;


namespace pcapFileIO
{
    public class pcapParser : IpcapParser
    {
        private pcapFrame.DataLinkTypeEnum dataLinkType;
        public const uint LIBPCAP_MAGIC_NUMBER = 0xa1b2c3d4;
        private bool littleEndian;
        private List<KeyValuePair<string, string>> metadata;
        private IpcapStreamReader pcapStreamReader;

        public pcapParser(IpcapStreamReader pcapStreamReader) : this(pcapStreamReader, null)
        {
        }

        public pcapParser(IpcapStreamReader pcapStreamReader, byte[] firstFourBytes)
        {
            this.pcapStreamReader = pcapStreamReader;
            this.metadata = new List<KeyValuePair<string, string>>();
            byte[] buffer = new byte[4];
            byte[] buffer2 = new byte[2];
            if ((firstFourBytes == null) || (firstFourBytes.Length != 4))
            {
                buffer = this.pcapStreamReader.BlockingRead(4);
            }
            else
            {
                buffer = firstFourBytes;
            }
            if (this.ToUInt32(buffer, false) == LIBPCAP_MAGIC_NUMBER)
            {
                this.littleEndian = false;
                this.metadata.Add(new KeyValuePair<string, string>("Endianness", "Big Endian"));
            }
            else if (this.ToUInt32(buffer, true) == LIBPCAP_MAGIC_NUMBER)
            {
                this.littleEndian = true;
                this.metadata.Add(new KeyValuePair<string, string>("Endianness", "Little Endian"));
            }
            else
            {
                string[] strArray = new string[] { "The stream is not a PCAP file. Magic number is ", this.ToUInt32(buffer, false).ToString("X2"), " or ", this.ToUInt32(buffer, true).ToString("X2"), " but should be ", 0xa1b2c3d4.ToString("X2"), "." };
                throw new InvalidDataException(string.Concat(strArray));
            }
            this.pcapStreamReader.BlockingRead(buffer2, 0, 2);
            this.ToUInt16(buffer2, this.littleEndian);
            this.pcapStreamReader.BlockingRead(buffer2, 0, 2);
            this.ToUInt16(buffer2, this.littleEndian);
            this.pcapStreamReader.BlockingRead(buffer, 0, 4);
            this.ToUInt32(buffer, this.littleEndian);
            this.pcapStreamReader.BlockingRead(buffer, 0, 4);
            this.pcapStreamReader.BlockingRead(buffer, 0, 4);
            this.ToUInt32(buffer, this.littleEndian);
            this.pcapStreamReader.BlockingRead(buffer, 0, 4);
            this.dataLinkType = (pcapFrame.DataLinkTypeEnum) this.ToUInt32(buffer, this.littleEndian);
            this.metadata.Add(new KeyValuePair<string, string>("Data Link Type", this.dataLinkType.ToString()));
        }

        public pcapFrame ReadPcapPacketBlocking()
        {
            long num = this.ToUInt32(this.pcapStreamReader.BlockingRead(4), this.littleEndian);
            uint num2 = this.ToUInt32(this.pcapStreamReader.BlockingRead(4), this.littleEndian);
            int bytesToRead = (int) this.ToUInt32(this.pcapStreamReader.BlockingRead(4), this.littleEndian);
            if (bytesToRead > 0x20000)
            {
                throw new Exception("Frame size is too large! Frame size = " + bytesToRead);
            }
            if (bytesToRead < 0)
            {
                throw new Exception("Cannot read frames of negative sizes! Frame size = " + bytesToRead);
            }
            this.pcapStreamReader.BlockingRead(4);
            byte[] data = this.pcapStreamReader.BlockingRead(bytesToRead);
            DateTime time = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            long ticks = (long) (((num * 1000000L) + num2) * 10);
            TimeSpan span = new TimeSpan(ticks);
            return new pcapFrame(time.Add(span), data, this.dataLinkType);
        }

        private ushort ToUInt16(byte[] buffer, bool littleEndian)
        {
            if (littleEndian)
            {
                return (ushort) (buffer[0] ^ (buffer[1] << 8));
            }
            return (ushort) ((buffer[0] << 8) ^ buffer[1]);
        }

        private uint ToUInt32(byte[] buffer, bool littleEndian)
        {
            if (littleEndian)
            {
                return (uint) (((buffer[0] ^ (buffer[1] << 8)) ^ (buffer[2] << 0x10)) ^ (buffer[3] << 0x18));
            }
            return (uint) ((((buffer[0] << 0x18) ^ (buffer[1] << 0x10)) ^ (buffer[2] << 8)) ^ buffer[3]);
        }

        public IList<pcapFrame.DataLinkTypeEnum> DataLinkTypes
        {
            get
            {
                return new pcapFrame.DataLinkTypeEnum[] { this.dataLinkType };
            }
        }

        public List<KeyValuePair<string, string>> Metadata
        {
            get
            {
                return this.metadata;
            }
        }
    }
}

