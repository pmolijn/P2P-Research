namespace pcapFileIO
{
    using System;

    public interface IFrameWriter : IDisposable
    {
        void Close();
        void WriteFrame(pcapFrame frame);
        void WriteFrame(pcapFrame frame, bool flush);
        void WriteFrame(byte[] rawFrameHeaderBytes, byte[] rawFrameDataBytes, bool littleEndian);

        string Filename { get; }

        bool IsOpen { get; }

        bool OutputIsPcapNg { get; }
    }
}

