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

    internal class ArpPacket : AbstractPacket
    {
        private byte hardwareLength;
        private ushort harwareType;
        private ushort operation;
        private byte protocolLength;
        private ushort protocolType;
        private byte[] senderHardwareAddress;
        private byte[] senderProtocolAddress;
        private byte[] targetHardwareAddress;
        private byte[] targetProtocolAddress;

        internal ArpPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "ARP")
        {
            this.harwareType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex);
            if ((this.harwareType != 1) && !base.ParentFrame.QuickParse)
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex, packetStartIndex + 1, "ARP HardwareType not Ethernet"));
            }
            this.protocolType = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 2);
            if ((this.protocolType != 0x800) && !base.ParentFrame.QuickParse)
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 2, packetStartIndex + 3, "ARP ProtocolType not IPv4"));
            }
            this.hardwareLength = parentFrame.Data[packetStartIndex + 4];
            if ((this.hardwareLength != 6) && !base.ParentFrame.QuickParse)
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 4, packetStartIndex + 4, "ARP HardwareLength<>6 (not Ethernet)"));
            }
            this.senderHardwareAddress = new byte[this.hardwareLength];
            this.targetHardwareAddress = new byte[this.hardwareLength];
            this.protocolLength = parentFrame.Data[packetStartIndex + 5];
            if ((this.protocolLength != 4) && !base.ParentFrame.QuickParse)
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 5, packetStartIndex + 5, "ARP ProtocolLength<>4 (not IPv4) (it is: " + this.protocolLength + ")"));
            }
            this.senderProtocolAddress = new byte[this.protocolLength];
            this.targetProtocolAddress = new byte[this.protocolLength];
            this.operation = ByteConverter.ToUInt16(parentFrame.Data, packetStartIndex + 6);
            if (((this.operation != 1) && (this.operation != 2)) && !base.ParentFrame.QuickParse)
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 6, packetStartIndex + 7, "ARP Operation not Request nor Reply"));
            }
            if (!this.TryCopy(parentFrame.Data, packetStartIndex + 8, this.senderHardwareAddress, 0, this.hardwareLength))
            {
                if (!base.ParentFrame.QuickParse)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, packetStartIndex + 8, (packetStartIndex + 8) + this.hardwareLength, "Error retrieving sender hardware address from ARP packet"));
                }
            }
            else if (!this.TryCopy(parentFrame.Data, (packetStartIndex + 8) + this.hardwareLength, this.senderProtocolAddress, 0, this.protocolLength))
            {
                if (!base.ParentFrame.QuickParse)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, (packetStartIndex + 8) + this.hardwareLength, ((packetStartIndex + 8) + this.hardwareLength) + this.protocolLength, "Error retrieving sender protocol address from ARP packet"));
                }
            }
            else if (!this.TryCopy(parentFrame.Data, ((packetStartIndex + 8) + this.hardwareLength) + this.protocolLength, this.targetHardwareAddress, 0, this.hardwareLength))
            {
                if (!base.ParentFrame.QuickParse)
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, ((packetStartIndex + 8) + this.hardwareLength) + this.protocolLength, ((packetStartIndex + 8) + (2 * this.hardwareLength)) + this.protocolLength, "Error retrieving target hardware address from ARP packet"));
                }
            }
            else if (!this.TryCopy(parentFrame.Data, ((packetStartIndex + 8) + (2 * this.hardwareLength)) + this.protocolLength, this.targetProtocolAddress, 0, this.protocolLength) && !base.ParentFrame.QuickParse)
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, ((packetStartIndex + 8) + (2 * this.hardwareLength)) + this.protocolLength, ((packetStartIndex + 8) + (2 * this.hardwareLength)) + (2 * this.protocolLength), "Error retrieving target protocol address from ARP packet"));
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

        private bool TryCopy(Array sourceArray, int sourceIndex, Array destinationArray, int destinationIndex, int length)
        {
            if (((sourceIndex < 0) || (sourceIndex >= sourceArray.Length)) || ((sourceIndex + length) > sourceArray.Length))
            {
                return false;
            }
            if ((destinationIndex < 0) || ((destinationIndex + length) > destinationArray.Length))
            {
                return false;
            }
            try
            {
                Array.Copy(sourceArray, sourceIndex, destinationArray, destinationIndex, length);
            }
            catch
            {
                return false;
            }
            return true;
        }

        internal PhysicalAddress SenderHardwareAddress
        {
            get
            {
                return new PhysicalAddress(this.senderHardwareAddress);
            }
        }

        internal IPAddress SenderIPAddress
        {
            get
            {
                try
                {
                    return new IPAddress(this.senderProtocolAddress);
                }
                catch
                {
                    return null;
                }
            }
        }

        internal PhysicalAddress TargetHardwareAddress
        {
            get
            {
                return new PhysicalAddress(this.targetHardwareAddress);
            }
        }

        internal IPAddress TargetIPAddress
        {
            get
            {
                try
                {
                    return new IPAddress(this.targetProtocolAddress);
                }
                catch
                {
                    return null;
                }
            }
        }

    }
}

