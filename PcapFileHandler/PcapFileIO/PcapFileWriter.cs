using System;
using System.Collections.Generic;
using System.IO;

namespace pcapFileIO
{
    public class pcapFileWriter : IFrameWriter, IDisposable
    {
        private pcapFrame.DataLinkTypeEnum dataLinkType;
        private string filename;
        private FileStream fileStream;
        private uint framesWritten;
        private bool isOpen;
        private const uint MAGIC_NUMBER = 0xa1b2c3d4;
        private const ushort MAJOR_VERSION_NUMBER = 2;
        private const ushort MINOR_VERSION_NUMBER = 4;
        private DateTime referenceTime;

        public pcapFileWriter(string filename, pcapFrame.DataLinkTypeEnum dataLinkType) : this(filename, dataLinkType, FileMode.Create, 0x800000)
        {
        }

        public pcapFileWriter(string filename, pcapFrame.DataLinkTypeEnum dataLinkType, FileMode fileMode, int bufferSize) : this(filename, dataLinkType, fileMode, bufferSize, false)
        {
        }

        public pcapFileWriter(string filename, pcapFrame.DataLinkTypeEnum dataLinkType, FileMode fileMode, int bufferSize, bool littleEndian)
        {
            this.framesWritten = 0;
            this.filename = filename;
            this.dataLinkType = dataLinkType;
            this.referenceTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            this.fileStream = new FileStream(filename, fileMode, FileAccess.Write, FileShare.Read, bufferSize, FileOptions.SequentialScan);
            this.isOpen = true;
            if ((fileMode != FileMode.Append) || (this.fileStream.Position == 0L))
            {
                List<byte[]> list = new List<byte[]> {
                    ToByteArray((uint) MAGIC_NUMBER),
                    ToByteArray((ushort) 2),
                    ToByteArray((ushort) 4),
                    ToByteArray((uint) 0),
                    ToByteArray((uint) 0),
                    ToByteArray((uint) 0xffff),
                    ToByteArray((uint) dataLinkType)
                };
                foreach (byte[] buffer in list)
                {
                    if (littleEndian)
                    {
                        Array.Reverse(buffer);
                    }
                    this.fileStream.Write(buffer, 0, buffer.Length);
                }
            }
        }

        public void Close()
        {
            this.fileStream.Flush();
            this.fileStream.Close();
            this.isOpen = false;
        }

        public void Dispose()
        {
            this.Close();
        }

        public static byte[] ToByteArray(long value)
        {
            byte[] array = new byte[8];
            ToByteArray((uint) (value >> 0x20), array, 0);
            ToByteArray((uint) value, array, 4);
            return array;
        }

        public static byte[] ToByteArray(ushort value)
        {
            byte[] array = new byte[2];
            ToByteArray(value, array, 0);
            return array;
        }

        public static byte[] ToByteArray(uint value)
        {
            byte[] array = new byte[4];
            ToByteArray(value, array, 0);
            return array;
        }

        public static void ToByteArray(ushort value, byte[] array, int arrayOffset)
        {
            array[arrayOffset] = (byte) (value >> 8);
            array[arrayOffset + 1] = (byte) (value & 0xff);
        }

        public static void ToByteArray(uint value, byte[] array, int arrayOffset)
        {
            array[arrayOffset] = (byte) (value >> 0x18);
            array[arrayOffset + 1] = (byte) ((value >> 0x10) & 0xff);
            array[arrayOffset + 2] = (byte) ((value >> 8) & 0xff);
            array[arrayOffset + 3] = (byte) (value & 0xff);
        }

        public void WriteFrame(pcapFrame frame)
        {
            this.WriteFrame(frame, false);
        }

        public void WriteFrame(pcapFrame frame, bool flush)
        {
            long num = frame.Timestamp.Subtract(this.referenceTime).Ticks / 10L;
            uint num2 = (uint) (num / 0xf4240L);
            uint num3 = (uint) (num % 0xf4240L);
            this.fileStream.Write(ToByteArray(num2), 0, 4);
            this.fileStream.Write(ToByteArray(num3), 0, 4);
            this.fileStream.Write(ToByteArray((uint) frame.Data.Length), 0, 4);
            this.fileStream.Write(ToByteArray((uint) frame.Data.Length), 0, 4);
            this.fileStream.Write(frame.Data, 0, frame.Data.Length);
            if (flush)
            {
                this.fileStream.Flush();
            }
            this.framesWritten++;
        }

        public void WriteFrame(byte[] rawFrameHeaderBytes, byte[] rawFrameDataBytes, bool littleEndian)
        {
            this.fileStream.Write(rawFrameHeaderBytes, 0, rawFrameHeaderBytes.Length);
            this.fileStream.Write(rawFrameDataBytes, 0, rawFrameDataBytes.Length);
        }

        public pcapFrame.DataLinkTypeEnum DataLinkType
        {
            get
            {
                return this.dataLinkType;
            }
        }

        public string Filename
        {
            get
            {
                return this.filename;
            }
        }

        public uint FramesWritten
        {
            get
            {
                return this.framesWritten;
            }
        }

        public bool IsOpen
        {
            get
            {
                return this.isOpen;
            }
        }

        public bool OutputIsPcapNg
        {
            get
            {
                return false;
            }
        }
    }
}

