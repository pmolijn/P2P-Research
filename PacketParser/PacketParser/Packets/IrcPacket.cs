namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public class IrcPacket : AbstractPacket, ISessionPacket
    {
        private const int MAX_MESSAGE_LENGTH = 510;
        private List<Message> messages;
        private bool packetHeaderIsComplete;
        private int parsedBytesCount;

        private IrcPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "IRC")
        {
            this.parsedBytesCount = 0;
            this.packetHeaderIsComplete = false;
            this.messages = new List<Message>();
            int index = packetStartIndex;
            while (index < packetEndIndex)
            {
                byte[] prefix = null;
                if (parentFrame.Data[index] == 0x3a)
                {
                    prefix = ByteConverter.ToByteArray(parentFrame.Data, ref index, (byte) 0x20, false);
                }
                while (parentFrame.Data[index] == 0x20)
                {
                    index++;
                }
                byte[] command = ByteConverter.ToByteArray(parentFrame.Data, ref index, (byte) 0x20, false);
                while (parentFrame.Data[index] == 0x20)
                {
                    index++;
                }
                byte[] endValues = new byte[] { 13, 10 };
                byte[] sourceArray = ByteConverter.ToByteArray(parentFrame.Data, ref index, endValues, false);
                if (index < parentFrame.Data.Length)
                {
                    if (parentFrame.Data[index] == 10)
                    {
                        index++;
                    }
                    this.packetHeaderIsComplete = true;
                    this.parsedBytesCount = index - packetStartIndex;
                }
                else if (this.parsedBytesCount > 0)
                {
                    return;
                }
                LinkedList<byte[]> parameters = new LinkedList<byte[]>();
                int length = 0;
                while (length < sourceArray.Length)
                {
                    if (sourceArray[length] == 0x3a)
                    {
                        byte[] destinationArray = new byte[(sourceArray.Length - length) - 1];
                        Array.Copy(sourceArray, length + 1, destinationArray, 0, destinationArray.Length);
                        parameters.AddLast(destinationArray);
                        length = sourceArray.Length;
                    }
                    else
                    {
                        parameters.AddLast(ByteConverter.ToByteArray(sourceArray, ref length, (byte) 0x20, false));
                        while ((length < sourceArray.Length) && (sourceArray[length] == 0x20))
                        {
                            length++;
                        }
                    }
                }
                this.messages.Add(new Message(prefix, command, parameters));
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            char c = (char) parentFrame.Data[packetStartIndex];
            if ((!char.IsDigit(c) && !char.IsLetter(c)) && (c != ':'))
            {
                return false;
            }
            if ((Array.IndexOf<byte>(parentFrame.Data, 10, packetStartIndex) == -1) && ((packetEndIndex - packetStartIndex) > 510))
            {
                return false;
            }
            int num = Array.IndexOf<byte>(parentFrame.Data, 13, packetStartIndex);
            if (((num >= packetEndIndex) || ((num != -1) && (parentFrame.Data[num + 1] != 10))) && ((packetEndIndex - packetStartIndex) > 510))
            {
                return false;
            }
            try
            {
                result = new IrcPacket(parentFrame, packetStartIndex, packetEndIndex);
            }
            catch
            {
                result = null;
            }
            if (result == null)
            {
                return false;
            }
            return true;
        }

        public ICollection<Message> Messages
        {
            get
            {
                return this.messages;
            }
        }

        public bool PacketHeaderIsComplete
        {
            get
            {
                return this.packetHeaderIsComplete;
            }
        }

        public int ParsedBytesCount
        {
            get
            {
                return this.parsedBytesCount;
            }
        }


        private enum ircChars : byte
        {
            Colon = 0x3a,
            CR = 13,
            LF = 10,
            Nul = 0,
            Space = 0x20
        }

        public class Message
        {
            private byte[] command;
            private ICollection<byte[]> parameters;
            private byte[] prefix;

            internal Message(byte[] prefix, byte[] command, ICollection<byte[]> parameters)
            {
                this.prefix = prefix;
                this.command = command;
                this.parameters = parameters;
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                if ((this.prefix != null) && (this.prefix.Length > 0))
                {
                    builder.Append(":" + this.Prefix);
                }
                builder.Append(" " + this.Command);
                foreach (string str in this.Parameters)
                {
                    builder.Append(" " + str);
                }
                return builder.ToString();
            }

            public string Command
            {
                get
                {
                    if (this.command == null)
                    {
                        return null;
                    }
                    return ByteConverter.ReadString(this.command);
                }
            }

            public IEnumerable<string> Parameters
            {
                get
                {
                    foreach (byte[] iteratorVariable0 in this.parameters)
                    {
                        yield return ByteConverter.ReadString(iteratorVariable0);
                    }
                }
            }

            public string Prefix
            {
                get
                {
                    if (this.prefix == null)
                    {
                        return null;
                    }
                    return ByteConverter.ReadString(this.prefix);
                }
            }

        }
    }
}

