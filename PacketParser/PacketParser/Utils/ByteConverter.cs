namespace PacketParser.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;

    public static class ByteConverter
    {
        public static string ReadHexString(byte[] data, int nBytesToRead)
        {
            return ReadHexString(data, nBytesToRead, 0);
        }

        public static string ReadHexString(byte[] data, int nBytesToRead, int offset)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < nBytesToRead; i++)
            {
                builder.Append(data[offset + i].ToString("X2"));
            }
            return builder.ToString();
        }

        public static string ReadLengthValueString(byte[] data, ref int index, int stringLengthFieldBytes)
        {
            int lenght = 0;
            if (stringLengthFieldBytes == 1)
            {
                lenght = data[index];
            }
            else if (stringLengthFieldBytes == 2)
            {
                lenght = ToUInt16(data, index);
            }
            else
            {
                if (stringLengthFieldBytes != 4)
                {
                    throw new Exception("Selected stringLengthFieldBytes is not supported");
                }
                lenght = (int) ToUInt32(data, index);
            }
            index += stringLengthFieldBytes;
            string str = ReadString(data, index, lenght);
            index += lenght;
            return str;
        }

        public static string ReadLine(byte[] data, ref int dataIndex)
        {
            int num = 0x4000;
            StringBuilder builder = new StringBuilder();
            bool flag = false;
            bool flag2 = false;
            int num2 = 0;
            while (!flag || !flag2)
            {
                if (((dataIndex + num2) >= data.Length) || (num2 >= num))
                {
                    return null;
                }
                byte num3 = data[dataIndex + num2];
                if (num3 == 13)
                {
                    flag = true;
                }
                else if (flag && (num3 == 10))
                {
                    flag2 = true;
                }
                else
                {
                    builder.Append((char) num3);
                    flag = false;
                    flag2 = false;
                }
                num2++;
            }
            dataIndex += num2;
            return builder.ToString();
        }

        public static string ReadNullTerminatedString(byte[] data, ref int dataIndex)
        {
            return ReadNullTerminatedString(data, ref dataIndex, false, false);
        }

        public static string ReadNullTerminatedString(byte[] data, ref int dataIndex, bool unicodeData, bool reverseOrder)
        {
            int maxStringLength = 0x400;
            return ReadNullTerminatedString(data, ref dataIndex, unicodeData, reverseOrder, maxStringLength);
        }

        public static string ReadNullTerminatedString(byte[] data, ref int dataIndex, bool unicodeData, bool reverseOrder, int maxStringLength)
        {
            StringBuilder builder = new StringBuilder();
            if (!unicodeData)
            {
                for (int i = 0; ((dataIndex + i) < data.Length) && (i < maxStringLength); i++)
                {
                    byte num2 = data[dataIndex + i];
                    if (num2 == 0)
                    {
                        dataIndex += i + 1;
                        return builder.ToString();
                    }
                    builder.Append((char) num2);
                }
            }
            else
            {
                for (int j = 0; ((dataIndex + j) < data.Length) && (j < (maxStringLength * 2)); j += 2)
                {
                    ushort num4;
                    if (((dataIndex + j) + 1) < data.Length)
                    {
                        num4 = ToUInt16(data, dataIndex + j, reverseOrder);
                    }
                    else
                    {
                        num4 = data[dataIndex + j];
                    }
                    if (num4 == 0)
                    {
                        dataIndex += j + 2;
                        return builder.ToString();
                    }
                    builder.Append((char) num4);
                }
            }
            return builder.ToString();
        }

        public static List<byte> ReadQuotedPrintable(byte[] quotedPrintableData)
        {
            List<byte> list = new List<byte>();
            byte num = 0x3d;
            for (int i = 0; i < quotedPrintableData.Length; i++)
            {
                if ((quotedPrintableData[i] == num) && ((i + 2) < quotedPrintableData.Length))
                {
                    string str = ReadString(quotedPrintableData, i + 1, 2);
                    try
                    {
                        list.Add(Convert.ToByte(str, 0x10));
                    }
                    catch (Exception)
                    {
                    }
                    i += 2;
                }
                else
                {
                    list.Add(quotedPrintableData[i]);
                }
            }
            return list;
        }

        public static string ReadString(byte[] data)
        {
            int dataIndex = 0;
            return ReadString(data, ref dataIndex, data.Length, false, false);
        }

        public static string ReadString(byte[] data, string nonAsciiNonPrintableReplacement)
        {
            return Regex.Replace(ReadString(data), @"\p{Cc}", nonAsciiNonPrintableReplacement);
        }

        public static string ReadString(byte[] data, int startIndex, int lenght)
        {
            return ReadString(data, ref startIndex, lenght, false, false);
        }

        public static string ReadString(byte[] data, int startIndex, int lenght, bool unicodeData, bool reverseOrder)
        {
            return ReadString(data, ref startIndex, lenght, unicodeData, reverseOrder);
        }

        public static string ReadString(byte[] data, ref int dataIndex, int bytesToRead, bool unicodeData, bool reverseOrder)
        {
            return ReadString(data, ref dataIndex, bytesToRead, unicodeData, reverseOrder, Encoding.Normal);
        }

        public static string ReadString(byte[] data, ref int dataIndex, int bytesToRead, bool unicodeData, bool reverseOrder, Encoding encoding)
        {
            int num = 0;
            StringBuilder builder = new StringBuilder();
            while ((num < bytesToRead) && ((dataIndex + num) < data.Length))
            {
                if (unicodeData)
                {
                    ushort num2 = ToUInt16(data, dataIndex + num, reverseOrder);
                    if (encoding == Encoding.TDS_password)
                    {
                        num2 = (ushort) (num2 ^ 0xa5a5);
                        num2 = SwapNibbles(num2);
                    }
                    builder.Append((char) num2);
                    num += 2;
                }
                else
                {
                    builder.Append((char) data[dataIndex + num]);
                    num++;
                }
            }
            dataIndex += num;
            return builder.ToString();
        }

        public static double StringToClosestDouble(string numberLikeLookingString)
        {
            double num = 0.0;
            int num2 = 0;
            for (int i = 0; i < numberLikeLookingString.Length; i++)
            {
                char c = numberLikeLookingString[i];
                if (char.IsNumber(c))
                {
                    if (num2 == 0)
                    {
                        num = (num * 10.0) + ((double) c);
                    }
                    else
                    {
                        num += num / Math.Pow(10.0, (double) num2);
                        num2++;
                    }
                }
                else if ((num2 == 0) && ((c == '.') || (c == ',')))
                {
                    num2 = 1;
                }
            }
            return num;
        }

        public static ushort SwapNibbles(ushort data)
        {
            return (ushort) (((data >> 4) & 0xf0f) | ((data << 4) & 0xf0f0));
        }

        public static void ToByteArray(ushort value, byte[] array, int arrayOffset)
        {
            array[arrayOffset] = (byte) (value >> 8);
            array[arrayOffset + 1] = (byte) (value & 0xff);
        }

        public static void ToByteArray(uint value, byte[] array, int arrayOffset)
        {
            array[arrayOffset] = (byte) (value >> 0x18);
            array[arrayOffset + 1] = (byte) ((value >> 0x10) & 0xff);
            array[arrayOffset + 2] = (byte) ((value >> 8) & 0xff);
            array[arrayOffset + 3] = (byte) (value & 0xff);
        }

        public static byte[] ToByteArray(byte[] source, ref int index, byte endValue, bool copyEndValue)
        {
            byte[] endValues = new byte[] { endValue };
            return ToByteArray(source, ref index, endValues, copyEndValue);
        }

        public static byte[] ToByteArray(byte[] source, ref int index, byte[] endValues, bool copyEndValue)
        {
            int num = source.Length - index;
            foreach (byte num2 in endValues)
            {
                int num3 = Array.IndexOf<byte>(source, num2, index);
                if ((num3 > index) && (((num3 - index) + 1) < num))
                {
                    num = (num3 - index) + 1;
                }
            }
            int num4 = num;
            if (!copyEndValue && (Array.IndexOf<byte>(endValues, source[(index + num) - 1]) != -1))
            {
                num4--;
            }
            byte[] destinationArray = new byte[num4];
            Array.Copy(source, index, destinationArray, 0, destinationArray.Length);
            index += num;
            return destinationArray;
        }

        public static byte[] ToByteArrayFromHexString(string hexString)
        {
            if (!hexString.StartsWith("0x"))
            {
                throw new Exception("HexString must start with \"0x\"");
            }
            if ((hexString.Length % 2) != 0)
            {
                throw new Exception("HexString must contain an even number of bytes");
            }
            byte[] buffer = new byte[(hexString.Length - 2) / 2];
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = Convert.ToByte(hexString.Substring(2 + (i * 2), 2), 0x10);
            }
            return buffer;
        }

        public static string ToMd5HashString(string originalText)
        {
            MD5 md = MD5.Create();
            byte[] buffer = new byte[originalText.Length];
            for (int i = 0; i < originalText.Length; i++)
            {
                buffer[i] = (byte) originalText[i];
            }
            byte[] buffer2 = md.ComputeHash(buffer);
            StringBuilder builder = new StringBuilder();
            for (int j = 0; j < buffer2.Length; j++)
            {
                builder.Append(buffer2[j].ToString("X2").ToLower());
            }
            return builder.ToString();
        }

        public static List<byte> ToQuotedPrintable(string text)
        {
            List<byte> list = new List<byte>();
            foreach (byte num in System.Text.Encoding.GetEncoding(850).GetBytes(text))
            {
                if ((num >= 0x21) && (num <= 60))
                {
                    list.Add(num);
                }
                else if ((num >= 0x3e) && (num <= 0x7e))
                {
                    list.Add(num);
                }
                else if ((num == 9) || (num == 0x20))
                {
                    list.Add(num);
                }
                else
                {
                    string s = "=" + num.ToString("X2");
                    foreach (byte num2 in System.Text.Encoding.ASCII.GetBytes(s))
                    {
                        list.Add(num2);
                    }
                }
            }
            return list;
        }

        public static ushort ToUInt16(byte[] value)
        {
            return (ushort) ToUInt32(value, 0, 2, false);
        }

        public static ushort ToUInt16(byte[] value, int startIndex)
        {
            return (ushort) ToUInt32(value, startIndex, 2, false);
        }

        public static ushort ToUInt16(byte[] value, int startIndex, bool reverseByteOrder)
        {
            return (ushort) ToUInt32(value, startIndex, 2, reverseByteOrder);
        }

        public static uint ToUInt32(byte[] value)
        {
            return ToUInt32(value, 0, value.Length, false);
        }

        public static uint ToUInt32(IPAddress ip)
        {
            byte[] addressBytes = ip.GetAddressBytes();
            long num = 0L;
            for (int i = 0; i < addressBytes.Length; i++)
            {
                num = (num << 8) + addressBytes[i];
            }
            return (uint) num;
        }

        public static uint ToUInt32(byte[] value, int startIndex)
        {
            return ToUInt32(value, startIndex, 4, false);
        }

        public static uint ToUInt32(ushort ushort1, ushort ushort2)
        {
            uint num = ushort1;
            num = num << 0x10;
            return (num ^ ushort2);
        }

        public static uint ToUInt32(byte[] value, int startIndex, int nBytes)
        {
            return ToUInt32(value, startIndex, nBytes, false);
        }

        public static uint ToUInt32(byte[] value, int startIndex, int nBytes, bool reverseByteOrder)
        {
            uint num = 0;
            for (int i = 0; (i < nBytes) && ((i + startIndex) < value.Length); i++)
            {
                num = num << 8;
                if (reverseByteOrder)
                {
                    num += value[((startIndex + nBytes) - 1) - i];
                }
                else
                {
                    num += value[startIndex + i];
                }
            }
            return num;
        }

        public static ulong ToUInt64(byte[] value, int startIndex, bool reverseOrder)
        {
            ulong num = 0L;
            uint num2 = ToUInt32(value, startIndex, 4, reverseOrder);
            uint num3 = ToUInt32(value, startIndex + 4, 4, reverseOrder);
            if (reverseOrder)
            {
                num += num3;
                num = num << 0x20;
                return (num + num2);
            }
            num += num2;
            num = num << 0x20;
            return (num + num3);
        }

        public static DateTime ToUnixTimestamp(byte[] data, int offset)
        {
            long num = ToUInt32(data, offset);
            DateTime time = new DateTime(0x7b2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return time.AddTicks(num * 0x989680L);
        }

        public static string ToXxdHexString(byte[] data)
        {
            string str = ReadHexString(data, data.Length);
            string str2 = ReadString(data, ".");
            return (str + "\t" + str2);
        }

        public enum Encoding
        {
            Normal,
            TDS_password
        }
    }
}

