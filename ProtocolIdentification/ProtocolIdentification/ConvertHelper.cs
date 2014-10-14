namespace ProtocolIdentification
{
    using System;

    internal class ConvertHelper
    {
        private static int GetLargestPrimeValue(int maxBitsIntPrimeNumber)
        {
            if (maxBitsIntPrimeNumber == 1)
            {
                return 1;
            }
            if (maxBitsIntPrimeNumber == 2)
            {
                return 3;
            }
            if (maxBitsIntPrimeNumber == 3)
            {
                return 7;
            }
            if (maxBitsIntPrimeNumber == 4)
            {
                return 13;
            }
            if (maxBitsIntPrimeNumber == 5)
            {
                return 0x1f;
            }
            if (maxBitsIntPrimeNumber == 6)
            {
                return 0x3d;
            }
            if (maxBitsIntPrimeNumber == 7)
            {
                return 0x7f;
            }
            if (maxBitsIntPrimeNumber == 8)
            {
                return 0xfb;
            }
            if (maxBitsIntPrimeNumber == 9)
            {
                return 0x1fd;
            }
            if (maxBitsIntPrimeNumber == 10)
            {
                return 0x3fd;
            }
            if (maxBitsIntPrimeNumber == 11)
            {
                return 0x7f7;
            }
            if (maxBitsIntPrimeNumber == 12)
            {
                return 0xffd;
            }
            if (maxBitsIntPrimeNumber == 13)
            {
                return 0x1fff;
            }
            if (maxBitsIntPrimeNumber == 14)
            {
                return 0x3ffd;
            }
            if (maxBitsIntPrimeNumber == 15)
            {
                return 0x7fed;
            }
            if (maxBitsIntPrimeNumber != 0x10)
            {
                throw new Exception("This algorithm does not hold a precalculated prime number of " + maxBitsIntPrimeNumber + " bits");
            }
            return 0xfff1;
        }

        public static int GetMaskedValue(int data, int nBitsFromStartToKeep, int nBitsFromEndToKeep)
        {
            int num = 0;
            int num2 = 0;
            int num3 = 0;
            for (int i = 0; i < nBitsFromStartToKeep; i++)
            {
                num2 |= ((int) 1) << (0x1f - i);
            }
            for (int j = 0; j < nBitsFromEndToKeep; j++)
            {
                num3 |= ((int) 1) << j;
            }
            num = num2 | num3;
            return (data & num);
        }

        public static byte GetNibble(byte byteValue, bool mostSignificantNibble)
        {
            if (mostSignificantNibble)
            {
                return (byte) (byteValue >> 4);
            }
            return (byte) (byteValue & 15);
        }

        public static byte ToByteNibble(byte byteValue)
        {
            return (byte) ((byteValue ^ (byteValue >> 4)) & 15);
        }

        public static byte ToByteNibblePair(byte firstByte, byte secondByte)
        {
            return (byte) ((ToByteNibble(firstByte) << 4) ^ ToByteNibble(secondByte));
        }

        public static int ToHashValue(byte[] data, int hashBitCount)
        {
            int[] numArray = new int[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                numArray[i] = data[i];
            }
            return ToHashValue(numArray, hashBitCount);
        }

        public static int ToHashValue(int[] data, int hashBitCount)
        {
            int num = 0;
            for (int i = 0; i < data.Length; i++)
            {
                int nBitsFromStartToKeep = (i * 3) % 0x20;
                num ^= (data[i] << nBitsFromStartToKeep) ^ (GetMaskedValue(data[i], nBitsFromStartToKeep, 0) >> (0x20 - i));
            }
            return ToHashValue(num, hashBitCount);
        }

        public static int ToHashValue(int data, int hashBitCount)
        {
            return ToHashValue(data, hashBitCount, true);
        }

        public static int ToHashValue(int data, int hashBitCount, bool usePrimeModulo)
        {
            int num;
            int num2 = ((int) 1) << hashBitCount;
            if (usePrimeModulo)
            {
                num = data % GetLargestPrimeValue(hashBitCount);
            }
            else
            {
                num = data;
                for (int i = 1; ((data >> (i * hashBitCount)) > 0) && ((i * hashBitCount) < 0x20); i++)
                {
                    num ^= data >> (i * hashBitCount);
                }
                num = num % num2;
            }
            while (num < 0)
            {
                num += num2;
            }
            return num;
        }
    }
}

