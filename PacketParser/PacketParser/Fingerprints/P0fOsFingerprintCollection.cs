namespace PacketParser.Fingerprints
{
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class P0fOsFingerprintCollection : AbstractTtlDistanceCalculator, IOsFingerprinter, ITtlDistanceCalculator
    {
        private int maxTtlDistance = 0x1f;
        private List<P0fFingerprint> synAckOsFingerprints;
        private List<P0fFingerprint> synOsFingerprints;
        private bool[] timeToLiveExists = new bool[0x100];

        internal P0fOsFingerprintCollection(string synFingerprintFile, string synAckFingerprintFile)
        {
            this.synOsFingerprints = this.GetFingerprintList(synFingerprintFile);
            this.synAckOsFingerprints = this.GetFingerprintList(synAckFingerprintFile);
        }

        private List<P0fFingerprint> GetFingerprintList(string fingerprintFile)
        {
            FileStream stream = new FileStream(fingerprintFile, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(stream);
            List<P0fFingerprint> list = new List<P0fFingerprint>();
            while (!reader.EndOfStream)
            {
                string fingerprintString = reader.ReadLine();
                if ((fingerprintString.Length > 10) && (fingerprintString[0] != '#'))
                {
                    P0fFingerprint item = new P0fFingerprint(fingerprintString);
                    list.Add(item);
                    this.timeToLiveExists[item.InitialTTL] = true;
                }
            }
            return list;
        }

        private byte GetOriginalTimeToLive(IPv4Packet ipv4Packet, TcpPacket tcpPacket)
        {
            for (int i = 0; (i < this.maxTtlDistance) && ((ipv4Packet.TimeToLive + i) <= 0xff); i++)
            {
                if (this.timeToLiveExists[ipv4Packet.TimeToLive + i] && ((ipv4Packet.TimeToLive + i) == this.GetOriginalTimeToLive(ipv4Packet.TimeToLive)))
                {
                    return (byte) (ipv4Packet.TimeToLive + i);
                }
            }
            for (int j = 0; (j < this.maxTtlDistance) && ((ipv4Packet.TimeToLive + j) <= 0xff); j++)
            {
                if (this.timeToLiveExists[ipv4Packet.TimeToLive + j])
                {
                    return (byte) (ipv4Packet.TimeToLive + j);
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
                List<P0fFingerprint> synAckOsFingerprints;
                if (tcpPacket.FlagBits.Acknowledgement)
                {
                    synAckOsFingerprints = this.synAckOsFingerprints;
                }
                else
                {
                    synAckOsFingerprints = this.synOsFingerprints;
                }
                byte originalTimeToLive = this.GetOriginalTimeToLive(packet, tcpPacket);
                foreach (P0fFingerprint fingerprint in synAckOsFingerprints)
                {
                    if (fingerprint.Matches(packet, tcpPacket, originalTimeToLive))
                    {
                        osList = new List<string>();
                        osList.Add(fingerprint.OS);
                        return true;
                    }
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
                return "p0f";
            }
        }

        internal class P0fFingerprint
        {
            private bool dontFragment;
            private byte initialTtl;
            private string optionValue;
            private string osDetails;
            private string osGenre;
            private string overallSynPacketSize;
            private string quirksList;
            private string windowSize;

            internal P0fFingerprint(string fingerprintString)
            {
                char[] separator = new char[] { ':' };
                string[] strArray = fingerprintString.Split(separator);
                this.windowSize = strArray[0];
                this.initialTtl = Convert.ToByte(strArray[1], 10);
                this.dontFragment = strArray[2].Equals("1");
                this.overallSynPacketSize = strArray[3];
                this.optionValue = strArray[4];
                this.quirksList = strArray[5];
                this.osGenre = strArray[6];
                this.osDetails = strArray[7];
            }

            internal bool Matches(IPv4Packet ipPacket, TcpPacket tcpPacket, byte originalTimeToLive)
            {
                if (this.windowSize.StartsWith("S"))
                {
                    int num = Convert.ToInt32(this.windowSize.Substring(1));
                    int num2 = 0;
                    foreach (KeyValuePair<TcpPacket.OptionKinds, byte[]> pair in tcpPacket.OptionList)
                    {
                        if (pair.Key.Equals(TcpPacket.OptionKinds.MaximumSegmentSize))
                        {
                            num2 = (int) ByteConverter.ToUInt32(pair.Value);
                        }
                    }
                    if (tcpPacket.WindowSize != (num * num2))
                    {
                        return false;
                    }
                }
                else if (this.windowSize.StartsWith("T"))
                {
                    int num3 = Convert.ToInt32(this.windowSize.Substring(1));
                    int num4 = 0x5dc;
                    foreach (KeyValuePair<TcpPacket.OptionKinds, byte[]> pair2 in tcpPacket.OptionList)
                    {
                        if (pair2.Key.Equals(TcpPacket.OptionKinds.MaximumSegmentSize))
                        {
                            num4 = ((int) ByteConverter.ToUInt32(pair2.Value)) + 40;
                        }
                    }
                    if (tcpPacket.WindowSize != (num3 * num4))
                    {
                        return false;
                    }
                }
                else if (this.windowSize.StartsWith("%"))
                {
                    int num5 = Convert.ToInt32(this.windowSize.Substring(1));
                    if ((tcpPacket.WindowSize % num5) != 0)
                    {
                        return false;
                    }
                }
                else if (!this.windowSize.StartsWith("*") && !this.windowSize.Equals(tcpPacket.WindowSize.ToString()))
                {
                    return false;
                }
                if (originalTimeToLive != this.initialTtl)
                {
                    return false;
                }
                if (ipPacket.DontFragmentFlag != this.dontFragment)
                {
                    return false;
                }
                if (!this.overallSynPacketSize.Equals("!") && !this.overallSynPacketSize.Equals(ipPacket.PacketByteCount.ToString()))
                {
                    return false;
                }
                char[] separator = new char[] { ',' };
                string[] strArray = this.optionValue.Split(separator);
                if (strArray.Length != tcpPacket.OptionList.Count)
                {
                    return false;
                }
                for (int i = 0; i < strArray.Length; i++)
                {
                    if (strArray[i].Equals("N"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair3 = tcpPacket.OptionList[i];
                        if (!pair3.Key.Equals(TcpPacket.OptionKinds.NoOperation))
                        {
                            return false;
                        }
                    }
                    else if (strArray[i].Equals("E"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair4 = tcpPacket.OptionList[i];
                        if (!pair4.Key.Equals(TcpPacket.OptionKinds.EndOfOptionList))
                        {
                            return false;
                        }
                    }
                    else if (strArray[i].StartsWith("W"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair5 = tcpPacket.OptionList[i];
                        if (!pair5.Key.Equals(TcpPacket.OptionKinds.WindowScaleFactor))
                        {
                            return false;
                        }
                        if (strArray[i][1] != '%')
                        {
                            if (strArray[i][1] != '*')
                            {
                                int num7;
                                if (!int.TryParse(strArray[i].Substring(1), out num7))
                                {
                                    return false;
                                }
                                KeyValuePair<TcpPacket.OptionKinds, byte[]> pair7 = tcpPacket.OptionList[i];
                                int num10 = (int) ByteConverter.ToUInt32(pair7.Value);
                                if (num7 != num10)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            KeyValuePair<TcpPacket.OptionKinds, byte[]> pair6 = tcpPacket.OptionList[i];
                            int num8 = (int) ByteConverter.ToUInt32(pair6.Value);
                            int num9 = Convert.ToInt32(strArray[i].Substring(2));
                            if ((num8 % num9) != 0)
                            {
                                return false;
                            }
                        }
                    }
                    else if (strArray[i].StartsWith("M"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair8 = tcpPacket.OptionList[i];
                        if (!pair8.Key.Equals(TcpPacket.OptionKinds.MaximumSegmentSize))
                        {
                            return false;
                        }
                        if (strArray[i][1] != '%')
                        {
                            if (strArray[i][1] != '*')
                            {
                                int num11;
                                if (!int.TryParse(strArray[i].Substring(1), out num11))
                                {
                                    return false;
                                }
                                KeyValuePair<TcpPacket.OptionKinds, byte[]> pair10 = tcpPacket.OptionList[i];
                                int num14 = (int) ByteConverter.ToUInt32(pair10.Value);
                                if (num11 != num14)
                                {
                                    return false;
                                }
                            }
                        }
                        else
                        {
                            KeyValuePair<TcpPacket.OptionKinds, byte[]> pair9 = tcpPacket.OptionList[i];
                            int num12 = (int) ByteConverter.ToUInt32(pair9.Value);
                            int num13 = Convert.ToInt32(strArray[i].Substring(2));
                            if ((num12 % num13) != 0)
                            {
                                return false;
                            }
                        }
                    }
                    else if (strArray[i].Equals("S"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair11 = tcpPacket.OptionList[i];
                        if (!pair11.Key.Equals(TcpPacket.OptionKinds.SackPermitted))
                        {
                            return false;
                        }
                    }
                    else if (strArray[i].Equals("K"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair12 = tcpPacket.OptionList[i];
                        if (!pair12.Key.Equals(TcpPacket.OptionKinds.Sack))
                        {
                            return false;
                        }
                    }
                    else if (strArray[i].Equals("T"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair13 = tcpPacket.OptionList[i];
                        if (!pair13.Key.Equals(TcpPacket.OptionKinds.Timestamp))
                        {
                            return false;
                        }
                    }
                    else if (strArray[i].Equals("T0"))
                    {
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair14 = tcpPacket.OptionList[i];
                        if (!pair14.Key.Equals(TcpPacket.OptionKinds.Timestamp))
                        {
                            return false;
                        }
                        KeyValuePair<TcpPacket.OptionKinds, byte[]> pair15 = tcpPacket.OptionList[i];
                        byte[] buffer = pair15.Value;
                        for (int j = 0; j < buffer.Length; j++)
                        {
                            if (buffer[j] != 0)
                            {
                                return false;
                            }
                        }
                    }
                    else
                    {
                        strArray[i].StartsWith("?");
                    }
                }
                return true;
            }

            public override string ToString()
            {
                return "P0f";
            }

            internal byte InitialTTL
            {
                get
                {
                    return this.initialTtl;
                }
            }

            internal string OS
            {
                get
                {
                    return (this.osGenre + " " + this.osDetails);
                }
            }

            internal string OsDetails
            {
                get
                {
                    return this.osDetails;
                }
            }

            internal string OsGenre
            {
                get
                {
                    return this.osGenre;
                }
            }
        }
    }
}

