namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class SipPacket : AbstractPacket
    {
        private string callId;
        private string contact;
        private int contentLength;
        private string contentType;
        private string from;
        private string messageLine;
        private string to;

        internal SipPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "SIP")
        {
            int dataIndex = base.PacketStartIndex;
            this.messageLine = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Message Line", this.messageLine);
            }
            string str = "dummy value";
            NameValueCollection c = new NameValueCollection();
            while ((dataIndex < base.PacketEndIndex) && (str.Length > 0))
            {
                str = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
                if (str.Contains(":"))
                {
                    string str2 = str.Substring(0, str.IndexOf(':'));
                    string s = str.Substring(str.IndexOf(':') + 1).Trim();
                    if ((str2.Length > 0) && (s.Length > 0))
                    {
                        c[str2] = s;
                        switch (str2)
                        {
                            case "To":
                            case "t":
                            {
                                this.to = s;
                                continue;
                            }
                            case "From":
                            case "f":
                            {
                                this.from = s;
                                continue;
                            }
                            case "Call-ID":
                            {
                                this.callId = s;
                                continue;
                            }
                            case "Contact":
                            {
                                this.contact = s;
                                continue;
                            }
                            case "Content-Type":
                            case "c":
                            {
                                this.contentType = s;
                                continue;
                            }
                            case "Content-Length":
                            case "l":
                                int.TryParse(s, out this.contentLength);
                                break;
                        }
                    }
                }
            }
            base.Attributes.Add(c);
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        internal string From
        {
            get
            {
                return this.from;
            }
        }

        internal string MessageLine
        {
            get
            {
                return this.messageLine;
            }
        }

        internal string To
        {
            get
            {
                return this.to;
            }
        }
    }
}

