namespace PacketParser.CleartextDictionary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class BloomFilter
    {
        private BitArray bitArray;
        private int indexMask;
        private int nHashFunctions;
        private int tmpStatFilledValues;

        public BloomFilter(List<string> wordList)
        {
            int num = 0;
            while ((((int) 1) << num) < (14 * wordList.Count))
            {
                num++;
            }
            num++;
            int length = ((int) 1) << (num - 1);
            this.indexMask = length - 1;
            this.bitArray = new BitArray(length, false);
            this.nHashFunctions = (int) ((0.7 * length) / ((double) wordList.Count));
            foreach (string str in wordList)
            {
                this.AddWord(str);
            }
            for (int i = 0; i < this.bitArray.Length; i++)
            {
                if (this.bitArray[i])
                {
                    this.tmpStatFilledValues++;
                }
            }
        }

        private void AddWord(string word)
        {
            word = word.ToLower();
            foreach (int num in this.GetIndexes(word))
            {
                this.bitArray[num] = true;
            }
        }

        private int[] GetIndexes(string word)
        {
            int[] numArray = new int[this.nHashFunctions];
            for (int i = 0; i < numArray.Length; i++)
            {
                int hashCode = (word + i.ToString()).GetHashCode();
                numArray[i] = (hashCode ^ (hashCode >> 0x10)) & this.indexMask;
            }
            return numArray;
        }

        public bool HasWord(string word)
        {
            foreach (int num in this.GetIndexes(word))
            {
                if (!this.bitArray[num])
                {
                    return false;
                }
            }
            return true;
        }
    }
}

