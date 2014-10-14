namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Mime;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Web;

    internal class HttpPacket : AbstractPacket, ISessionPacket
    {
        private string authorizationCredentailsPassword;
        private string authorizationCredentialsUsername;
        private string contentDispositionFilename;
        private string contentEncoding;
        private int contentLength;
        private string contentType;
        private string cookie;
        private List<string> headerFields;
        private byte[] messageBody;
        private bool messageTypeIsRequest;
        private bool packetHeaderIsComplete;
        private string requestedFileName;
        private string requestedHost;
        private RequestMethods requestMethod;
        private string serverBanner;
        private string statusCode;
        private string transferEncoding;
        private string userAgentBanner;
        private string wwwAuthenticateRealm;

        private HttpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "HTTP")
        {
            string str2;
            this.headerFields = new List<string>();
            this.requestedHost = null;
            this.requestedFileName = null;
            this.userAgentBanner = null;
            this.statusCode = null;
            this.serverBanner = null;
            this.contentType = null;
            this.contentLength = -1;
            this.contentEncoding = null;
            this.cookie = null;
            this.transferEncoding = null;
            this.wwwAuthenticateRealm = null;
            this.authorizationCredentialsUsername = null;
            this.authorizationCredentailsPassword = null;
            this.packetHeaderIsComplete = false;
            this.contentDispositionFilename = null;
            int dataIndex = packetStartIndex;
            string str = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
            if (str == null)
            {
                throw new Exception("HTTP packet does not contain a valid start line. Probably a false HTTP positive");
            }
            if (str.Length > 0x800)
            {
                throw new Exception("HTTP start line is longer than 255 bytes. Probably a false HTTP positive");
            }
            if (dataIndex > packetEndIndex)
            {
                throw new Exception("HTTP start line ends after packet end...");
            }
            if (str.StartsWith("GET"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.GET;
            }
            else if (str.StartsWith("HEAD"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.HEAD;
            }
            else if (str.StartsWith("POST"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.POST;
            }
            else if (str.StartsWith("PUT"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.PUT;
            }
            else if (str.StartsWith("DELETE"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.DELETE;
            }
            else if (str.StartsWith("TRACE"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.TRACE;
            }
            else if (str.StartsWith("OPTIONS"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.OPTIONS;
            }
            else if (str.StartsWith("CONNECT"))
            {
                this.messageTypeIsRequest = true;
                this.requestMethod = RequestMethods.CONNECT;
            }
            else
            {
                if (!str.StartsWith("HTTP"))
                {
                    throw new Exception("Incorrect HTTP Message Type or Request Method");
                }
                this.messageTypeIsRequest = false;
                this.requestMethod = RequestMethods.none;
            }
        Label_01E3:
            str2 = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
            if (str2 != null)
            {
                if (str2.Length > 0)
                {
                    this.headerFields.Add(str2);
                    this.ExtractHeaderField(str2);
                    goto Label_01E3;
                }
                this.packetHeaderIsComplete = true;
            }
            if (this.packetHeaderIsComplete && (dataIndex < packetEndIndex))
            {
                this.messageBody = new byte[(packetEndIndex - dataIndex) + 1];
                Array.Copy(parentFrame.Data, dataIndex, this.messageBody, 0, this.messageBody.Length);
            }
            else
            {
                this.messageBody = null;
            }
            if (this.messageTypeIsRequest)
            {
                if (this.requestMethod == RequestMethods.GET)
                {
                    string str3 = str.Substring(4, str.Length - 4);
                    if (str3.Contains(" HTTP"))
                    {
                        str3 = str3.Substring(0, str3.IndexOf(" HTTP"));
                    }
                    if (str3.Length > 0)
                    {
                        this.requestedFileName = str3;
                    }
                    else
                    {
                        this.requestedFileName = null;
                    }
                }
                else if (this.requestMethod == RequestMethods.POST)
                {
                    string str4 = str.Substring(5, str.Length - 5);
                    if (str4.Contains(" HTTP"))
                    {
                        str4 = str4.Substring(0, str4.IndexOf(" HTTP"));
                    }
                    if (str4.Length > 0)
                    {
                        this.requestedFileName = str4;
                    }
                }
            }
            else if (str.StartsWith("HTTP/1."))
            {
                this.statusCode = str.Substring(9, 3);
            }
        }

        internal bool ContentIsComplete()
        {
            if (this.contentLength == 0)
            {
                return true;
            }
            if (this.messageBody == null)
            {
                return false;
            }
            return (this.messageBody.Length >= this.contentLength);
        }

        private void ExtractHeaderField(string headerField)
        {
            if (headerField.StartsWith("Host: "))
            {
                this.requestedHost = headerField.Substring(6);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Requested Host", headerField.Substring(6));
                }
            }
            else if (headerField.StartsWith("User-Agent: ", StringComparison.OrdinalIgnoreCase))
            {
                this.userAgentBanner = headerField.Substring(12);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("User-Agent", this.userAgentBanner = headerField.Substring(12));
                }
            }
            else if (headerField.StartsWith("Server: ", StringComparison.OrdinalIgnoreCase))
            {
                this.serverBanner = headerField.Substring(8);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Server banner", this.serverBanner = headerField.Substring(8));
                }
            }
            else if (headerField.StartsWith("Cookie: ", StringComparison.OrdinalIgnoreCase))
            {
                this.cookie = headerField.Substring(8);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Cookie", this.cookie);
                }
            }
            else if (headerField.StartsWith("Content-Type: ", StringComparison.OrdinalIgnoreCase))
            {
                this.contentType = headerField.Substring(14);
            }
            else if (headerField.StartsWith("Content-Length: ", StringComparison.OrdinalIgnoreCase))
            {
                this.contentLength = Convert.ToInt32(headerField.Substring(0x10));
            }
            else if (headerField.StartsWith("Content-Encoding: ", StringComparison.OrdinalIgnoreCase))
            {
                this.contentEncoding = headerField.Substring(0x12);
            }
            else if (headerField.StartsWith("Transfer-Encoding: ", StringComparison.OrdinalIgnoreCase))
            {
                this.transferEncoding = headerField.Substring(0x13);
            }
            else if (headerField.StartsWith("WWW-Authenticate: ", StringComparison.OrdinalIgnoreCase) && headerField.Contains("realm=\""))
            {
                int startIndex = headerField.IndexOf("realm=\"") + 7;
                int index = headerField.IndexOf('"', startIndex);
                if ((startIndex >= 0) && (index > 0))
                {
                    this.wwwAuthenticateRealm = headerField.Substring(startIndex, index - startIndex);
                }
            }
            else if (headerField.StartsWith("Proxy-Authenticate: Basic realm=", StringComparison.OrdinalIgnoreCase))
            {
                this.wwwAuthenticateRealm = headerField.Substring(0x21, headerField.Length - 0x22);
            }
            else if (headerField.StartsWith("Authorization: Basic ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    byte[] buffer = Convert.FromBase64String(headerField.Substring(0x15));
                    StringBuilder builder = new StringBuilder(buffer.Length);
                    foreach (byte num3 in buffer)
                    {
                        builder.Append((char) num3);
                    }
                    string str2 = builder.ToString();
                    if (str2.Contains(":"))
                    {
                        this.authorizationCredentialsUsername = str2.Substring(0, str2.IndexOf(':'));
                        if ((str2.IndexOf(':') + 1) < str2.Length)
                        {
                            this.authorizationCredentailsPassword = str2.Substring(str2.IndexOf(':') + 1);
                        }
                        else
                        {
                            this.authorizationCredentailsPassword = "";
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.ParentFrame.Errors.Add(new Frame.Error(base.ParentFrame, base.PacketStartIndex, base.PacketEndIndex, "Cannot parse credentials in HTTP Authorization (" + exception.Message + ")"));
                    }
                }
            }
            else if (headerField.StartsWith("Authorization: Digest ", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    foreach (string str4 in headerField.Substring(0x16).Split(new char[] { ',' }))
                    {
                        string[] strArray = str4.Split(new char[] { '=' });
                        if (strArray.Length == 2)
                        {
                            string str5 = strArray[0].Trim();
                            string str6 = strArray[1].Trim(new char[] { ' ', '"', '\'' });
                            if (str5.Equals("username", StringComparison.InvariantCultureIgnoreCase))
                            {
                                this.authorizationCredentialsUsername = str6;
                                if (this.authorizationCredentailsPassword == null)
                                {
                                    this.authorizationCredentailsPassword = "N/A";
                                }
                            }
                            else if (str5.Equals("realm", StringComparison.InvariantCultureIgnoreCase))
                            {
                                this.wwwAuthenticateRealm = str6;
                            }
                        }
                    }
                }
                catch (Exception exception2)
                {
                    if (!base.ParentFrame.QuickParse)
                    {
                        base.ParentFrame.Errors.Add(new Frame.Error(base.ParentFrame, base.PacketStartIndex, base.PacketEndIndex, "Cannot parse credentials in HTTP Authorization (" + exception2.Message + ")"));
                    }
                }
            }
            else if (headerField.StartsWith("Content-Disposition:") && headerField.Contains("filename="))
            {
                string str7 = headerField.Substring(headerField.IndexOf("filename=") + 9).Trim();
                if (str7.StartsWith("\"") && (str7.IndexOf('"', 1) > 0))
                {
                    str7 = str7.Substring(1, str7.IndexOf('"', 1) - 1);
                }
                if (str7.Length > 0)
                {
                    this.contentDispositionFilename = str7;
                }
            }
        }

        internal List<MultipartPart> GetFormData()
        {
            List<MultipartPart> list = new List<MultipartPart>();
            if (((this.RequestMethod != RequestMethods.POST) || (this.messageBody == null)) || ((this.messageBody.Length <= 0) || (this.contentType == null)))
            {
                return list;
            }
            if (this.contentType.ToLower(CultureInfo.InvariantCulture).StartsWith("application/x-www-form-urlencoded"))
            {
                MultipartPart item = new MultipartPart(this.GetUrlEncodedNameValueCollection(ByteConverter.ReadString(this.messageBody), true));
                list.Add(item);
                return list;
            }
            if (this.contentType.ToLower(CultureInfo.InvariantCulture).StartsWith("multipart/form-data"))
            {
                string str = this.contentType.Substring(0x15);
                if (str.StartsWith("boundary="))
                {
                    string boundary = str.Substring(9);
                    foreach (MultipartPart part2 in PartBuilder.GetParts(this.messageBody, boundary))
                    {
                        list.Add(part2);
                    }
                    return list;
                }
            }
            return null;
        }

        internal NameValueCollection GetQuerystringData()
        {
            if ((this.requestedFileName != null) && this.requestedFileName.Contains("?"))
            {
                return this.GetUrlEncodedNameValueCollection(this.requestedFileName.Substring(this.requestedFileName.IndexOf('?') + 1), false);
            }
            return null;
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        private NameValueCollection GetUrlEncodedNameValueCollection(string urlEncodedData, bool isFormPostData)
        {
            NameValueCollection values = new NameValueCollection();
            char[] separator = new char[] { '&' };
            ICollection<string> is2 = HttpUtility.UrlDecode(urlEncodedData).Split(separator);
            if (isFormPostData)
            {
                List<string> list = new List<string>();
                bool flag = true;
                foreach (string str2 in is2)
                {
                    if (flag)
                    {
                        list.Add(str2);
                        if (str2.Contains("=[{") && !str2.EndsWith("}]"))
                        {
                            flag = false;
                        }
                    }
                    else
                    {
                        list[list.Count - 1] = list[list.Count - 1] + separator + str2;
                        if (str2.EndsWith("}]"))
                        {
                            flag = true;
                        }
                    }
                }
                is2 = list;
            }
            foreach (string str3 in is2)
            {
                if (str3.Length > 0)
                {
                    int index = str3.IndexOf('=');
                    if ((index > 0) && (index < (str3.Length - 1)))
                    {
                        string name = HttpUtility.UrlDecode(str3.Substring(0, index));
                        string str5 = HttpUtility.UrlDecode(str3.Substring(index + 1));
                        values.Add(name, str5);
                    }
                }
            }
            return values;
        }

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            int dataIndex = packetStartIndex;
            string str = ByteConverter.ReadLine(parentFrame.Data, ref dataIndex);
            if (str == null)
            {
                return false;
            }
            if (str.Length > 0x800)
            {
                return false;
            }
            if ((((!str.StartsWith("GET") && !str.StartsWith("HEAD")) && (!str.StartsWith("POST") && !str.StartsWith("PUT"))) && ((!str.StartsWith("DELETE") && !str.StartsWith("TRACE")) && (!str.StartsWith("OPTIONS") && !str.StartsWith("CONNECT")))) && !str.StartsWith("HTTP"))
            {
                return false;
            }
            try
            {
                result = new HttpPacket(parentFrame, packetStartIndex, packetEndIndex);
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

        internal string AuthorizationCredentialsPassword
        {
            get
            {
                return this.authorizationCredentailsPassword;
            }
        }

        internal string AuthorizationCredentialsUsername
        {
            get
            {
                return this.authorizationCredentialsUsername;
            }
        }

        internal string ContentDispositionFilename
        {
            get
            {
                return this.contentDispositionFilename;
            }
        }

        internal string ContentEncoding
        {
            get
            {
                return this.contentEncoding;
            }
        }

        internal int ContentLength
        {
            get
            {
                return this.contentLength;
            }
        }

        internal string ContentType
        {
            get
            {
                return this.contentType;
            }
        }

        internal string Cookie
        {
            get
            {
                return this.cookie;
            }
        }

        internal List<string> HeaderFields
        {
            get
            {
                return this.headerFields;
            }
        }

        internal byte[] MessageBody
        {
            get
            {
                return this.messageBody;
            }
        }

        internal bool MessageTypeIsRequest
        {
            get
            {
                return this.messageTypeIsRequest;
            }
        }

        public bool PacketHeaderIsComplete
        {
            get
            {
                return this.packetHeaderIsComplete;
            }
        }

        public int ParsedBytesCount
        {
            get
            {
                return base.PacketLength;
            }
        }

        internal string RequestedFileName
        {
            get
            {
                return this.requestedFileName;
            }
        }

        internal string RequestedHost
        {
            get
            {
                return this.requestedHost;
            }
        }

        internal RequestMethods RequestMethod
        {
            get
            {
                return this.requestMethod;
            }
        }

        internal string ServerBanner
        {
            get
            {
                return this.serverBanner;
            }
        }

        internal string StatusCode
        {
            get
            {
                return this.statusCode;
            }
        }

        internal string TransferEncoding
        {
            get
            {
                return this.transferEncoding;
            }
        }

        internal string UserAgentBanner
        {
            get
            {
                return this.userAgentBanner;
            }
        }

        internal string WwwAuthenticateRealm
        {
            get
            {
                return this.wwwAuthenticateRealm;
            }
        }


        internal enum ContentEncodings
        {
            Gzip,
            Compress,
            Deflate,
            Identity
        }

        internal enum RequestMethods
        {
            GET,
            HEAD,
            POST,
            PUT,
            DELETE,
            TRACE,
            OPTIONS,
            CONNECT,
            none
        }
    }
}

