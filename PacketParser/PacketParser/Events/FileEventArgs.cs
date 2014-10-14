namespace PacketParser.Events
{
    using PacketParser.FileTransfer;
    using System;

    public class FileEventArgs : EventArgs
    {
        public ReconstructedFile File;

        public FileEventArgs(ReconstructedFile file)
        {
            this.File = file;
        }
    }
}

