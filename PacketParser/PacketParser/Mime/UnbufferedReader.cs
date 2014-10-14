namespace PacketParser.Mime
{
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    internal class UnbufferedReader
    {
        private Stream stream;

        public UnbufferedReader(Stream stream)
        {
            this.stream = stream;
            this.stream.Position = 0L;
        }

        public string ReadLine(int returnStringTruncateLength)
        {
            List<byte> list;
            byte[] pattern = new byte[] { 13, 10 };
            long num = KnuthMorrisPratt.ReadTo(pattern, this.stream, out list);
            if ((list.Count < 2) || (num < 0L))
            {
                return null;
            }
            StringBuilder builder = new StringBuilder(list.Count - 2);
            for (int i = 0; (i < (list.Count - 2)) && (builder.Length < returnStringTruncateLength); i++)
            {
                if (!char.IsControl((char)list[i]))
                {
                    builder.Append((char) list[i]);
                }
            }
            return builder.ToString();
        }

        public Stream BaseStream
        {
            get
            {
                return this.stream;
            }
        }

        public bool EndOfStream
        {
            get
            {
                return (this.stream.Position >= this.stream.Length);
            }
        }
    }
}

