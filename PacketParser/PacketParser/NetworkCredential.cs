namespace PacketParser
{
    using System;
    using System.Collections.Specialized;
    using System.Xml;

    public class NetworkCredential : IComparable<NetworkCredential>
    {
        private NetworkHost client;
        private bool isProvenValid;
        private DateTime loginTimestamp;
        private string password;
        private string protocolString;
        private NetworkHost server;
        private string username;

        internal NetworkCredential(NetworkHost client, NetworkHost server, string protocolString, string username, DateTime loginTimestamp) : this(client, server, protocolString, username, null, loginTimestamp)
        {
        }

        internal NetworkCredential(NetworkHost client, NetworkHost server, string protocolString, string username, string password, DateTime loginTimestamp) : this(client, server, protocolString, username, password, false, loginTimestamp)
        {
        }

        internal NetworkCredential(NetworkHost client, NetworkHost server, string protocolString, string username, string password, bool isProvenValid, DateTime loginTimestamp)
        {
            this.client = client;
            this.server = server;
            this.protocolString = protocolString;
            this.username = username;
            this.password = password;
            this.isProvenValid = isProvenValid;
            this.loginTimestamp = loginTimestamp;
        }

        public int CompareTo(NetworkCredential other)
        {
            return this.Key.CompareTo(other.Key);
        }

        public static string GetCredentialSessionString(NetworkCredential credential)
        {
            return GetCredentialSessionString(credential.client, credential.server, credential.protocolString);
        }

        public static string GetCredentialSessionString(NetworkHost client, NetworkHost server, string protocolString)
        {
            return (client.IPAddress.ToString() + server.IPAddress.ToString() + protocolString);
        }

        public static NetworkCredential GetNetworkCredential(NameValueCollection parameters, NetworkHost client, NetworkHost server, string protocolString, DateTime timestamp)
        {
            if (parameters != null)
            {
                string username = null;
                string str2 = null;
                string password = null;
                string str4 = null;
                foreach (string str5 in parameters)
                {
                    if (str5.Equals("user[screen_name]"))
                    {
                        username = parameters[str5];
                    }
                    else if (str5.Equals("gmailchat"))
                    {
                        username = parameters[str5];
                        if (password == null)
                        {
                            password = "N/A (unknown Google password)";
                        }
                    }
                    else if (str5.Equals("login_str"))
                    {
                        username = "Facebook email: " + parameters[str5];
                        if (password == null)
                        {
                            password = "N/A (unknown Facebook password)";
                        }
                    }
                    else if (str5.Equals("login_username"))
                    {
                        username = parameters[str5];
                    }
                    else if (str5.Equals("secretkey"))
                    {
                        password = parameters[str5];
                    }
                    else if (str5.Equals("xml") && parameters[str5].Contains("mail_inc_pass"))
                    {
                        XmlDocument document = new XmlDocument();
                        document.LoadXml(parameters[str5]);
                        XmlNode node = document.SelectSingleNode("/webmail/param[@name='mail_inc_pass']");
                        XmlNode node2 = document.SelectSingleNode("/webmail/param[@name='email']");
                        XmlNode node3 = document.SelectSingleNode("/webmail/param[@name='mail_inc_login']");
                        if (((password == null) && (node != null)) && ((node.InnerText != null) && (node.InnerText.Length > 0)))
                        {
                            password = node.InnerText;
                        }
                        if (((username == null) && (node2 != null)) && ((node2.InnerText != null) && (node2.InnerText.Length > 0)))
                        {
                            username = node2.InnerText;
                        }
                        else if (((username == null) && (node3 != null)) && ((node3.InnerText != null) && (node3.InnerText.Length > 0)))
                        {
                            username = node3.InnerText;
                        }
                    }
                    else if (str5.Equals("profile_id"))
                    {
                        username = "Facebook profile ID: " + parameters[str5];
                        if (password == null)
                        {
                            password = "N/A (unknown Facebook password)";
                        }
                    }
                    else if (str5.ToLower().Contains("username"))
                    {
                        str2 = parameters[str5];
                    }
                    else if (str5.ToLower().Contains("password"))
                    {
                        str4 = parameters[str5];
                    }
                    else if (str5.ToLower().Contains("user") || str5.ToLower().Contains("usr"))
                    {
                        str2 = parameters[str5];
                    }
                    else if (str5.ToLower().Contains("pass") || str5.ToLower().Contains("pw"))
                    {
                        str4 = parameters[str5];
                    }
                    else if ((str2 == null) && str5.ToLower().Contains("mail"))
                    {
                        str2 = parameters[str5];
                    }
                    else if ((str2 == null) && str5.ToLower().Contains("log"))
                    {
                        str2 = parameters[str5];
                    }
                }
                if (username == null)
                {
                    username = str2;
                }
                if (password == null)
                {
                    password = str4;
                }
                if ((username != null) && (password != null))
                {
                    return new NetworkCredential(client, server, protocolString, username, password, timestamp);
                }
                if (username != null)
                {
                    return new NetworkCredential(client, server, protocolString, username, timestamp);
                }
            }
            return null;
        }

        public override string ToString()
        {
            return (this.server.ToString() + " " + this.protocolString + " " + this.username);
        }

        public NetworkHost Client
        {
            get
            {
                return this.client;
            }
        }

        public bool IsProvenValid
        {
            get
            {
                return this.isProvenValid;
            }
            set
            {
                this.isProvenValid = value;
            }
        }

        public string Key
        {
            get
            {
                if (this.password == null)
                {
                    return (this.protocolString + this.username + this.server.IPAddress.ToString() + this.client.IPAddress.ToString());
                }
                return string.Concat(new object[] { this.protocolString, this.username, this.password.GetHashCode(), this.server.IPAddress.ToString(), this.client.IPAddress.ToString() });
            }
        }

        public DateTime LoginTimestamp
        {
            get
            {
                return this.loginTimestamp;
            }
            set
            {
                this.loginTimestamp = value;
            }
        }

        public string Password
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

        public string ProtocolString
        {
            get
            {
                return this.protocolString;
            }
        }

        public NetworkHost Server
        {
            get
            {
                return this.server;
            }
        }

        public string Username
        {
            get
            {
                return this.username;
            }
        }
    }
}

