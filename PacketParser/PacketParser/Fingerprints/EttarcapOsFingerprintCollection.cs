namespace PacketParser.Fingerprints
{
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    internal class EttarcapOsFingerprintCollection : AbstractTtlDistanceCalculator, IOsFingerprinter, ITtlDistanceCalculator
    {
        private int maxTtlDistance = 0x1f;
        private SortedDictionary<string, List<string>> osDictionary;
        private bool[] timeToLiveExists;

        internal EttarcapOsFingerprintCollection(string osFingerprintFilename)
        {
            FileStream stream = new FileStream(osFingerprintFilename, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(stream);
            this.osDictionary = new SortedDictionary<string, List<string>>();
            this.timeToLiveExists = new bool[0x100];
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();
                if ((str.Length > 0) && (str[0] != '#'))
                {
                    string key = null;
                    string item = null;
                    if (str.Length > 0x1d)
                    {
                        key = str.Substring(0, 0x1c);
                        item = str.Substring(0x1d);
                    }
                    if ((key != null) && (item != null))
                    {
                        if (!this.osDictionary.ContainsKey(key))
                        {
                            List<string> list = new List<string> {
                                item
                            };
                            this.osDictionary.Add(key, list);
                        }
                        else
                        {
                            this.osDictionary[key].Add(item);
                        }
                        byte index = byte.Parse(key.Substring(10, 2), NumberStyles.HexNumber);
                        this.timeToLiveExists[index] = true;
                    }
                }
            }
        }

        private string GetEttercapOperatingSystemFingerprint(IPv4Packet ipv4Packet, TcpPacket tcpPacket, byte originalTimeToLive)
        {
            if ((tcpPacket.OptionList == null) || (tcpPacket.OptionList.Count == 0))
            {
                return this.GetEttercapOperatingSystemFingerprint(tcpPacket.WindowSize, null, originalTimeToLive, null, false, false, ipv4Packet.DontFragmentFlag, false, tcpPacket.FlagBits.Synchronize, tcpPacket.FlagBits.Acknowledgement, new int?(ipv4Packet.ParentFrame.Data.Length - ipv4Packet.PacketStartIndex));
            }
            ushort? tcpOptionMaximumSegmentSize = null;
            byte? tcpOptionWindowScaleFactor = null;
            bool tcpOptionSackPermitted = false;
            bool tcpOptionNoOperation = false;
            bool tcpOptionTimestampPresent = false;
            foreach (KeyValuePair<TcpPacket.OptionKinds, byte[]> pair in tcpPacket.OptionList)
            {
                if (pair.Key.Equals(TcpPacket.OptionKinds.MaximumSegmentSize))
                {
                    if ((pair.Value != null) && (pair.Value.Length > 1))
                    {
                        tcpOptionMaximumSegmentSize = new ushort?(ByteConverter.ToUInt16(pair.Value, 0));
                    }
                }
                else if (pair.Key.Equals(TcpPacket.OptionKinds.WindowScaleFactor))
                {
                    if ((pair.Value != null) && (pair.Value.Length > 0))
                    {
                        tcpOptionWindowScaleFactor = new byte?(pair.Value[0]);
                    }
                }
                else if (pair.Key.Equals(TcpPacket.OptionKinds.SackPermitted))
                {
                    tcpOptionSackPermitted = true;
                }
                else if (pair.Key.Equals(TcpPacket.OptionKinds.NoOperation))
                {
                    tcpOptionNoOperation = true;
                }
                else if (pair.Key.Equals(TcpPacket.OptionKinds.Timestamp))
                {
                    tcpOptionTimestampPresent = true;
                }
            }
            return this.GetEttercapOperatingSystemFingerprint(tcpPacket.WindowSize, tcpOptionMaximumSegmentSize, originalTimeToLive, tcpOptionWindowScaleFactor, tcpOptionSackPermitted, tcpOptionNoOperation, ipv4Packet.DontFragmentFlag, tcpOptionTimestampPresent, tcpPacket.FlagBits.Synchronize, tcpPacket.FlagBits.Acknowledgement, new int?(ipv4Packet.ParentFrame.Data.Length - ipv4Packet.PacketStartIndex));
        }

        private string GetEttercapOperatingSystemFingerprint(ushort tcpWindowSize, ushort? tcpOptionMaximumSegmentSize, byte ipTimeToLive, byte? tcpOptionWindowScaleFactor, bool tcpOptionSackPermitted, bool tcpOptionNoOperation, bool ipFlagDontFragment, bool tcpOptionTimestampPresent, bool tcpFlagSyn, bool tcpFlagAck, int? ipPacketTotalLength)
        {
            StringBuilder builder = new StringBuilder(tcpWindowSize.ToString("X4"));
            ushort? nullable = tcpOptionMaximumSegmentSize;
            int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
            if (!nullable3.HasValue)
            {
                builder.Append(":_MSS");
            }
            else
            {
                builder.Append(":" + tcpOptionMaximumSegmentSize.Value.ToString("X4"));
            }
            builder.Append(":" + ipTimeToLive.ToString("X2"));
            byte? nullable4 = tcpOptionWindowScaleFactor;
            int? nullable6 = nullable4.HasValue ? new int?(nullable4.GetValueOrDefault()) : null;
            if (!nullable6.HasValue)
            {
                builder.Append(":WS");
            }
            else
            {
                builder.Append(":" + tcpOptionWindowScaleFactor.Value.ToString("X2"));
            }
            if (tcpOptionSackPermitted)
            {
                builder.Append(":1");
            }
            else
            {
                builder.Append(":0");
            }
            if (tcpOptionNoOperation)
            {
                builder.Append(":1");
            }
            else
            {
                builder.Append(":0");
            }
            if (ipFlagDontFragment)
            {
                builder.Append(":1");
            }
            else
            {
                builder.Append(":0");
            }
            if (tcpOptionTimestampPresent)
            {
                builder.Append(":1");
            }
            else
            {
                builder.Append(":0");
            }
            if (tcpFlagSyn && !tcpFlagAck)
            {
                builder.Append(":S");
            }
            else if (tcpFlagSyn && tcpFlagAck)
            {
                builder.Append(":A");
            }
            else
            {
                return null;
            }
            if (!ipPacketTotalLength.HasValue)
            {
                builder.Append(":LT");
            }
            else
            {
                builder.Append(":" + ipPacketTotalLength.Value.ToString("X2"));
            }
            return builder.ToString();
        }

        private string[] GetOperatingSystems(IPv4Packet ipv4Packet, TcpPacket tcpPacket, byte originalTimeToLive)
        {
            string key = this.GetEttercapOperatingSystemFingerprint(ipv4Packet, tcpPacket, originalTimeToLive);
            if (this.osDictionary.ContainsKey(key))
            {
                return this.osDictionary[key].ToArray();
            }
            if (this.osDictionary.ContainsKey(key.Substring(0, key.Length - 3) + ":LT"))
            {
                return this.osDictionary[key.Substring(0, key.Length - 3) + ":LT"].ToArray();
            }
            return new string[0];
        }

        private byte GetOriginalTimeToLive(IPv4Packet ipv4Packet, TcpPacket tcpPacket)
        {
            for (int i = 0; (i < this.maxTtlDistance) && ((ipv4Packet.TimeToLive + i) <= 0xff); i++)
            {
                if (this.timeToLiveExists[ipv4Packet.TimeToLive + i])
                {
                    string[] strArray = this.GetOperatingSystems(ipv4Packet, tcpPacket, (byte) (ipv4Packet.TimeToLive + i));
                    if ((strArray != null) && (strArray.Length > 0))
                    {
                        return (byte) (ipv4Packet.TimeToLive + i);
                    }
                }
            }
            return base.GetOriginalTimeToLive(ipv4Packet.TimeToLive);
        }

        public override byte GetTtlDistance(byte ipTimeToLive)
        {
            return (byte) (this.GetOriginalTimeToLive(ipTimeToLive) - ipTimeToLive);
        }

        public byte GetTtlDistance(IPv4Packet ipv4Packet, TcpPacket tcpPacket)
        {
            return (byte) (this.GetOriginalTimeToLive(ipv4Packet, tcpPacket) - ipv4Packet.TimeToLive);
        }

        public override string ToString()
        {
            return "Ettercap";
        }

        public bool TryGetOperatingSystems(out IList<string> osList, IEnumerable<AbstractPacket> packetList)
        {
            IPv4Packet packet = null;
            TcpPacket tcpPacket = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(IPv4Packet))
                {
                    packet = (IPv4Packet) packet3;
                }
                else if (packet3.GetType() == typeof(TcpPacket))
                {
                    tcpPacket = (TcpPacket) packet3;
                }
            }
            if (((packet != null) && (tcpPacket != null)) && tcpPacket.FlagBits.Synchronize)
            {
                byte originalTimeToLive = this.GetOriginalTimeToLive(packet, tcpPacket);
                osList = new List<string>();
                foreach (string str in this.GetOperatingSystems(packet, tcpPacket, originalTimeToLive))
                {
                    osList.Add(str);
                }
                if (osList.Count > 0)
                {
                    return true;
                }
            }
            osList = null;
            return false;
        }

        public override bool TryGetTtlDistance(out byte ttlDistance, IEnumerable<AbstractPacket> packetList)
        {
            IPv4Packet packet = null;
            TcpPacket tcpPacket = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(IPv4Packet))
                {
                    packet = (IPv4Packet) packet3;
                }
                else if (packet3.GetType() == typeof(TcpPacket))
                {
                    tcpPacket = (TcpPacket) packet3;
                }
            }
            if ((packet != null) && (tcpPacket != null))
            {
                ttlDistance = this.GetTtlDistance(packet, tcpPacket);
                return true;
            }
            ttlDistance = 0;
            return false;
        }

        public string Name
        {
            get
            {
                return "Ettercap";
            }
        }
    }
}

