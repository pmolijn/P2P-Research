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
    using System.Globalization;
    using System.IO;

    internal class HttpPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public HttpPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            bool flag = false;
            HttpPacket httpPacket = null;
            TcpPacket tcpPacket = null;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(HttpPacket))
                {
                    httpPacket = (HttpPacket) packet3;
                }
                else if (packet3.GetType() == typeof(TcpPacket))
                {
                    tcpPacket = (TcpPacket) packet3;
                }
            }
            if ((((httpPacket != null) && (tcpPacket != null)) && httpPacket.PacketHeaderIsComplete) && (((httpPacket.RequestMethod != HttpPacket.RequestMethods.POST) || (httpPacket.ContentLength > 0x1000)) || httpPacket.ContentIsComplete()))
            {
                flag = this.ExtractHttpData(httpPacket, tcpPacket, sourceHost, destinationHost, base.MainPacketHandler);
            }
            if (flag)
            {
                return tcpPacket.PayloadDataLength;
            }
            return 0;
        }

        private bool ExtractHttpData(HttpPacket httpPacket, TcpPacket tcpPacket, NetworkHost sourceHost, NetworkHost destinationHost, PacketHandler mainPacketHandler)
        {
            if (httpPacket.MessageTypeIsRequest)
            {
                NameValueCollection parameters = null;
                if ((httpPacket.UserAgentBanner != null) && (httpPacket.UserAgentBanner.Length > 0))
                {
                    sourceHost.AddHttpUserAgentBanner(httpPacket.UserAgentBanner);
                }
                if ((httpPacket.RequestedHost != null) && (httpPacket.RequestedHost.Length > 0))
                {
                    destinationHost.AddHostName(httpPacket.RequestedHost);
                }
                if (httpPacket.Cookie != null)
                {
                    parameters = new NameValueCollection();
                    char[] separator = new char[] { ';', ',' };
                    foreach (string str in httpPacket.Cookie.Split(separator))
                    {
                        string name = str.Trim();
                        int index = name.IndexOf('=');
                        if (index > 0)
                        {
                            parameters.Add(name.Substring(0, index), name.Substring(index + 1));
                        }
                        else
                        {
                            parameters.Add(name, "");
                        }
                    }
                    NetworkCredential credential = NetworkCredential.GetNetworkCredential(parameters, sourceHost, destinationHost, "HTTP Cookie parameter", httpPacket.ParentFrame.Timestamp);
                    if (credential != null)
                    {
                        mainPacketHandler.AddCredential(credential);
                    }
                    mainPacketHandler.OnParametersDetected(new ParametersEventArgs(tcpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + tcpPacket.SourcePort, "TCP " + tcpPacket.DestinationPort, parameters, httpPacket.ParentFrame.Timestamp, "HTTP Cookie"));
                    NetworkCredential credential2 = new NetworkCredential(sourceHost, destinationHost, "HTTP Cookie", httpPacket.Cookie, "N/A", httpPacket.ParentFrame.Timestamp);
                    mainPacketHandler.AddCredential(credential2);
                }
                if (httpPacket.AuthorizationCredentialsUsername != null)
                {
                    NetworkCredential credential3 = new NetworkCredential(sourceHost, destinationHost, httpPacket.PacketTypeDescription, httpPacket.AuthorizationCredentialsUsername, httpPacket.AuthorizationCredentialsPassword, httpPacket.ParentFrame.Timestamp);
                    mainPacketHandler.AddCredential(credential3);
                }
                if ((httpPacket.HeaderFields != null) && (httpPacket.HeaderFields.Count > 0))
                {
                    SortedList<string, string> list = new SortedList<string, string>();
                    list.Add("accept", null);
                    list.Add("connection", null);
                    list.Add("accept-language", null);
                    list.Add("accept-encoding", null);
                    NameValueCollection values2 = new NameValueCollection();
                    foreach (string str3 in httpPacket.HeaderFields)
                    {
                        int length = str3.IndexOf(':');
                        if ((length > 0) && (length < str3.Length))
                        {
                            string str4 = str3.Substring(0, length).Trim();
                            if (!list.ContainsKey(str4.ToLower()))
                            {
                                values2.Add(str4, str3.Substring(length + 1).Trim());
                            }
                        }
                    }
                    base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(httpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + tcpPacket.SourcePort, "TCP " + tcpPacket.DestinationPort, values2, httpPacket.ParentFrame.Timestamp, "HTTP Header"));
                    foreach (string str5 in values2.Keys)
                    {
                        if (str5.StartsWith("X-", StringComparison.InvariantCultureIgnoreCase))
                        {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + str5, values2[str5]);
                        }
                        else if (str5.StartsWith("HTTP_X", StringComparison.InvariantCultureIgnoreCase))
                        {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + str5, values2[str5]);
                        }
                        else if (str5.StartsWith("X_", StringComparison.InvariantCultureIgnoreCase))
                        {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + str5, values2[str5]);
                        }
                        else if (str5.StartsWith("HTTP_MSISDN", StringComparison.InvariantCultureIgnoreCase))
                        {
                            sourceHost.AddNumberedExtraDetail("HTTP header: " + str5, values2[str5]);
                        }
                    }
                }
                if (((httpPacket.RequestMethod == HttpPacket.RequestMethods.GET) || (httpPacket.RequestMethod == HttpPacket.RequestMethods.POST)) && (httpPacket.RequestedFileName != null))
                {
                    string str9;
                    NameValueCollection querystringData = httpPacket.GetQuerystringData();
                    if ((querystringData != null) && (querystringData.Count > 0))
                    {
                        mainPacketHandler.OnParametersDetected(new ParametersEventArgs(tcpPacket.ParentFrame.FrameNumber, sourceHost, destinationHost, "TCP " + tcpPacket.SourcePort, "TCP " + tcpPacket.DestinationPort, querystringData, tcpPacket.ParentFrame.Timestamp, "HTTP QueryString"));
                        NetworkCredential credential4 = NetworkCredential.GetNetworkCredential(querystringData, sourceHost, destinationHost, "HTTP GET QueryString", tcpPacket.ParentFrame.Timestamp);
                        if (credential4 != null)
                        {
                            mainPacketHandler.AddCredential(credential4);
                        }
                        if (querystringData.HasKeys())
                        {
                            Dictionary<string, string> dictionary = new Dictionary<string, string>();
                            foreach (string str6 in querystringData.AllKeys)
                            {
                                dictionary.Add(str6, querystringData[str6]);
                            }
                            if (dictionary.ContainsKey("utmsr"))
                            {
                                sourceHost.AddNumberedExtraDetail("Screen resolution (Google Analytics)", dictionary["utmsr"]);
                            }
                            if (dictionary.ContainsKey("utmsc"))
                            {
                                sourceHost.AddNumberedExtraDetail("Color depth (Google Analytics)", dictionary["utmsc"]);
                            }
                            if (dictionary.ContainsKey("utmul"))
                            {
                                sourceHost.AddNumberedExtraDetail("Browser language (Google Analytics)", dictionary["utmul"]);
                            }
                            if (dictionary.ContainsKey("utmfl"))
                            {
                                sourceHost.AddNumberedExtraDetail("Flash version (Google Analytics)", dictionary["utmfl"]);
                            }
                            if (((httpPacket.RequestMethod == HttpPacket.RequestMethods.POST) && dictionary.ContainsKey("a")) && (dictionary["a"].Equals("SendMessage") && !httpPacket.ContentIsComplete()))
                            {
                                return false;
                            }
                        }
                    }
                    string requestedFileName = httpPacket.RequestedFileName;
                    string str8 = null;
                    if (requestedFileName.Contains("?"))
                    {
                        if ((requestedFileName.IndexOf('?') + 1) < requestedFileName.Length)
                        {
                            str8 = requestedFileName.Substring(requestedFileName.IndexOf('?') + 1);
                        }
                        requestedFileName = requestedFileName.Substring(0, requestedFileName.IndexOf('?'));
                    }
                    if (requestedFileName.StartsWith("http://"))
                    {
                        requestedFileName = requestedFileName.Substring(7);
                    }
                    if (requestedFileName.StartsWith("www.") && requestedFileName.Contains("/"))
                    {
                        requestedFileName = requestedFileName.Substring(requestedFileName.IndexOf("/"));
                    }
                    char[] destinationArray = new char[Path.GetInvalidPathChars().Length + 1];
                    Array.Copy(Path.GetInvalidPathChars(), destinationArray, Path.GetInvalidPathChars().Length);
                    destinationArray[destinationArray.Length - 1] = '/';
                    string[] strArray = requestedFileName.Split(destinationArray);
                    string fileLocation = "";
                    if (requestedFileName.EndsWith("/"))
                    {
                        str9 = "index.html";
                        for (int i = 0; i < strArray.Length; i++)
                        {
                            if ((strArray[i].Length > 0) && !strArray[i].Contains(".."))
                            {
                                fileLocation = fileLocation + "/" + strArray[i];
                            }
                        }
                    }
                    else
                    {
                        str9 = strArray[strArray.Length - 1];
                        for (int j = 0; j < (strArray.Length - 1); j++)
                        {
                            if ((strArray[j].Length > 0) && !strArray[j].Contains(".."))
                            {
                                fileLocation = fileLocation + "/" + strArray[j];
                            }
                        }
                    }
                    if ((str8 != null) && (str8.Length > 0))
                    {
                        str9 = str9 + "." + str8.GetHashCode().ToString("X4");
                    }
                    try
                    {
                        if (mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, true))
                        {
                            FileStreamAssembler assembler = mainPacketHandler.FileStreamAssemblerList.GetAssembler(destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, true);
                            mainPacketHandler.FileStreamAssemblerList.Remove(assembler, true);
                        }
                        string details = httpPacket.RequestedFileName;
                        if (((httpPacket.RequestedHost != null) && (httpPacket.RequestedHost.Length > 0)) && ((httpPacket.RequestedFileName != null) && httpPacket.RequestedFileName.StartsWith("/")))
                        {
                            details = httpPacket.RequestedHost + httpPacket.RequestedFileName;
                        }
                        FileStreamAssembler assembler2 = new FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, destinationHost, tcpPacket.DestinationPort, sourceHost, tcpPacket.SourcePort, tcpPacket != null, FileStreamTypes.HttpGetNormal, str9, fileLocation, details, httpPacket.ParentFrame.FrameNumber, httpPacket.ParentFrame.Timestamp);
                        mainPacketHandler.FileStreamAssemblerList.Add(assembler2);
                    }
                    catch (Exception exception)
                    {
                        mainPacketHandler.OnAnomalyDetected("Error creating assembler for HTTP file transfer: " + exception.Message);
                    }
                    if (httpPacket.RequestMethod == HttpPacket.RequestMethods.POST)
                    {
                        if ((httpPacket.ContentType != null) && httpPacket.ContentType.StartsWith("multipart/form-data"))
                        {
                            FileStreamAssembler assembler3 = null;
                            try
                            {
                                string str12;
                                if (mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true))
                                {
                                    FileStreamAssembler assembler4 = mainPacketHandler.FileStreamAssemblerList.GetAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true);
                                    mainPacketHandler.FileStreamAssemblerList.Remove(assembler4, true);
                                }
                                if (httpPacket.ContentType.ToLower(CultureInfo.InvariantCulture).StartsWith("multipart/form-data; boundary=") && (httpPacket.ContentType.Length > 30))
                                {
                                    str12 = httpPacket.ContentType.Substring(30);
                                }
                                else
                                {
                                    str12 = "";
                                }
                                assembler3 = new FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, tcpPacket != null, FileStreamTypes.HttpPostMimeMultipartFormData, str9 + ".form-data.mime", fileLocation, str12, httpPacket.ParentFrame.FrameNumber, httpPacket.ParentFrame.Timestamp) {
                                    FileContentLength = httpPacket.ContentLength,
                                    FileSegmentRemainingBytes = httpPacket.ContentLength
                                };
                                mainPacketHandler.FileStreamAssemblerList.Add(assembler3);
                                if ((assembler3.TryActivate() && (httpPacket.MessageBody != null)) && (httpPacket.MessageBody.Length > 0))
                                {
                                    assembler3.AddData(httpPacket.MessageBody, tcpPacket.SequenceNumber);
                                }
                                goto Label_0F29;
                            }
                            catch (Exception exception2)
                            {
                                if (assembler3 != null)
                                {
                                    assembler3.Clear();
                                }
                                mainPacketHandler.OnAnomalyDetected("Error creating assembler for HTTP file transfer: " + exception2.Message);
                                goto Label_0F29;
                            }
                        }
                        List<MultipartPart> formData = httpPacket.GetFormData();
                        if (formData != null)
                        {
                            foreach (MultipartPart part in formData)
                            {
                                if (((part.Attributes["requests"] != null) && (httpPacket.GetQuerystringData() != null)) && (httpPacket.GetQuerystringData()["a"] == "SendMessage"))
                                {
                                    string str13 = part.Attributes["requests"];
                                    if (str13.StartsWith("[{") && str13.EndsWith("}]"))
                                    {
                                        str13 = str13.Substring(2, str13.Length - 4);
                                    }
                                    int startIndex = -1;
                                    int num6 = -1;
                                    while (num6 < (str13.Length - 2))
                                    {
                                        if (num6 > 0)
                                        {
                                            startIndex = str13.IndexOf(',', num6) + 1;
                                        }
                                        else
                                        {
                                            startIndex = 0;
                                        }
                                        if (str13[startIndex] == '"')
                                        {
                                            startIndex = str13.IndexOf('"', startIndex) + 1;
                                            num6 = str13.IndexOf('"', startIndex);
                                            while (str13[num6 - 1] == '\\')
                                            {
                                                num6 = str13.IndexOf('"', num6 + 1);
                                            }
                                        }
                                        else
                                        {
                                            num6 = str13.IndexOf(':', startIndex);
                                        }
                                        string str14 = str13.Substring(startIndex, num6 - startIndex);
                                        startIndex = str13.IndexOf(':', num6) + 1;
                                        if (str13[startIndex] == '"')
                                        {
                                            startIndex = str13.IndexOf('"', startIndex) + 1;
                                            num6 = str13.IndexOf('"', startIndex);
                                            while (str13[num6 - 1] == '\\')
                                            {
                                                num6 = str13.IndexOf('"', num6 + 1);
                                            }
                                        }
                                        else if (str13.IndexOf(',', startIndex) > 0)
                                        {
                                            num6 = str13.IndexOf(',', startIndex);
                                        }
                                        else
                                        {
                                            num6 = str13.Length;
                                        }
                                        string str15 = str13.Substring(startIndex, num6 - startIndex);
                                        str13 = str13.Replace(@"\n", Environment.NewLine).Replace(@"\r", "\r").Replace(@"\t", "\t");
                                        part.Attributes.Add(str14, str15);
                                    }
                                }
                            }
                            base.MainPacketHandler.ExtractMultipartFormData(formData, sourceHost, destinationHost, tcpPacket.ParentFrame.Timestamp, httpPacket.ParentFrame.FrameNumber, "TCP " + tcpPacket.SourcePort, "TCP " + tcpPacket.DestinationPort, ApplicationLayerProtocol.Http, parameters);
                        }
                    }
                }
            }
            else
            {
                if ((httpPacket.ServerBanner != null) && (httpPacket.ServerBanner.Length > 0))
                {
                    sourceHost.AddHttpServerBanner(httpPacket.ServerBanner, tcpPacket.SourcePort);
                }
                if ((httpPacket.WwwAuthenticateRealm != null) && (httpPacket.WwwAuthenticateRealm.Length > 0))
                {
                    sourceHost.AddHostName(httpPacket.WwwAuthenticateRealm);
                    sourceHost.ExtraDetailsList["WWW-Authenticate realm"] = httpPacket.WwwAuthenticateRealm;
                }
                if (mainPacketHandler.FileStreamAssemblerList.ContainsAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true))
                {
                    FileStreamAssembler assembler5 = mainPacketHandler.FileStreamAssemblerList.GetAssembler(sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true);
                    if ((httpPacket.ContentLength >= 0) || (httpPacket.ContentLength == -1))
                    {
                        assembler5.FileContentLength = httpPacket.ContentLength;
                        assembler5.FileSegmentRemainingBytes = httpPacket.ContentLength;
                    }
                    if (httpPacket.ContentLength == 0)
                    {
                        mainPacketHandler.FileStreamAssemblerList.Remove(assembler5, true);
                    }
                    else
                    {
                        if (httpPacket.ContentDispositionFilename != null)
                        {
                            assembler5.Filename = httpPacket.ContentDispositionFilename;
                        }
                        if (((httpPacket.ContentType != null) && httpPacket.ContentType.Contains("/")) && (httpPacket.ContentType.IndexOf('/') < (httpPacket.ContentType.Length - 1)))
                        {
                            string extension = StringManglerUtil.GetExtension(httpPacket.ContentType);
                            if ((((extension.Length > 0) && !assembler5.Filename.EndsWith("." + extension, StringComparison.InvariantCultureIgnoreCase)) && (!assembler5.Filename.EndsWith("jpg", StringComparison.InvariantCultureIgnoreCase) || !extension.Equals("jpeg", StringComparison.InvariantCultureIgnoreCase))) && (!assembler5.Filename.EndsWith("htm", StringComparison.InvariantCultureIgnoreCase) || !extension.Equals("html", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                assembler5.Filename = assembler5.Filename + "." + extension;
                            }
                        }
                        if (httpPacket.TransferEncoding == "chunked")
                        {
                            assembler5.FileStreamType = FileStreamTypes.HttpGetChunked;
                        }
                        if ((httpPacket.ContentEncoding != null) && (httpPacket.ContentEncoding.Length > 0))
                        {
                            if (httpPacket.ContentEncoding.Equals("gzip"))
                            {
                                assembler5.ContentEncoding = HttpPacket.ContentEncodings.Gzip;
                            }
                            else if (httpPacket.ContentEncoding.Equals("deflate"))
                            {
                                assembler5.ContentEncoding = HttpPacket.ContentEncodings.Deflate;
                            }
                        }
                        if (((assembler5.TryActivate() && (httpPacket.MessageBody != null)) && (httpPacket.MessageBody.Length > 0)) && (((assembler5.FileStreamType == FileStreamTypes.HttpGetChunked) || (httpPacket.MessageBody.Length <= assembler5.FileSegmentRemainingBytes)) || (assembler5.FileSegmentRemainingBytes == -1)))
                        {
                            assembler5.AddData(httpPacket.MessageBody, tcpPacket.SequenceNumber);
                        }
                    }
                }
            }
        Label_0F29:
            return true;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.Http;
            }
        }
    }
}

