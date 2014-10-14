namespace PacketParser.FileTransfer
{
    using PacketParser;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class FileStreamAssemblerList : PopularityList<string, FileStreamAssembler>
    {
        private bool decompressGzipStreams;
        private string fileOutputDirectory;
        private PacketParser.PacketHandler packetHandler;

        internal FileStreamAssemblerList(PacketParser.PacketHandler packetHandler, int maxPoolSize, string fileOutputDirectory) : base(maxPoolSize)
        {
            this.packetHandler = packetHandler;
            this.decompressGzipStreams = true;
            this.fileOutputDirectory = Path.GetDirectoryName(fileOutputDirectory);
        }

        internal void Add(FileStreamAssembler assembler)
        {
            string assemblerId = this.GetAssemblerId(assembler);
            base.Add(assemblerId, assembler);
        }

        internal void ClearAll()
        {
            foreach (FileStreamAssembler assembler in base.GetValueEnumerator())
            {
                assembler.Clear();
            }
            base.Clear();
            foreach (string str in Directory.GetDirectories(this.FileOutputDirectory))
            {
                if (str == (this.FileOutputDirectory + Path.DirectorySeparatorChar + "cache"))
                {
                    foreach (string str2 in Directory.GetFiles(str))
                    {
                        try
                        {
                            File.Delete(str2);
                        }
                        catch
                        {
                            this.packetHandler.OnAnomalyDetected("Error deleting file \"" + str2 + "\"");
                        }
                    }
                }
                else
                {
                    try
                    {
                        Directory.Delete(str, true);
                    }
                    catch (Exception)
                    {
                        this.packetHandler.OnAnomalyDetected("Error deleting directory \"" + str + "\"");
                    }
                }
            }
        }

        internal bool ContainsAssembler(FileStreamAssembler assembler)
        {
            string assemblerId = this.GetAssemblerId(assembler);
            return this.ContainsAssembler(assemblerId, false);
        }

        private bool ContainsAssembler(string assemblerId, bool assemblerMustBeActive)
        {
            if (!base.ContainsKey(assemblerId))
            {
                return false;
            }
            if (assemblerMustBeActive)
            {
                return base[assemblerId].IsActive;
            }
            return true;
        }

        internal bool ContainsAssembler(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer)
        {
            string assemblerId = this.GetAssemblerId(sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer);
            return this.ContainsAssembler(assemblerId, false);
        }

        internal bool ContainsAssembler(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer, bool assemblerMustBeActive)
        {
            string assemblerId = this.GetAssemblerId(sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer);
            return this.ContainsAssembler(assemblerId, assemblerMustBeActive);
        }

        internal bool ContainsAssembler(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer, bool assemblerIsAcive, FileStreamTypes fileStreamType)
        {
            string key = this.GetAssemblerId(sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer);
            return ((base.ContainsKey(key) && (base[key].FileStreamType == fileStreamType)) && (base[key].IsActive == assemblerIsAcive));
        }

        internal FileStreamAssembler GetAssembler(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer)
        {
            return this.GetAssembler(sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer, "");
        }

        internal FileStreamAssembler GetAssembler(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer, string extendedFileId)
        {
            string str = this.GetAssemblerId(sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer, extendedFileId);
            return base[str];
        }

        private string GetAssemblerId(FileStreamAssembler assembler)
        {
            return this.GetAssemblerId(assembler.SourceHost, assembler.SourcePort, assembler.DestinationHost, assembler.DestinationPort, assembler.TcpTransfer, assembler.ExtendedFileId);
        }

        private string GetAssemblerId(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer)
        {
            return this.GetAssemblerId(sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer, "");
        }

        private string GetAssemblerId(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer, string extendedFileId)
        {
            return (sourceHost.IPAddress.ToString() + sourcePort.ToString() + destinationHost.IPAddress.ToString() + destinationPort.ToString() + tcpTransfer.ToString() + extendedFileId);
        }

        internal IEnumerable<FileStreamAssembler> GetAssemblers(NetworkHost sourceHost, NetworkHost destinationHost, FileStreamTypes fileStreamType, bool isActive)
        {
            foreach (FileStreamAssembler iteratorVariable0 in this.GetValueEnumerator())
            {
                if (((iteratorVariable0.IsActive == isActive) && (iteratorVariable0.SourceHost == sourceHost)) && ((iteratorVariable0.DestinationHost == destinationHost) && (iteratorVariable0.FileStreamType == fileStreamType)))
                {
                    yield return iteratorVariable0;
                }
            }
        }

        internal void Remove(FileStreamAssembler assembler, bool closeAssembler)
        {
            string assemblerId = this.GetAssemblerId(assembler);
            if (base.ContainsKey(assemblerId))
            {
                base.Remove(assemblerId);
            }
            if (closeAssembler)
            {
                assembler.Clear();
            }
        }

        internal bool DecompressGzipStreams
        {
            get
            {
                return this.decompressGzipStreams;
            }
        }

        internal string FileOutputDirectory
        {
            get
            {
                return this.fileOutputDirectory;
            }
        }

        internal PacketParser.PacketHandler PacketHandler
        {
            get
            {
                return this.packetHandler;
            }
        }

    }
}

