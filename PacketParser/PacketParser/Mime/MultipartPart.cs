namespace PacketParser.Mime
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Runtime.InteropServices;

    internal class MultipartPart
    {
        private NameValueCollection attributes;
        private byte[] data;

        internal MultipartPart(NameValueCollection attributes) : this(attributes, new byte[0])
        {
        }

        internal MultipartPart(byte[] partData) : this(new ByteArrayStream(partData, 0L), 0L, partData.Length)
        {
        }

        internal MultipartPart(NameValueCollection attributes, byte[] data)
        {
            this.attributes = attributes;
            this.data = data;
        }

        internal MultipartPart(Stream stream, long partStartIndex, int partLength)
        {
            ReadHeaderAttributes(stream, partStartIndex, out this.attributes);
            this.data = new byte[(partLength + partStartIndex) - stream.Position];
            stream.Read(this.data, 0, this.data.Length);
        }

        internal static void ReadHeaderAttributes(Stream stream, long partStartIndex, out NameValueCollection attributes)
        {
            stream.Position = partStartIndex;
            UnbufferedReader reader = new UnbufferedReader(stream);
            attributes = new NameValueCollection();
            string str = reader.ReadLine(200);
            char[] separator = new char[] { ';' };
            while (((str != null) && (str.Length > 0)) && (stream.Position < stream.Length))
            {
                string[] strArray = str.Split(separator);
                for (int i = 0; i < strArray.Length; i++)
                {
                    try
                    {
                        if (strArray[i].Contains("=\"") && (strArray[i].Length > (strArray[i].IndexOf('"') + 1)))
                        {
                            string name = strArray[i].Substring(0, strArray[i].IndexOf('=')).Trim();
                            string str3 = Rfc2047Parser.DecodeRfc2047Parts(strArray[i].Substring(strArray[i].IndexOf('"') + 1, (strArray[i].LastIndexOf('"') - strArray[i].IndexOf('"')) - 1).Trim());
                            attributes.Add(name, str3);
                        }
                        else if (strArray[i].Contains(": "))
                        {
                            string str4 = strArray[i].Substring(0, strArray[i].IndexOf(':')).Trim();
                            string str5 = Rfc2047Parser.DecodeRfc2047Parts(strArray[i].Substring(strArray[i].IndexOf(':') + 1).Trim());
                            attributes.Add(str4, str5);
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
                str = reader.ReadLine(200);
            }
        }

        public NameValueCollection Attributes
        {
            get
            {
                return this.attributes;
            }
        }

        public byte[] Data
        {
            get
            {
                return this.data;
            }
        }
    }
}

