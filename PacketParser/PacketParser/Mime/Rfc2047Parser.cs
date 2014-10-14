namespace PacketParser.Mime
{
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal class Rfc2047Parser
    {
        private static readonly char[] EQ = new char[] { '=' };
        private static readonly char[] QU = new char[] { '?' };

        public static string DecodeRfc2047Parts(string encoded)
        {
            try
            {
                StringBuilder builder = new StringBuilder();
                int startIndex = 0;
                while (startIndex < (encoded.Length - 4))
                {
                    int index = encoded.IndexOf("=?", startIndex);
                    if (index < 0)
                    {
                        builder.Append(encoded.Substring(startIndex));
                        return builder.ToString();
                    }
                    builder.Append(encoded, startIndex, index - startIndex);
                    int num3 = encoded.IndexOf("?=", (int) (index + 2));
                    while ((num3 > index) && (num3 <= (encoded.Length - 2)))
                    {
                        if (IsRfc2047String(encoded.Substring(index, (num3 - index) + 2)))
                        {
                            break;
                        }
                        num3 = encoded.IndexOf("?=", (int) (num3 + 1));
                    }
                    if (num3 < 0)
                    {
                        builder.Append(encoded.Substring(index));
                        return builder.ToString();
                    }
                    if (IsRfc2047String(encoded.Substring(index, (num3 - index) + 2)))
                    {
                        num3 += 2;
                        builder.Append(ParseRfc2047String(encoded.Substring(index, num3 - index)));
                        startIndex = num3;
                    }
                    else
                    {
                        builder.Append(encoded.Substring(index));
                        return builder.ToString();
                    }
                }
                if (startIndex < encoded.Length)
                {
                    builder.Append(encoded.Substring(startIndex));
                }
                return builder.ToString();
            }
            catch (Exception)
            {
                return encoded;
            }
        }

        public static bool IsRfc2047String(string s)
        {
            try
            {
                return ((s.StartsWith("=?") && s.EndsWith("?=")) && (s.Trim(EQ).Split(QU, StringSplitOptions.RemoveEmptyEntries).Length == 3));
            }
            catch (Exception)
            {
            }
            return false;
        }

        public static string ParseRfc2047String(string rfc2047String)
        {
            if (!rfc2047String.StartsWith("=?") || !rfc2047String.EndsWith("?="))
            {
                throw new Exception("Invalid RFC 2047 string");
            }
            string[] strArray = rfc2047String.Trim(EQ).Split(QU, StringSplitOptions.RemoveEmptyEntries);
            if (strArray.Length != 3)
            {
                throw new Exception("Invalid RFC 2047 string");
            }
            System.Text.Encoding encoding = System.Text.Encoding.GetEncoding(strArray[0]);
            List<byte> list = new List<byte>();
            for (int i = 0; i < strArray[2].Length; i++)
            {
                list.Add((byte) strArray[2][i]);
            }
            if (strArray[1].Equals("B", StringComparison.InvariantCultureIgnoreCase))
            {
                list = new List<byte>(Convert.FromBase64String(strArray[2]));
            }
            else if (strArray[1].Equals("Q", StringComparison.InvariantCultureIgnoreCase))
            {
                list = ByteConverter.ReadQuotedPrintable(list.ToArray());
            }
            return encoding.GetString(list.ToArray());
        }
    }
}

