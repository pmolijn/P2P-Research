namespace PacketParser.CleartextDictionary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;

    public class WordDictionary
    {
        private BloomFilter bloomFilter;
        private BitArray byteLetters = new BitArray(0x100);
        private int longestWord = 0;
        private int minWordLength = 3;

        private void AddWord(string word, List<string> wordList)
        {
            if (word.Length >= this.minWordLength)
            {
                wordList.Add(word.ToLower());
                foreach (char ch in word)
                {
                    this.byteLetters[(byte) ch] = true;
                }
                foreach (char ch2 in word.ToUpper())
                {
                    this.byteLetters[(byte) ch2] = true;
                }
                if (word.Length > this.longestWord)
                {
                    this.longestWord = word.Length;
                }
            }
        }

        internal bool HasWord(string word)
        {
            word = word.ToLower();
            return (((word.Length <= this.longestWord) && (word.Length >= this.minWordLength)) && this.bloomFilter.HasWord(word));
        }

        internal bool IsLetter(byte b)
        {
            return this.byteLetters[b];
        }

        public void LoadDictionaryFile(string dictionaryFile)
        {
            List<string> wordList = new List<string>();
            FileStream stream = new FileStream(dictionaryFile, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(stream);
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();
                char[] separator = new char[] { ' ', ',', '.', ' ', '!', '?', '<', '>', '(', ')', '{', '}', '[', ']', '"', '\'' };
                foreach (string str2 in str.Split(separator))
                {
                    this.AddWord(str2, wordList);
                }
            }
            this.bloomFilter = new BloomFilter(wordList);
        }

        internal int LongestWord
        {
            get
            {
                return this.longestWord;
            }
        }
    }
}

