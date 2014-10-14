namespace PacketParser.Utils
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    public class StringManglerUtil
    {
        private static readonly char[] FILE_CHARS = new char[] { '-', '_' };
        private static readonly char[] LOWER_CASE = new char[] { 
            'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 
            'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'
         };
        private static readonly char[] NUMBERS = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '0' };
        private static readonly char[] UPPER_CASE = new char[] { 
            'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z'
         };

        public static byte[][] ConvertStringsToByteArrayArray(IEnumerable strings)
        {
            return ConvertStringsToByteArrayArray(strings, 3);
        }

        public static byte[][] ConvertStringsToByteArrayArray(IEnumerable strings, int minLength)
        {
            List<byte[]> list = new List<byte[]>();
            foreach (string str in strings)
            {
                if (str.StartsWith("0x"))
                {
                    list.Add(ByteConverter.ToByteArrayFromHexString(str));
                }
                else if (str.Length >= minLength)
                {
                    char[] chars = str.ToCharArray();
                    byte[] bytes = System.Text.Encoding.Default.GetBytes(chars);
                    byte[] destinationArray = System.Text.Encoding.BigEndianUnicode.GetBytes(chars);
                    if ((destinationArray.Length > 0) && (destinationArray[0] == 0))
                    {
                        byte[] sourceArray = destinationArray;
                        destinationArray = new byte[sourceArray.Length - 1];
                        Array.Copy(sourceArray, 1, destinationArray, 0, destinationArray.Length);
                    }
                    byte[] item = System.Text.Encoding.UTF8.GetBytes(chars);
                    list.Add(destinationArray);
                    list.Add(bytes);
                    if (bytes.Length != item.Length)
                    {
                        list.Add(item);
                    }
                }
            }
            return list.ToArray();
        }

        public static string ConvertToFilename(string anyString, int maxLength)
        {
            StringBuilder builder = new StringBuilder();
            List<char> list = new List<char>();
            list.AddRange(NUMBERS);
            list.AddRange(LOWER_CASE);
            list.AddRange(UPPER_CASE);
            list.AddRange(FILE_CHARS);
            foreach (char ch in anyString.ToCharArray())
            {
                if (list.Contains(ch))
                {
                    builder.Append(ch);
                    if (builder.Length >= maxLength)
                    {
                        break;
                    }
                }
            }
            return builder.ToString();
        }

        public static string GetExtension(string contentType)
        {
            if (contentType == null)
            {
                return null;
            }
            string str = contentType.Substring(contentType.IndexOf('/') + 1);
            if (str.Contains(";"))
            {
                str = str.Substring(0, str.IndexOf(";"));
            }
            if (str.Equals("plain", StringComparison.InvariantCultureIgnoreCase))
            {
                str = "txt";
            }
            return str;
        }

        public static void WritePascalString(string s, BinaryWriter w)
        {
            char[] chArray;
            if (s.Length > 0xff)
            {
                chArray = s.ToCharArray(0, 0xff);
            }
            else
            {
                chArray = s.ToCharArray();
            }
            w.Write((byte) chArray.Length);
            long position = w.BaseStream.Position;
            w.Write(chArray);
            long num1 = w.BaseStream.Position;
        }
    }
}

