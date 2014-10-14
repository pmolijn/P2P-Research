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
    using System.Text.RegularExpressions;
    using System.Threading;

    internal class SshPacket : AbstractPacket, ISessionPacket
    {
        private string sshApplication;
        private string sshVersion;

        private SshPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "SSH")
        {
            this.sshVersion = null;
            this.sshApplication = null;
            if ((packetEndIndex - base.PacketStartIndex) > 100)
            {
                throw new Exception("Too long SSH banner");
            }
            if ((packetEndIndex - base.PacketStartIndex) < 8)
            {
                throw new Exception("Too short SSH banner");
            }
            int dataIndex = base.PacketStartIndex;
            if (ByteConverter.ReadString(parentFrame.Data, ref dataIndex, 4, false, false, ByteConverter.Encoding.Normal) != "SSH-")
            {
                throw new Exception("Data does not start with SSH-");
            }
            string str = ByteConverter.ReadString(parentFrame.Data, dataIndex, packetEndIndex - dataIndex);
            while (str.EndsWith("\r") || str.EndsWith("\n"))
            {
                str = str.Substring(0, str.Length - 1);
            }
            this.sshVersion = str.Substring(0, str.IndexOf('-'));
            this.sshApplication = str.Substring(str.IndexOf('-') + 1);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            Regex regex = new Regex("^SSH-[12].[0-9]");
            if ((packetEndIndex - packetStartIndex) > 100)
            {
                return false;
            }
            if ((packetEndIndex - packetStartIndex) < 8)
            {
                return false;
            }
            string input = ByteConverter.ReadString(parentFrame.Data, packetStartIndex, packetEndIndex - packetStartIndex);
            if (!regex.IsMatch(input))
            {
                return false;
            }
            try
            {
                result = new SshPacket(parentFrame, packetStartIndex, packetEndIndex);
            }
            catch
            {
                result = null;
            }
            if (result == null)
            {
                return false;
            }
            return true;
        }

        public bool PacketHeaderIsComplete
        {
            get
            {
                return true;
            }
        }

        public int ParsedBytesCount
        {
            get
            {
                return base.PacketLength;
            }
        }

        public string SshApplication
        {
            get
            {
                return this.sshApplication;
            }
        }

        public string SshVersion
        {
            get
            {
                return this.sshVersion;
            }
        }
    }
}

