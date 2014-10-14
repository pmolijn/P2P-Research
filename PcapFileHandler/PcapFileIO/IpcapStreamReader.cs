using System;

namespace pcapFileIO
{
    public interface IpcapStreamReader
    {
        byte[] BlockingRead(int bytesToRead);
        int BlockingRead(byte[] buffer, int offset, int count);

        long Position { get; }
    }
}

