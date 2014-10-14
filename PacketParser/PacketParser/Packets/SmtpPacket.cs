namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class SmtpPacket : AbstractPacket
    {
        private bool clientToServer;
        private List<KeyValuePair<int, string>> replyList;
        private List<KeyValuePair<string, string>> requestCommandAndArgumentList;

        internal SmtpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex, bool clientToServer) : base(parentFrame, packetStartIndex, packetEndIndex, "SMTP")
        {
            this.clientToServer = clientToServer;
            this.requestCommandAndArgumentList = new List<KeyValuePair<string, string>>();
            this.replyList = new List<KeyValuePair<int, string>>();
            if (!clientToServer)
            {
                int dataIndex = base.PacketStartIndex;
                while ((dataIndex < packetEndIndex) && (this.replyList.Count < 0x3e8))
                {
                    string str4;
                    int num3;
                    string str5 = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
                    if (!int.TryParse(str5.Substring(0, 3), out num3))
                    {
                        return;
                    }
                    if (str5.Length > 4)
                    {
                        str4 = str5.Substring(4);
                    }
                    else
                    {
                        str4 = "";
                    }
                    this.replyList.Add(new KeyValuePair<int, string>(num3, str4));
                }
            }
            else
            {
                int num = base.PacketStartIndex;
                while ((num < packetEndIndex) && (this.requestCommandAndArgumentList.Count < 0x3e8))
                {
                    string str = ByteConverter.ReadLine(parentFrame.Data, ref num);
                    string key = null;
                    string str3 = null;
                    if (str.Contains(" "))
                    {
                        key = str.Substring(0, str.IndexOf(' '));
                        if (str.Length > (str.IndexOf(' ') + 1))
                        {
                            str3 = str.Substring(str.IndexOf(' ') + 1);
                        }
                        else
                        {
                            str3 = "";
                        }
                    }
                    else if (str.Length == 4)
                    {
                        key = str;
                        str3 = "";
                    }
                    if (key == null)
                    {
                        return;
                    }
                    KeyValuePair<string, string> item = new KeyValuePair<string, string>(key, str3);
                    this.requestCommandAndArgumentList.Add(item);
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

        internal string ReadLine()
        {
            int packetStartIndex = base.PacketStartIndex;
            return ByteConverter.ReadLine(base.ParentFrame.Data, ref packetStartIndex);
        }

        internal bool ClientToServer
        {
            get
            {
                return this.clientToServer;
            }
        }

        internal IEnumerable<KeyValuePair<int, string>> Replies
        {
            get
            {
                return this.replyList;
            }
        }

        internal List<int> ReplyCodes
        {
            get
            {
                List<int> list = new List<int>();
                foreach (KeyValuePair<int, string> pair in this.Replies)
                {
                    list.Add(pair.Key);
                }
                return list;
            }
        }

        internal IEnumerable<KeyValuePair<string, string>> RequestCommandsAndArguments
        {
            get
            {
                return this.requestCommandAndArgumentList;
            }
        }


        public enum ClientCommands
        {
            HELO,
            MAIL,
            RCPT,
            DATA,
            RSET,
            SEND,
            SOML,
            SAML,
            VRFY,
            EXPN,
            HELP,
            NOOP,
            QUIT,
            TURN,
            EHLO,
            AUTH
        }
    }
}

