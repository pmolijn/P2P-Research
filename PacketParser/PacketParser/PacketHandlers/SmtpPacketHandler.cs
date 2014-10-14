namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.FileTransfer;
    using PacketParser.Mime;
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;

    internal class SmtpPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        private PopularityList<NetworkTcpSession, SmtpSession> smtpSessionList;

        public SmtpPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.smtpSessionList = new PopularityList<NetworkTcpSession, SmtpSession>(100);
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            SmtpSession session;
            if (this.smtpSessionList.ContainsKey(tcpSession))
            {
                session = this.smtpSessionList[tcpSession];
            }
            else
            {
                session = new SmtpSession();
                this.smtpSessionList.Add(tcpSession, session);
            }
            TcpPacket packet = null;
            SmtpPacket packet2 = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(TcpPacket))
                {
                    packet = (TcpPacket) packet3;
                }
                else if (packet3.GetType() == typeof(SmtpPacket))
                {
                    packet2 = (SmtpPacket) packet3;
                }
            }
            if (packet2 == null)
            {
                return 0;
            }
            if (packet2.ClientToServer)
            {
                if (session.State == SmtpSession.SmtpState.Username)
                {
                    string s = packet2.ReadLine().Trim();
                    try
                    {
                        byte[] bytes = Convert.FromBase64String(s);
                        session.Username = System.Text.Encoding.ASCII.GetString(bytes);
                        goto Label_092F;
                    }
                    catch (FormatException)
                    {
                        goto Label_092F;
                    }
                }
                if (session.State == SmtpSession.SmtpState.Password)
                {
                    string str2 = packet2.ReadLine().Trim();
                    try
                    {
                        byte[] buffer2 = Convert.FromBase64String(str2);
                        session.Password = System.Text.Encoding.ASCII.GetString(buffer2);
                        goto Label_092F;
                    }
                    catch (FormatException)
                    {
                        goto Label_092F;
                    }
                }
                if (session.State == SmtpSession.SmtpState.Data)
                {
                    session.AddData(packet2.ParentFrame.Data, packet2.PacketStartIndex, packet2.PacketLength);
                    if (session.State == SmtpSession.SmtpState.Footer)
                    {
                        UnbufferedReader streamReader = new UnbufferedReader(session.DataStream);
                        string from = null;
                        string to = null;
                        string subject = null;
                        string anyString = null;
                        NameValueCollection c = null;
                        foreach (MultipartPart part in PartBuilder.GetParts(streamReader))
                        {
                            if (c == null)
                            {
                                from = part.Attributes["From"];
                                to = part.Attributes["To"];
                                subject = part.Attributes["Subject"];
                                anyString = part.Attributes["Message-ID"];
                                c = part.Attributes;
                            }
                            base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(packet2.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + packet.SourcePort, "TCP " + packet.DestinationPort, part.Attributes, packet.ParentFrame.Timestamp, "SMTP packet"));
                            string contentType = part.Attributes["Content-Type"];
                            string name = part.Attributes["charset"];
                            System.Text.Encoding encoding = null;
                            if ((name != null) && (name.Length > 0))
                            {
                                try
                                {
                                    encoding = System.Text.Encoding.GetEncoding(name);
                                }
                                catch
                                {
                                }
                            }
                            bool flag = false;
                            string str9 = part.Attributes["Content-Disposition"];
                            if ((str9 != null) && str9.Contains("attachment"))
                            {
                                flag = true;
                            }
                            if ((!flag && (contentType == null)) || contentType.Equals("text/plain", StringComparison.InvariantCultureIgnoreCase))
                            {
                                byte[] data = null;
                                if (part.Attributes["Content-Transfer-Encoding"] == "quoted-printable")
                                {
                                    data = ByteConverter.ReadQuotedPrintable(part.Data).ToArray();
                                }
                                else if (part.Attributes["Content-Transfer-Encoding"] == "base64")
                                {
                                    data = Convert.FromBase64String(ByteConverter.ReadString(part.Data));
                                }
                                else
                                {
                                    data = part.Data;
                                }
                                string message = null;
                                if (encoding == null)
                                {
                                    message = ByteConverter.ReadString(data);
                                }
                                else
                                {
                                    message = encoding.GetString(data);
                                }
                                if (message != null)
                                {
                                    NameValueCollection attributes = new NameValueCollection();
                                    attributes.Add(c);
                                    attributes.Add(part.Attributes);
                                    base.MainPacketHandler.OnMessageDetected(new MessageEventArgs(ApplicationLayerProtocol.Smtp, sourceHost, destinationHost, packet2.ParentFrame.FrameNumber, packet2.ParentFrame.Timestamp, from, to, subject, message, attributes));
                                }
                                continue;
                            }
                            string filename = part.Attributes["name"];
                            if ((filename == null) || (filename.Length == 0))
                            {
                                filename = part.Attributes["filename"];
                            }
                            if ((filename == null) || (filename.Length == 0))
                            {
                                if ((subject != null) && (subject.Length > 3))
                                {
                                    filename = StringManglerUtil.ConvertToFilename(subject, 10);
                                }
                                else if ((anyString != null) && (anyString.Length > 3))
                                {
                                    filename = StringManglerUtil.ConvertToFilename(anyString, 10);
                                }
                                if ((filename == null) || (filename.Length < 3))
                                {
                                    filename = "email_" + (part.GetHashCode() % 0x3e8);
                                }
                                string extension = StringManglerUtil.GetExtension(contentType);
                                if ((extension == null) || (extension.Length < 1))
                                {
                                    extension = "dat";
                                }
                                filename = filename + "." + extension;
                            }
                            List<byte> list = new List<byte>();
                            if (part.Attributes["Content-Transfer-Encoding"] == "base64")
                            {
                                int dataIndex = 0;
                                while (dataIndex < part.Data.Length)
                                {
                                    string str13 = ByteConverter.ReadLine(part.Data, ref dataIndex);
                                    if ((str13 == null) && (dataIndex < part.Data.Length))
                                    {
                                        str13 = ByteConverter.ReadString(part.Data, dataIndex, part.Data.Length - dataIndex, false, false);
                                        dataIndex = part.Data.Length;
                                    }
                                    try
                                    {
                                        list.AddRange(Convert.FromBase64String(str13));
                                        continue;
                                    }
                                    catch (FormatException)
                                    {
                                        continue;
                                    }
                                }
                            }
                            else if (part.Attributes["Content-Transfer-Encoding"] == "quoted-printable")
                            {
                                list = ByteConverter.ReadQuotedPrintable(part.Data);
                            }
                            else
                            {
                                list.AddRange(part.Data);
                            }
                            if ((list != null) && (list.Count > 0))
                            {
                                FileStreamAssembler assembler = new FileStreamAssembler(base.MainPacketHandler.FileStreamAssemblerList, sourceHost, packet.SourcePort, destinationHost, packet.DestinationPort, true, FileStreamTypes.SMTP, filename, "/", list.Count, list.Count, "E-mail From: " + from + " To: " + to + " Subject: " + subject, filename, packet.ParentFrame.FrameNumber, packet.ParentFrame.Timestamp);
                                if (assembler.TryActivate())
                                {
                                    assembler.AddData(list.ToArray(), packet.SequenceNumber);
                                }
                                else
                                {
                                    assembler.Clear();
                                    assembler.FinishAssembling();
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (KeyValuePair<string, string> pair in packet2.RequestCommandsAndArguments)
                    {
                        if (pair.Key.Equals(SmtpPacket.ClientCommands.HELO.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            string local1 = pair.Value;
                        }
                        else if (pair.Key.Equals(SmtpPacket.ClientCommands.EHLO.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            string local2 = pair.Value;
                        }
                        else if (pair.Key.Equals(SmtpPacket.ClientCommands.AUTH.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (pair.Value.ToUpper().Contains("LOGIN"))
                            {
                                session.State = SmtpSession.SmtpState.AuthLogin;
                            }
                        }
                        else if (pair.Key.Equals(SmtpPacket.ClientCommands.MAIL.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (pair.Value.StartsWith("FROM", StringComparison.InvariantCultureIgnoreCase))
                            {
                                int index = pair.Value.IndexOf(':');
                                if ((index > 0) && (pair.Value.Length > (index + 1)))
                                {
                                    session.MailFrom = pair.Value.Substring(index + 1).Trim();
                                }
                            }
                        }
                        else if (pair.Key.Equals(SmtpPacket.ClientCommands.RCPT.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            if (pair.Value.StartsWith("TO", StringComparison.InvariantCultureIgnoreCase))
                            {
                                int num3 = pair.Value.IndexOf(':');
                                if ((num3 > 0) && (pair.Value.Length > (num3 + 1)))
                                {
                                    session.AddRecipient(pair.Value.Substring(num3 + 1).Trim());
                                }
                            }
                        }
                        else if (pair.Key.Equals(SmtpPacket.ClientCommands.DATA.ToString(), StringComparison.InvariantCultureIgnoreCase))
                        {
                            session.State = SmtpSession.SmtpState.Data;
                        }
                    }
                }
            }
            else
            {
                foreach (KeyValuePair<int, string> pair2 in packet2.Replies)
                {
                    if (pair2.Key == 0x14e)
                    {
                        if (pair2.Value.Equals("VXNlcm5hbWU6"))
                        {
                            session.State = SmtpSession.SmtpState.Username;
                        }
                        else if (pair2.Value.Equals("UGFzc3dvcmQ6"))
                        {
                            session.State = SmtpSession.SmtpState.Password;
                        }
                    }
                    else if (pair2.Key == 0xeb)
                    {
                        base.MainPacketHandler.AddCredential(new NetworkCredential(tcpSession.ClientHost, tcpSession.ServerHost, packet2.PacketTypeDescription, session.Username, session.Password, packet2.ParentFrame.Timestamp));
                        session.State = SmtpSession.SmtpState.Authenticated;
                    }
                    else if (pair2.Key >= 500)
                    {
                        session.State = SmtpSession.SmtpState.None;
                    }
                    else if (pair2.Key == 0x162)
                    {
                        session.State = SmtpSession.SmtpState.Data;
                    }
                    else if (pair2.Key == 250)
                    {
                        session.State = SmtpSession.SmtpState.None;
                    }
                }
            }
        Label_092F:
            return packet.PayloadDataLength;
        }

        public void Reset()
        {
            this.smtpSessionList.Clear();
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.Smtp;
            }
        }

        internal class SmtpSession
        {
            private ASCIIEncoding asciiEncoding = new ASCIIEncoding();
            private static readonly byte[] DATA_TERMINATOR = new byte[] { 13, 10, 0x2e, 13, 10 };
            private MemoryStream dataStream = new MemoryStream();
            private string mailFrom;
            private string password;
            private List<string> rcptTo = new List<string>();
            private SmtpState state = SmtpState.None;
            private string username;

            internal SmtpSession()
            {
            }

            internal void AddData(string dataString)
            {
                byte[] bytes = this.asciiEncoding.GetBytes(dataString);
                this.AddData(bytes, 0, bytes.Length);
            }

            internal void AddData(byte[] buffer, int offset, int count)
            {
                List<byte> list;
                long num = KnuthMorrisPratt.ReadTo(DATA_TERMINATOR, buffer, offset, out list);
                if ((num == -1L) && (this.dataStream.Length > 0L))
                {
                    int num2 = Math.Min(DATA_TERMINATOR.Length - 1, (int) this.dataStream.Length);
                    byte[] buffer2 = new byte[num2];
                    this.dataStream.Seek(this.dataStream.Length - num2, SeekOrigin.Begin);
                    int length = this.dataStream.Read(buffer2, 0, num2);
                    byte[] destinationArray = new byte[(length + buffer.Length) - offset];
                    Array.Copy(buffer2, 0, destinationArray, 0, length);
                    Array.Copy(buffer, offset, destinationArray, length, buffer.Length - offset);
                    long num4 = KnuthMorrisPratt.ReadTo(DATA_TERMINATOR, destinationArray, 0, out list);
                    if (num4 >= 0L)
                    {
                        count = (((int) num4) - length) + 2;
                        this.state = SmtpState.Footer;
                    }
                }
                else if (num >= 0L)
                {
                    count = (((int) num) - offset) + 2;
                    this.state = SmtpState.Footer;
                }
                if (count > 0)
                {
                    this.dataStream.Seek(0L, SeekOrigin.End);
                    this.dataStream.Write(buffer, offset, count);
                }
            }

            internal void AddRecipient(string rcptTo)
            {
                this.rcptTo.Add(rcptTo);
            }

            internal MemoryStream DataStream
            {
                get
                {
                    return this.dataStream;
                }
            }

            internal string MailFrom
            {
                get
                {
                    return this.mailFrom;
                }
                set
                {
                    this.mailFrom = value;
                }
            }

            internal string Password
            {
                get
                {
                    return this.password;
                }
                set
                {
                    this.password = value;
                }
            }

            internal IEnumerable<string> RcptTo
            {
                get
                {
                    return this.rcptTo;
                }
            }

            internal SmtpState State
            {
                get
                {
                    return this.state;
                }
                set
                {
                    this.state = value;
                }
            }

            internal string Username
            {
                get
                {
                    return this.username;
                }
                set
                {
                    this.username = value;
                }
            }

            internal enum SmtpState
            {
                None,
                AuthLogin,
                Username,
                Password,
                Authenticated,
                Data,
                Footer
            }
        }
    }
}

