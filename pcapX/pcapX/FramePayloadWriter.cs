using PacketParser;
using PacketParser.Packets;
using pcapFileIO;
using System;
using System.IO;

namespace pcapX
{
    internal class FramePayloadWriter : IFrameWriter, IDisposable
    {
        private string filename;
        private FileStream fileStream;
        private Type inputPacketBaseType;
        private bool isOpen;
        private static Type rawPacketType = typeof(RawPacket);
        private static Type tcpPacket = typeof(TcpPacket);
        private static Type udpPacket = typeof(UdpPacket);

        public FramePayloadWriter(string filename, FileMode fileMode, int bufferSize, Type inputPacketBaseType)
        {
            this.filename = filename;
            this.fileStream = new FileStream(filename, fileMode, FileAccess.Write, FileShare.Write, bufferSize, FileOptions.SequentialScan);
            this.isOpen = true;
            this.inputPacketBaseType = inputPacketBaseType;
        }

        public void Close()
        {
            this.fileStream.Flush();
            this.fileStream.Close();
            this.isOpen = false;
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public void WriteFrame(pcapFrame frame)
        {
            this.WriteFrame(frame, false);
        }

        public void WriteFrame(pcapFrame frame, bool flush)
        {
            Frame frame2 = new Frame(frame.Timestamp, frame.Data, this.inputPacketBaseType, 0, false);
            foreach (AbstractPacket packet in frame2.PacketList)
            {
                if (packet.GetType() == rawPacketType)
                {
                    break;
                }
                if ((packet.GetType() == tcpPacket) || (packet.GetType() == udpPacket))
                {
                    foreach (AbstractPacket packet2 in packet.GetSubPackets(false))
                    {
                        byte[] packetData = packet2.GetPacketData();
                        this.fileStream.Write(packetData, 0, packetData.Length);
                        break;
                    }
                    if (flush)
                    {
                        this.fileStream.Flush();
                    }
                    break;
                }
            }
        }

        public void WriteFrame(byte[] rawFrameHeaderBytes, byte[] rawFrameDataBytes, bool littleEndian)
        {
            throw new NotImplementedException();
        }

        public string Filename
        {
            get
            {
                return this.filename;
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
                throw new NotImplementedException();
            }
        }
    }
}

