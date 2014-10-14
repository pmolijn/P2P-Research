namespace PacketParser.Utils
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.InteropServices;

    public static class KnuthMorrisPratt
    {
        public static int[] KmpFailureFunction(byte[] pattern)
        {
            int index = 1;
            int num2 = 0;
            int[] numArray = new int[pattern.Length];
            if (numArray.Length > 0)
            {
                numArray[0] = 0;
            }
            while (index < pattern.Length)
            {
                if (pattern[num2] == pattern[index])
                {
                    numArray[index] = num2 + 1;
                    index++;
                    num2++;
                }
                else
                {
                    if (num2 > 0)
                    {
                        num2 = numArray[num2 - 1];
                        continue;
                    }
                    numArray[index] = 0;
                    index++;
                }
            }
            return numArray;
        }

        public static long ReadTo(byte[] pattern, Stream stream, out List<byte> readBytes)
        {
            return ReadTo(pattern, stream, out readBytes, KmpFailureFunction(pattern));
        }

        public static long ReadTo(byte[] pattern, byte[] data, int offset, out List<byte> readBytes)
        {
            return ReadTo(pattern, data, offset, out readBytes, KmpFailureFunction(pattern));
        }

        public static long ReadTo(byte[] pattern, Stream stream, out List<byte> readBytes, int[] kmpFailureFunction)
        {
            readBytes = new List<byte>();
            long position = stream.Position;
            int index = 0;
            int num2 = stream.ReadByte();
            if (num2 < 0)
            {
                return (long) num2;
            }
            byte item = (byte) num2;
            readBytes.Add(item);
            while (stream.Position < (stream.Length + 1L))
            {
                if (pattern[index] == item)
                {
                    if (index == (pattern.Length - 1))
                    {
                        return (stream.Position - pattern.Length);
                    }
                    num2 = stream.ReadByte();
                    if (num2 < 0)
                    {
                        return (long) num2;
                    }
                    item = (byte) num2;
                    readBytes.Add(item);
                    index++;
                }
                else if (index > 0)
                {
                    index = kmpFailureFunction[index - 1];
                }
                else
                {
                    num2 = stream.ReadByte();
                    if (num2 < 0)
                    {
                        return (long) num2;
                    }
                    item = (byte) num2;
                    readBytes.Add(item);
                }
            }
            return -1L;
        }

        public static long ReadTo(byte[] pattern, byte[] data, int offset, out List<byte> readBytes, int[] kmpFailureFunction)
        {
            MemoryStream stream = new MemoryStream(data) {
                Position = offset
            };
            return ReadTo(pattern, stream, out readBytes, kmpFailureFunction);
        }
    }
}

