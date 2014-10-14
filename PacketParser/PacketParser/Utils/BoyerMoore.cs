namespace PacketParser.Utils
{
    using System;

    public class BoyerMoore
    {
        public static int IndexOf(byte[] haystack, byte[] needle)
        {
            if (needle.Length == 0)
            {
                return 0;
            }
            int[] byteTable = MakeByteTable(needle);
            int[] offsetTable = MakeOffsetTable(needle);
            return IndexOf(haystack, needle, byteTable, offsetTable);
        }

        public static int IndexOf(byte[] haystack, byte[] needle, int[] byteTable, int[] offsetTable)
        {
            int num2;
            if (needle.Length == 0)
            {
                return 0;
            }
            for (int i = needle.Length - 1; i < haystack.Length; i += Math.Max(offsetTable[(needle.Length - 1) - num2], byteTable[haystack[i]]))
            {
                for (num2 = needle.Length - 1; needle[num2] == haystack[i]; num2--)
                {
                    if (num2 == 0)
                    {
                        return i;
                    }
                    i--;
                }
            }
            return -1;
        }

        public static int IndexOf(byte[] haystack, byte[] needle, int[] byteTable, int[] offsetTable, bool ignoreCase)
        {
            int num2;
            if (!ignoreCase)
            {
                return IndexOf(haystack, needle, byteTable, offsetTable);
            }
            if (needle.Length == 0)
            {
                return 0;
            }
            for (int i = needle.Length - 1; i < haystack.Length; i += Math.Max(offsetTable[(needle.Length - 1) - num2], byteTable[ToUpper(haystack[i])]))
            {
                for (num2 = needle.Length - 1; ToUpper(needle[num2]) == ToUpper(haystack[i]); num2--)
                {
                    if (num2 == 0)
                    {
                        return i;
                    }
                    i--;
                }
            }
            return -1;
        }

        private static bool isPrefix(byte[] needle, int p)
        {
            int index = p;
            for (int i = 0; index < needle.Length; i++)
            {
                if (needle[index] != needle[i])
                {
                    return false;
                }
                index++;
            }
            return true;
        }

        public static int[] MakeByteTable(byte[] needle)
        {
            int[] numArray = new int[0x100];
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = needle.Length;
            }
            for (int j = 0; j < (needle.Length - 1); j++)
            {
                numArray[needle[j]] = (needle.Length - 1) - j;
            }
            return numArray;
        }

        public static int[] MakeByteTable(byte[] needle, bool ignoreCase)
        {
            if (!ignoreCase)
            {
                return MakeByteTable(needle);
            }
            int[] numArray = new int[0x100];
            for (int i = 0; i < numArray.Length; i++)
            {
                numArray[i] = needle.Length;
            }
            for (int j = 0; j < (needle.Length - 1); j++)
            {
                numArray[ToUpper(needle[j])] = (needle.Length - 1) - j;
            }
            return numArray;
        }

        public static int[] MakeOffsetTable(byte[] needle)
        {
            int[] numArray = new int[needle.Length];
            int length = needle.Length;
            for (int i = needle.Length - 1; i >= 0; i--)
            {
                if (isPrefix(needle, i + 1))
                {
                    length = i + 1;
                }
                numArray[(needle.Length - 1) - i] = ((length - i) + needle.Length) - 1;
            }
            for (int j = 0; j < (needle.Length - 1); j++)
            {
                int index = suffixLength(needle, j);
                numArray[index] = ((needle.Length - 1) - j) + index;
            }
            return numArray;
        }

        private static int suffixLength(byte[] needle, int p)
        {
            int num = 0;
            int index = p;
            for (int i = needle.Length - 1; (index >= 0) && (needle[index] == needle[i]); i--)
            {
                num++;
                index--;
            }
            return num;
        }

        public static byte[] ToUpper(byte[] b)
        {
            byte[] buffer = new byte[b.Length];
            for (int i = 0; i < b.Length; i++)
            {
                buffer[i] = ToUpper(b[i]);
            }
            return buffer;
        }

        public static byte ToUpper(byte b)
        {
            if (b < 0x61)
            {
                return b;
            }
            return (byte) (b - 0x20);
        }
    }
}

