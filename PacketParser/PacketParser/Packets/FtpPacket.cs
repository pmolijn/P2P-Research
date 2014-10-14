namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal class FtpPacket : AbstractPacket
    {
        private bool clientToServer;
        private string requestArgument;
        private string requestCommand;
        private string responseArgument;
        private int responseCode;
        private static readonly string[] userCommands = new string[] { 
            "USER", "PASS", "ACCT", "CWD", "CDUP", "SMNT", "QUIT", "REIN", "PORT", "PASV", "TYPE", "STRU", "MODE", "RETR", "STOR", "STOU", 
            "APPE", "ALLO", "REST", "RNFR", "RNTO", "ABOR", "DELE", "RMD", "MKD", "PWD", "LIST", "NLST", "SITE", "SYST", "STAT", "HELP", 
            "NOOP", "FEAT", "SIZE"
         };

        private FtpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex, bool clientToServer) : base(parentFrame, packetStartIndex, packetEndIndex, "FTP")
        {
            this.clientToServer = clientToServer;
            if (!clientToServer)
            {
                int dataIndex = base.PacketStartIndex;
                string str2 = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
                if (!int.TryParse(str2.Substring(0, 3), out this.responseCode))
                {
                    this.responseCode = 0;
                }
                else if (str2.Length > 4)
                {
                    this.responseArgument = str2.Substring(4);
                }
                else
                {
                    this.responseArgument = "";
                }
            }
            else
            {
                int num = base.PacketStartIndex;
                while ((num <= packetEndIndex) && (num < (base.PacketStartIndex + 0x7d0)))
                {
                    string str = ByteConverter.ReadLine(parentFrame.Data, ref num);
                    if (str.Contains(" "))
                    {
                        this.requestCommand = str.Substring(0, str.IndexOf(' '));
                        if (str.Length > (str.IndexOf(' ') + 1))
                        {
                            this.requestArgument = str.Substring(str.IndexOf(' ') + 1);
                        }
                        else
                        {
                            this.requestArgument = "";
                        }
                    }
                }
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        public static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, bool clientToServer, out AbstractPacket result)
        {
            result = null;
            try
            {
                if (clientToServer)
                {
                    char c = (char) parentFrame.Data[packetStartIndex];
                    if (!char.IsLetter(c))
                    {
                        return false;
                    }
                    int dataIndex = packetStartIndex;
                    string str = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
                    if (str.Contains(" "))
                    {
                        str = str.Substring(0, str.IndexOf(' '));
                    }
                    if (Array.IndexOf<string>(userCommands, str.ToUpper()) == -1)
                    {
                        return false;
                    }
                }
                else
                {
                    if (!char.IsDigit((char) parentFrame.Data[packetStartIndex]))
                    {
                        return false;
                    }
                    if (!char.IsDigit((char) parentFrame.Data[packetStartIndex + 1]))
                    {
                        return false;
                    }
                    if (!char.IsDigit((char) parentFrame.Data[packetStartIndex + 2]))
                    {
                        return false;
                    }
                    int num2 = packetStartIndex;
                    if (ByteConverter.ReadLine(parentFrame.Data, ref num2).Contains("ESMTP"))
                    {
                        return false;
                    }
                }
                result = new FtpPacket(parentFrame, packetStartIndex, packetEndIndex, clientToServer);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal bool ClientToServer
        {
            get
            {
                return this.clientToServer;
            }
        }

        internal string RequestArgument
        {
            get
            {
                return this.requestArgument;
            }
        }

        internal string RequestCommand
        {
            get
            {
                return this.requestCommand;
            }
        }

        internal string ResponseArgument
        {
            get
            {
                return this.responseArgument;
            }
        }

        internal int ResponseCode
        {
            get
            {
                return this.responseCode;
            }
        }

    }
}

