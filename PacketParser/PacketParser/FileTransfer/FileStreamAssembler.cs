namespace PacketParser.FileTransfer
{
    using PacketParser;
    using PacketParser.Mime;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Text;
    using System.Web;

    public class FileStreamAssembler : IDisposable
    {
        private int assembledByteCount;
        private HttpPacket.ContentEncodings contentEncoding;
        private NetworkHost destinationHost;
        private ushort destinationPort;
        private string details;
        private static char[] directorySeparators = new char[] { '\\', '/' };
        private string extendedFileId;
        private int fileContentLength;
        private string fileLocation;
        private string filename;
        private int fileSegmentRemainingBytes;
        private FileStream fileStream;
        private FileStreamTypes fileStreamType;
        private int initialFrameNumber;
        private bool isActive;
        private FileStreamAssemblerList parentAssemblerList;
        private NetworkHost sourceHost;
        private ushort sourcePort;
        private static char[] specialCharacters = new char[] { ':', '*', '?', '"', '<', '>', '|' };
        private SortedList<uint, byte[]> tcpPacketBufferWindow;
        private bool tcpTransfer;
        private DateTime timestamp;

        internal FileStreamAssembler(FileStreamAssemblerList parentAssemblerList, NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer, FileStreamTypes fileStreamType, string filename, string fileLocation, string details, int initialFrameNumber, DateTime timestamp) : this(parentAssemblerList, sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer, fileStreamType, filename, fileLocation, 0, 0, details, null, initialFrameNumber, timestamp)
        {
        }

        internal FileStreamAssembler(FileStreamAssemblerList parentAssemblerList, NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer, FileStreamTypes fileStreamType, string filename, string fileLocation, string details, string extendedFileId, int initialFrameNumber, DateTime timestamp) : this(parentAssemblerList, sourceHost, sourcePort, destinationHost, destinationPort, tcpTransfer, fileStreamType, filename, fileLocation, 0, 0, details, extendedFileId, initialFrameNumber, timestamp)
        {
        }

        internal FileStreamAssembler(FileStreamAssemblerList parentAssemblerList, NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, bool tcpTransfer, FileStreamTypes fileStreamType, string filename, string fileLocation, int fileContentLength, int fileSegmentRemainingBytes, string details, string extendedFileId, int initialFrameNumber, DateTime timestamp)
        {
            this.parentAssemblerList = parentAssemblerList;
            this.sourceHost = sourceHost;
            this.sourcePort = sourcePort;
            this.destinationHost = destinationHost;
            this.destinationPort = destinationPort;
            this.tcpTransfer = tcpTransfer;
            this.fileStreamType = fileStreamType;
            this.fileContentLength = fileContentLength;
            this.fileSegmentRemainingBytes = fileSegmentRemainingBytes;
            this.details = details;
            this.contentEncoding = HttpPacket.ContentEncodings.Identity;
            this.isActive = false;
            this.extendedFileId = extendedFileId;
            this.initialFrameNumber = initialFrameNumber;
            this.timestamp = timestamp;
            this.filename = filename;
            this.fileLocation = fileLocation;
            FixFilenameAndLocation(ref this.filename, ref this.fileLocation);
            this.assembledByteCount = 0;
            this.tcpPacketBufferWindow = new SortedList<uint, byte[]>();
            if (this.isActive)
            {
                this.fileStream = new FileStream(this.GetFilePath(true), FileMode.Create, FileAccess.ReadWrite);
            }
            else
            {
                this.fileStream = null;
            }
        }

        internal void AddData(TcpPacket tcpPacket)
        {
            if (!this.tcpTransfer)
            {
                throw new Exception("No TCP packets accepted, only UDP");
            }
            if (tcpPacket.PayloadDataLength > 0)
            {
                this.AddData(tcpPacket.GetTcpPacketPayloadData(), tcpPacket.SequenceNumber);
            }
        }

        internal void AddData(byte[] packetData, ushort packetNumber)
        {
            this.AddData(packetData, (uint) packetNumber);
        }

        internal void AddData(byte[] packetData, uint tcpPacketSequenceNumber)
        {
            if (!this.isActive)
            {
                throw new Exception("FileStreamAssembler has not been activated prior to adding data!");
            }
            if ((packetData.Length > 0) && !this.tcpPacketBufferWindow.ContainsKey(tcpPacketSequenceNumber))
            {
                if (((this.FileStreamType != FileStreamTypes.HttpGetChunked) && (this.FileStreamType != FileStreamTypes.TFTP)) && (this.FileContentLength != -1))
                {
                    if (this.fileSegmentRemainingBytes < packetData.Length)
                    {
                        this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Assembler is only expecting data segment length up to " + this.fileSegmentRemainingBytes + " bytes");
                        return;
                    }
                    this.fileSegmentRemainingBytes -= packetData.Length;
                }
                this.tcpPacketBufferWindow.Add(tcpPacketSequenceNumber, packetData);
                this.assembledByteCount += packetData.Length;
                while (this.tcpPacketBufferWindow.Count > 0x40)
                {
                    uint key = this.tcpPacketBufferWindow.Keys[0];
                    this.fileStream.Write(this.tcpPacketBufferWindow[key], 0, this.tcpPacketBufferWindow[key].Length);
                    this.tcpPacketBufferWindow.Remove(key);
                }
                if (((((this.FileStreamType == FileStreamTypes.HttpGetNormal) || (this.FileStreamType == FileStreamTypes.SMB)) || ((this.FileStreamType == FileStreamTypes.SMTP) || (this.fileStreamType == FileStreamTypes.TlsCertificate))) || (((this.fileStreamType == FileStreamTypes.FTP) || (this.fileStreamType == FileStreamTypes.HttpPostMimeMultipartFormData)) || ((this.fileStreamType == FileStreamTypes.HttpPostMimeFileData) || (this.fileStreamType == FileStreamTypes.OscarFileTransfer)))) && ((this.assembledByteCount >= this.fileContentLength) && (this.fileContentLength != -1)))
                {
                    this.FinishAssembling();
                }
                else if (((this.FileStreamType != FileStreamTypes.HttpGetChunked) && (this.FileStreamType != FileStreamTypes.TFTP)) && (this.FileSegmentRemainingBytes == 0))
                {
                    this.isActive = false;
                }
                else if (this.FileStreamType == FileStreamTypes.HttpGetChunked)
                {
                    byte[] buffer = new byte[] { 0x30, 13, 10, 13, 10 };
                    if (packetData.Length >= buffer.Length)
                    {
                        bool flag = true;
                        for (int i = 0; (i < buffer.Length) && flag; i++)
                        {
                            if (packetData[(packetData.Length - buffer.Length) + i] != buffer[i])
                            {
                                flag = false;
                            }
                        }
                        if (flag)
                        {
                            this.FinishAssembling();
                        }
                    }
                }
            }
        }

        internal void Clear()
        {
            this.tcpPacketBufferWindow.Clear();
            if (this.fileStream != null)
            {
                this.fileStream.Close();
                if (System.IO.File.Exists(this.fileStream.Name))
                {
                    System.IO.File.Delete(this.fileStream.Name);
                }
                this.fileStream = null;
            }
        }

        public void Dispose()
        {
            if (this.fileStream != null)
            {
                this.fileStream.Close();
                this.fileStream = null;
            }
        }

        internal void FinishAssembling()
        {
            this.isActive = false;
            try
            {
                foreach (byte[] buffer in this.tcpPacketBufferWindow.Values)
                {
                    this.fileStream.Write(buffer, 0, buffer.Length);
                }
                this.fileStream.Flush();
            }
            catch (Exception exception)
            {
                if (this.fileStream != null)
                {
                    this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error writing final data to file \"" + this.fileStream.Name + "\".\n" + exception.Message);
                }
                else
                {
                    this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error writing final data to file \"" + this.filename + "\".\n" + exception.Message);
                }
            }
            this.tcpPacketBufferWindow.Clear();
            this.parentAssemblerList.Remove(this, false);
            string filePath = this.GetFilePath(false);
            string path = filePath.Substring(0, filePath.Length - this.filename.Length);
            if ((this.fileStreamType != FileStreamTypes.HttpPostMimeMultipartFormData) && !Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                }
                catch (Exception exception2)
                {
                    this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error creating directory \"" + path + "\".\n" + exception2.Message);
                }
            }
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    System.IO.File.Delete(filePath);
                }
                catch (Exception)
                {
                    this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error deleting file \"" + filePath + "\" (tried to replace it)");
                }
            }
            if (((this.fileStreamType != FileStreamTypes.HttpGetChunked) && (!this.parentAssemblerList.DecompressGzipStreams || (this.contentEncoding != HttpPacket.ContentEncodings.Gzip))) && (this.contentEncoding != HttpPacket.ContentEncodings.Deflate))
            {
                if (this.fileStreamType == FileStreamTypes.HttpPostMimeMultipartFormData)
                {
                    UnbufferedReader streamReader = new UnbufferedReader(this.fileStream);
                    List<MultipartPart> formMultipartData = new List<MultipartPart>();
                    foreach (MultipartPart part in PartBuilder.GetParts(streamReader, this.Details))
                    {
                        formMultipartData.Add(part);
                    }
                    this.parentAssemblerList.PacketHandler.ExtractMultipartFormData(formMultipartData, this.sourceHost, this.destinationHost, this.timestamp, this.initialFrameNumber, "TCP " + this.sourcePort, "TCP " + this.destinationPort, ApplicationLayerProtocol.Unknown);
                    foreach (MultipartPart part2 in formMultipartData)
                    {
                        if (((part2.Attributes["filename"] != null) && (part2.Attributes["filename"].Length > 0)) && ((part2.Data != null) && (part2.Data.Length > 0)))
                        {
                            string fileLocation = part2.Attributes["filename"];
                            if (fileLocation.Contains("/"))
                            {
                                fileLocation = fileLocation.Substring(0, fileLocation.LastIndexOf('/'));
                            }
                            if (fileLocation.Contains(@"\"))
                            {
                                fileLocation = fileLocation.Substring(0, fileLocation.LastIndexOf('\\'));
                            }
                            string filename = part2.Attributes["filename"];
                            if (filename.Contains("/") && (filename.Length > (filename.LastIndexOf('/') + 1)))
                            {
                                filename = filename.Substring(filename.LastIndexOf('/') + 1);
                            }
                            if (filename.Contains(@"\") && (filename.Length > (filename.LastIndexOf('\\') + 1)))
                            {
                                filename = filename.Substring(filename.LastIndexOf('\\') + 1);
                            }
                            using (FileStreamAssembler assembler = new FileStreamAssembler(this.parentAssemblerList, this.sourceHost, this.sourcePort, this.destinationHost, this.destinationPort, this.tcpTransfer, FileStreamTypes.HttpPostMimeFileData, filename, fileLocation, part2.Attributes["filename"], this.initialFrameNumber, this.timestamp))
                            {
                                this.parentAssemblerList.Add(assembler);
                                assembler.FileContentLength = part2.Data.Length;
                                assembler.FileSegmentRemainingBytes = part2.Data.Length;
                                if (assembler.TryActivate())
                                {
                                    assembler.AddData(part2.Data, (ushort) 0);
                                }
                            }
                        }
                    }
                    this.fileStream.Close();
                    System.IO.File.Delete(this.GetFilePath(true));
                }
                else
                {
                    if (this.fileStream != null)
                    {
                        this.fileStream.Close();
                    }
                    try
                    {
                        string str5 = this.GetFilePath(true);
                        if (System.IO.File.Exists(str5))
                        {
                            System.IO.File.Move(str5, filePath);
                        }
                    }
                    catch (Exception exception7)
                    {
                        this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error moving file \"" + this.GetFilePath(true) + "\" to \"" + filePath + "\". " + exception7.Message);
                    }
                }
                goto Label_078C;
            }
            this.fileStream.Position = 0L;
            if (((this.fileStreamType == FileStreamTypes.HttpGetChunked) && this.parentAssemblerList.DecompressGzipStreams) && (this.contentEncoding == HttpPacket.ContentEncodings.Gzip))
            {
                using (DeChunkedDataStream stream = new DeChunkedDataStream(this.fileStream))
                {
                    using (GZipStream stream2 = new GZipStream(stream, CompressionMode.Decompress))
                    {
                        try
                        {
                            this.WriteStreamToFile(stream2, filePath);
                        }
                        catch (Exception exception3)
                        {
                            this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error: Cannot write to file " + filePath + " (" + exception3.Message + ")");
                        }
                        stream2.Close();
                    }
                    stream.Close();
                    goto Label_0424;
                }
            }
            if ((this.fileStreamType == FileStreamTypes.HttpGetChunked) && (this.contentEncoding == HttpPacket.ContentEncodings.Deflate))
            {
                using (DeChunkedDataStream stream3 = new DeChunkedDataStream(this.fileStream))
                {
                    using (DeflateStream stream4 = new DeflateStream(stream3, CompressionMode.Decompress))
                    {
                        try
                        {
                            this.WriteStreamToFile(stream4, filePath);
                        }
                        catch (Exception exception4)
                        {
                            this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error: Cannot write to file " + filePath + " (" + exception4.Message + ")");
                        }
                        stream4.Close();
                    }
                    stream3.Close();
                    goto Label_0424;
                }
            }
            if (this.fileStreamType == FileStreamTypes.HttpGetChunked)
            {
                using (DeChunkedDataStream stream5 = new DeChunkedDataStream(this.fileStream))
                {
                    try
                    {
                        this.WriteStreamToFile(stream5, filePath);
                    }
                    catch (Exception exception5)
                    {
                        this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error: Cannot write to file " + filePath + " (" + exception5.Message + ")");
                    }
                    stream5.Close();
                    goto Label_0424;
                }
            }
            using (GZipStream stream6 = new GZipStream(this.fileStream, CompressionMode.Decompress))
            {
                try
                {
                    this.WriteStreamToFile(stream6, filePath);
                }
                catch (Exception exception6)
                {
                    this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error: Cannot write to file " + filePath + " (" + exception6.Message + ")");
                }
                stream6.Close();
            }
        Label_0424:
            this.fileStream.Close();
            System.IO.File.Delete(this.GetFilePath(true));
        Label_078C:
            if (System.IO.File.Exists(filePath))
            {
                try
                {
                    ReconstructedFile file = new ReconstructedFile(filePath, this.sourceHost, this.destinationHost, this.sourcePort, this.destinationPort, this.tcpTransfer, this.fileStreamType, this.details, this.initialFrameNumber, this.timestamp);
                    this.parentAssemblerList.PacketHandler.AddReconstructedFile(file);
                }
                catch (Exception exception8)
                {
                    this.parentAssemblerList.PacketHandler.OnAnomalyDetected("Error creating reconstructed file: " + exception8.Message);
                }
            }
        }

        private static void FixFilenameAndLocation(ref string filename, ref string fileLocation)
        {
            if (filename.Contains("/"))
            {
                fileLocation = filename.Substring(0, filename.LastIndexOf('/') + 1);
                filename = filename.Substring(filename.LastIndexOf('/') + 1);
            }
            if (filename.Contains(@"\"))
            {
                fileLocation = filename.Substring(0, filename.LastIndexOf('\\') + 1);
                filename = filename.Substring(filename.LastIndexOf('\\') + 1);
            }
            filename = HttpUtility.UrlDecode(filename);
            while (filename.IndexOfAny(specialCharacters) > -1)
            {
                filename = filename.Remove(filename.IndexOfAny(specialCharacters), 1);
            }
            while (filename.IndexOfAny(directorySeparators) > -1)
            {
                filename = filename.Remove(filename.IndexOfAny(directorySeparators), 1);
            }
            while (filename.StartsWith("."))
            {
                filename = filename.Substring(1);
            }
            if (filename.Length > 0x20)
            {
                int startIndex = filename.LastIndexOf('.');
                if ((startIndex < 0) || (startIndex <= (filename.Length - 20)))
                {
                    filename = filename.Substring(0, 20);
                }
                else
                {
                    filename = filename.Substring(0, (20 - filename.Length) + startIndex) + filename.Substring(startIndex);
                }
            }
            fileLocation = HttpUtility.UrlDecode(fileLocation);
            fileLocation = fileLocation.Replace("..", "_");
            fileLocation = fileLocation.Replace('\\', '/');
            while (fileLocation.IndexOfAny(specialCharacters) > -1)
            {
                fileLocation = fileLocation.Remove(fileLocation.IndexOfAny(specialCharacters), 1);
            }
            if ((fileLocation.Length > 0) && !fileLocation.StartsWith("/"))
            {
                fileLocation = "/" + fileLocation;
            }
            fileLocation = fileLocation.TrimEnd(directorySeparators);
            if (fileLocation.Length > 40)
            {
                fileLocation = fileLocation.Substring(0, 40);
            }
        }

        private string GetFilePath(bool tempCachePath)
        {
            return GetFilePath(tempCachePath, this.tcpTransfer, this.sourceHost.IPAddress, this.destinationHost.IPAddress, this.sourcePort, this.destinationPort, this.fileStreamType, this.fileLocation, this.filename, this.parentAssemblerList, this.ExtendedFileId);
        }

        private static string GetFilePath(bool tempCachePath, bool tcpTransfer, IPAddress sourceIp, IPAddress destinationIp, ushort sourcePort, ushort destinationPort, FileStreamTypes fileStreamType, string fileLocation, string filename, FileStreamAssemblerList parentAssemblerList, string extendedFileId)
        {
            string str;
            string str2;
            string str3;
            string str4;
            string str5;
            if (filename.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                foreach (char ch in Path.GetInvalidFileNameChars())
                {
                    filename.Replace(ch, '_');
                }
            }
            fileLocation = fileLocation.Replace("..", "_");
            if (fileLocation.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
            {
                foreach (char ch2 in Path.GetInvalidPathChars())
                {
                    fileLocation.Replace(ch2, '_');
                }
            }
            if ((fileStreamType == FileStreamTypes.HttpGetNormal) || (fileStreamType == FileStreamTypes.HttpGetChunked))
            {
                str2 = "HTTP";
            }
            else if (fileStreamType == FileStreamTypes.SMB)
            {
                str2 = "SMB";
            }
            else if (fileStreamType == FileStreamTypes.TFTP)
            {
                str2 = "TFTP";
            }
            else if (fileStreamType == FileStreamTypes.TlsCertificate)
            {
                str2 = "TLS_Cert";
            }
            else if (fileStreamType == FileStreamTypes.FTP)
            {
                str2 = "FTP";
            }
            else if (fileStreamType == FileStreamTypes.HttpPostMimeMultipartFormData)
            {
                str2 = "MIME_form-data";
            }
            else if (fileStreamType == FileStreamTypes.HttpPostMimeFileData)
            {
                str2 = "MIME_file-data";
            }
            else if (fileStreamType == FileStreamTypes.OscarFileTransfer)
            {
                str2 = "OSCAR";
            }
            else
            {
                if (fileStreamType != FileStreamTypes.SMTP)
                {
                    throw new Exception("Not implemented yet");
                }
                str2 = "SMTP";
            }
            if (tcpTransfer)
            {
                str3 = "TCP";
            }
            else
            {
                str3 = "UDP";
            }
            if (tempCachePath)
            {
                str = "cache/" + sourceIp.ToString().Replace(':', '-') + "_" + str3 + sourcePort.ToString() + " - " + destinationIp.ToString().Replace(':', '-') + "_" + str3 + destinationPort.ToString() + "_" + str2 + extendedFileId + ".txt";
            }
            else
            {
                str = sourceIp.ToString().Replace(':', '-') + "/" + str2 + " - " + str3 + " " + sourcePort.ToString() + fileLocation + "/" + filename;
                try
                {
                    Path.GetDirectoryName(str);
                }
                catch
                {
                    str = sourceIp.ToString().Replace(':', '-') + "/" + str2 + " - " + str3 + " " + sourcePort.ToString() + "/" + Path.GetRandomFileName();
                }
            }
            str = parentAssemblerList.FileOutputDirectory + Path.DirectorySeparatorChar + str;
            if ((Path.DirectorySeparatorChar != '/') && str.Contains("/"))
            {
                str = str.Replace('/', Path.DirectorySeparatorChar);
            }
            if (tempCachePath || !System.IO.File.Exists(str))
            {
                return str;
            }
            int num = 1;
            int length = str.LastIndexOf('.');
            int num3 = str.LastIndexOf(Path.DirectorySeparatorChar);
            if (length < 0)
            {
                str4 = str;
                str5 = "";
            }
            else if (length > num3)
            {
                str4 = str.Substring(0, length);
                str5 = str.Substring(length);
            }
            else
            {
                str4 = str;
                str5 = "";
            }
            string path = string.Concat(new object[] { str4, "[", num, "]", str5 });
            while (System.IO.File.Exists(path))
            {
                num++;
                path = string.Concat(new object[] { str4, "[", num, "]", str5 });
            }
            return path;
        }

        internal void SetRemainingBytesInFile(int remainingByteCount)
        {
            this.fileContentLength = this.assembledByteCount + remainingByteCount;
        }

        internal bool TryActivate()
        {
            try
            {
                if (this.fileStream == null)
                {
                    this.fileStream = new FileStream(this.GetFilePath(true), FileMode.Create, FileAccess.ReadWrite);
                }
                this.isActive = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal void WriteStreamToFile(Stream stream, string destinationPath)
        {
            using (FileStream stream2 = new FileStream(destinationPath, FileMode.Create))
            {
                byte[] buffer = new byte[0x400];
                while (true)
                {
                    int count = stream.Read(buffer, 0, buffer.Length);
                    if (count == 0)
                    {
                        break;
                    }
                    stream2.Write(buffer, 0, count);
                }
                stream2.Close();
            }
        }

        internal int AssembledByteCount
        {
            get
            {
                return this.assembledByteCount;
            }
        }

        internal HttpPacket.ContentEncodings ContentEncoding
        {
            get
            {
                return this.contentEncoding;
            }
            set
            {
                this.contentEncoding = value;
                if (!this.parentAssemblerList.DecompressGzipStreams && !this.filename.EndsWith(".gz"))
                {
                    this.filename = this.filename + ".gz";
                }
            }
        }

        internal NetworkHost DestinationHost
        {
            get
            {
                return this.destinationHost;
            }
        }

        internal ushort DestinationPort
        {
            get
            {
                return this.destinationPort;
            }
        }

        internal string Details
        {
            get
            {
                return this.details;
            }
        }

        internal string ExtendedFileId
        {
            get
            {
                if (this.extendedFileId == null)
                {
                    return string.Empty;
                }
                return this.extendedFileId;
            }
            set
            {
                this.extendedFileId = value;
            }
        }

        internal int FileContentLength
        {
            get
            {
                return this.fileContentLength;
            }
            set
            {
                this.fileContentLength = value;
            }
        }

        internal string FileLocation
        {
            get
            {
                return this.fileLocation;
            }
        }

        internal string Filename
        {
            get
            {
                return this.filename;
            }
            set
            {
                this.filename = value;
            }
        }

        internal int FileSegmentRemainingBytes
        {
            get
            {
                return this.fileSegmentRemainingBytes;
            }
            set
            {
                this.fileSegmentRemainingBytes = value;
            }
        }

        internal FileStreamTypes FileStreamType
        {
            get
            {
                return this.fileStreamType;
            }
            set
            {
                this.fileStreamType = value;
            }
        }

        internal bool IsActive
        {
            get
            {
                return this.isActive;
            }
        }

        internal NetworkHost SourceHost
        {
            get
            {
                return this.sourceHost;
            }
        }

        internal ushort SourcePort
        {
            get
            {
                return this.sourcePort;
            }
        }

        internal bool TcpTransfer
        {
            get
            {
                return this.tcpTransfer;
            }
        }

        internal class DeChunkedDataStream : Stream, IDisposable
        {
            private Stream chunkedStream;
            private int currentChunkSize;
            private int readBytesInCurrentChunk;

            public DeChunkedDataStream(Stream chunkedStream)
            {
                this.chunkedStream = chunkedStream;
                this.currentChunkSize = 0;
                this.readBytesInCurrentChunk = 0;
            }

            public new void Dispose()
            {
                if (this.chunkedStream != null)
                {
                    this.chunkedStream.Close();
                    this.chunkedStream = null;
                }
                base.Dispose();
            }

            public override void Flush()
            {
                throw new Exception("The method or operation is not implemented.");
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                int num2;
                int num = 0;
                if (this.readBytesInCurrentChunk < this.currentChunkSize)
                {
                    goto Label_00AB;
                }
                StringBuilder builder = new StringBuilder();
            Label_0019:
                num2 = this.chunkedStream.ReadByte();
                if (num2 < 0)
                {
                    return 0;
                }
                byte num3 = (byte) num2;
                if (num3 != 13)
                {
                    char ch = (char) num3;
                    string str = "0123456789abcdefABCDEF";
                    if (str.Contains(ch.ToString()))
                    {
                        builder.Append((char) num3);
                    }
                    goto Label_0019;
                }
                this.chunkedStream.ReadByte();
                if (builder.Length <= 0)
                {
                    goto Label_0019;
                }
                if (builder.ToString().Length == 0)
                {
                    this.currentChunkSize = 0;
                }
                else
                {
                    this.currentChunkSize = Convert.ToInt32(builder.ToString(), 0x10);
                }
                this.readBytesInCurrentChunk = 0;
                if (this.currentChunkSize == 0)
                {
                    return 0;
                }
            Label_00AB:
                num = this.chunkedStream.Read(buffer, offset, Math.Min(count, this.currentChunkSize - this.readBytesInCurrentChunk));
                this.readBytesInCurrentChunk += num;
                if ((num < count) && (this.readBytesInCurrentChunk == this.currentChunkSize))
                {
                    return (num + this.Read(buffer, offset + num, count - num));
                }
                return num;
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
                    throw new Exception("The method or operation is not implemented.");
                }
            }

            public override long Position
            {
                get
                {
                    throw new Exception("The method or operation is not implemented.");
                }
                set
                {
                    throw new Exception("The method or operation is not implemented.");
                }
            }
        }
    }
}

