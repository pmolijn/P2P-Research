namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Net;
    using System.Net.NetworkInformation;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class DhcpPacket : AbstractPacket
    {
        private IPAddress clientIpAddress;
        private PhysicalAddress clientMacAddress;
        private byte dhcpMessageType;
        private IPAddress gatewayIpAddress;
        private OpCodeValue opCode;
        private List<Option> optionList;
        private ushort secondsElapsed;
        private IPAddress serverIpAddress;
        private uint transactionID;
        private IPAddress yourIpAddress;

        internal DhcpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "DHCP (Bootstrap protocol)")
        {
            Option option;
            this.opCode = (OpCodeValue) parentFrame.Data[packetStartIndex];
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("OpCode", this.OpCode.ToString());
            }
            this.transactionID = ByteConverter.ToUInt32(parentFrame.Data, packetStartIndex + 4);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Transaction ID", "0x" + this.transactionID.ToString("X2"));
            }
            this.secondsElapsed = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 8);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Seconds elapsed", this.secondsElapsed.ToString());
            }
            byte[] destinationArray = new byte[4];
            Array.ConstrainedCopy(parentFrame.Data, packetStartIndex + 12, destinationArray, 0, 4);
            this.clientIpAddress = new IPAddress(destinationArray);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Client IP Address", this.clientIpAddress.ToString());
            }
            Array.ConstrainedCopy(parentFrame.Data, packetStartIndex + 0x10, destinationArray, 0, 4);
            this.yourIpAddress = new IPAddress(destinationArray);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Your IP Address", this.yourIpAddress.ToString());
            }
            Array.ConstrainedCopy(parentFrame.Data, packetStartIndex + 20, destinationArray, 0, 4);
            this.serverIpAddress = new IPAddress(destinationArray);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Server IP Address", this.serverIpAddress.ToString());
            }
            Array.ConstrainedCopy(parentFrame.Data, packetStartIndex + 0x18, destinationArray, 0, 4);
            this.gatewayIpAddress = new IPAddress(destinationArray);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Gateway IP Address", this.gatewayIpAddress.ToString());
            }
            byte[] buffer2 = new byte[6];
            Array.ConstrainedCopy(parentFrame.Data, packetStartIndex + 0x1c, buffer2, 0, 6);
            this.clientMacAddress = new PhysicalAddress(buffer2);
            if (!base.ParentFrame.QuickParse)
            {
                base.Attributes.Add("Client MAC Address", this.clientMacAddress.ToString());
            }
            this.optionList = new List<Option>();
            for (int i = packetStartIndex + 240; i < packetEndIndex; i += option.OptionValue.Length + 2)
            {
                option = new Option(parentFrame.Data, i);
                if (option.OptionCode == 0xff)
                {
                    return;
                }
                if (option.OptionCode == 3)
                {
                    this.gatewayIpAddress = new IPAddress(option.OptionValue);
                }
                else if (((option.OptionCode == 0x35) && (option.OptionValue != null)) && (option.OptionValue.Length == 1))
                {
                    this.dhcpMessageType = option.OptionValue[0];
                }
                this.optionList.Add(option);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("DHCP Options", option.OptionCode.ToString());
                }
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

        internal IPAddress ClientIpAddress
        {
            get
            {
                return this.clientIpAddress;
            }
        }

        internal PhysicalAddress ClientMacAddress
        {
            get
            {
                return this.clientMacAddress;
            }
        }

        internal byte DhcpMessageType
        {
            get
            {
                return this.dhcpMessageType;
            }
        }

        internal IPAddress GatewayIpAddress
        {
            get
            {
                return this.gatewayIpAddress;
            }
        }

        internal OpCodeValue OpCode
        {
            get
            {
                return this.opCode;
            }
        }

        internal IList<Option> OptionList
        {
            get
            {
                return this.optionList;
            }
        }

        internal ushort SecondsElapsed
        {
            get
            {
                return this.secondsElapsed;
            }
        }

        internal IPAddress ServerIpAddress
        {
            get
            {
                return this.serverIpAddress;
            }
        }

        internal uint TransactionID
        {
            get
            {
                return this.transactionID;
            }
        }

        internal IPAddress YourIpAddress
        {
            get
            {
                return this.yourIpAddress;
            }
        }


        internal enum OpCodeValue : byte
        {
            BootReply = 2,
            BootRequest = 1
        }

        internal class Option
        {
            private byte dataLength;
            private byte optionCode;
            private byte[] value;

            internal Option(byte[] frameData, int optionStartIndex)
            {
                this.optionCode = frameData[optionStartIndex];
                this.dataLength = frameData[optionStartIndex + 1];
                this.value = new byte[this.dataLength];
                Array.ConstrainedCopy(frameData, optionStartIndex + 2, this.value, 0, this.dataLength);
            }

            internal byte OptionCode
            {
                get
                {
                    return this.optionCode;
                }
            }

            internal byte[] OptionValue
            {
                get
                {
                    return this.value;
                }
            }
        }
    }
}

