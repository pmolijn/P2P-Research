namespace PacketParser.FileTransfer
{
    using PacketParser;
    using pcapFileIO;
    using System;
    using System.Globalization;
    using System.IO;

    public class ReconstructedFile
    {
        private NetworkHost destinationHost;
        private ushort destinationPort;
        private string details;
        private string filename;
        private long fileSize;
        private FileStreamTypes fileStreamType;
        private int initialFrameNumber;
        private string md5Sum;
        private string path;
        private NetworkHost sourceHost;
        private ushort sourcePort;
        private bool tcpTransfer;
        private DateTime timestamp;

        internal ReconstructedFile(string path, NetworkHost sourceHost, NetworkHost destinationHost, ushort sourcePort, ushort destinationPort, bool tcpTransfer, FileStreamTypes fileStreamType, string details, int initialFrameNumber, DateTime timestamp)
        {
            this.path = path;
            try
            {
                if (path.Contains(@"\"))
                {
                    this.filename = path.Substring(path.LastIndexOf('\\') + 1);
                }
                else if (path.Contains("/"))
                {
                    this.filename = path.Substring(path.LastIndexOf('/') + 1);
                }
                else
                {
                    this.filename = path;
                }
            }
            catch (Exception)
            {
                this.filename = "";
            }
            this.sourceHost = sourceHost;
            this.destinationHost = destinationHost;
            this.sourcePort = sourcePort;
            this.destinationPort = destinationPort;
            this.tcpTransfer = tcpTransfer;
            this.fileStreamType = fileStreamType;
            this.details = details;
            FileInfo info = new FileInfo(path);
            this.fileSize = info.Length;
            this.initialFrameNumber = initialFrameNumber;
            this.timestamp = timestamp;
        }

        private string GetFileEnding()
        {
            if (!this.filename.Contains("."))
            {
                return "";
            }
            if (this.filename.EndsWith("."))
            {
                return "";
            }
            return this.filename.Substring(this.filename.LastIndexOf('.') + 1).ToLower();
        }

        public byte[] GetHeaderBytes(int nBytes)
        {
            using (FileStream stream = new FileStream(this.path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, nBytes, FileOptions.SequentialScan))
            {
                byte[] buffer = new byte[nBytes];
                int length = stream.Read(buffer, 0, nBytes);
                stream.Close();
                if (length >= nBytes)
                {
                    return buffer;
                }
                if (length < 0)
                {
                    return null;
                }
                byte[] destinationArray = new byte[length];
                Array.Copy(buffer, destinationArray, length);
                return destinationArray;
            }
        }

        private string GetTransportProtocolString()
        {
            if (this.tcpTransfer)
            {
                return "TCP";
            }
            return "UDP";
        }

        public bool IsIcon()
        {
            string fileEnding = this.GetFileEnding();
            if (fileEnding.Length == 0)
            {
                return false;
            }
            return (fileEnding == "ico");
        }

        public bool IsImage()
        {
            string fileEnding = this.GetFileEnding();
            if (fileEnding.Length == 0)
            {
                return false;
            }
            if (((!(fileEnding == "jpg") && !(fileEnding == "jpeg")) && (!(fileEnding == "gif") && !(fileEnding == "png"))) && ((!(fileEnding == "bmp") && !(fileEnding == "tif")) && !(fileEnding == "tiff")))
            {
                return false;
            }
            return true;
        }

        public bool IsMultipartFormData()
        {
            string fileEnding = this.GetFileEnding();
            if (fileEnding.Length == 0)
            {
                return false;
            }
            return (fileEnding == "mime");
        }

        public override string ToString()
        {
            string str;
            string str2;
            if (this.tcpTransfer)
            {
                str = this.sourceHost.ToString() + " TCP " + this.sourcePort;
                str2 = this.destinationHost.ToString() + " TCP " + this.destinationPort;
            }
            else
            {
                str = this.sourceHost.ToString() + " UDP " + this.sourcePort;
                str2 = this.destinationHost.ToString() + " UDP " + this.destinationPort;
            }
            return (this.filename + "\t" + str + "\t" + str2);
        }

        public NetworkHost DestinationHost
        {
            get
            {
                return this.destinationHost;
            }
        }

        public string DestinationPortString
        {
            get
            {
                return (this.GetTransportProtocolString() + " " + this.destinationPort);
            }
        }

        public string Details
        {
            get
            {
                return this.details;
            }
        }

        public string Filename
        {
            get
            {
                return this.filename;
            }
        }

        public string FilePath
        {
            get
            {
                return this.path;
            }
        }

        public string FileSizeString
        {
            get
            {
                NumberFormatInfo provider = new NumberFormatInfo {
                    NumberDecimalDigits = 0,
                    NumberGroupSizes = new int[] { 3 },
                    NumberGroupSeparator = " "
                };
                return (this.fileSize.ToString("N", provider) + " B");
            }
        }

        public FileStreamTypes FileStreamType
        {
            get
            {
                return this.fileStreamType;
            }
        }

        public int InitialFrameNumber
        {
            get
            {
                return this.initialFrameNumber;
            }
        }

        public string MD5Sum
        {
            get
            {
                if (this.md5Sum == null)
                {
                    this.md5Sum = Md5SingletonHelper.Instance.GetMd5Sum(this.path);
                }
                return this.md5Sum;
            }
        }

        public NetworkHost SourceHost
        {
            get
            {
                return this.sourceHost;
            }
        }

        public string SourcePortString
        {
            get
            {
                return (this.GetTransportProtocolString() + " " + this.sourcePort);
            }
        }

        public DateTime Timestamp
        {
            get
            {
                return this.timestamp;
            }
        }
    }
}

