namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First2PacketsFirst3ByteHashAndPacketLengthMeter : IAttributeMeter
    {
        private readonly int expectedMaxPacketLength = 0x5dc;
        private readonly double exponent;

        public First2PacketsFirst3ByteHashAndPacketLengthMeter()
        {
            this.exponent = Math.Log(16.0) / Math.Log((double) this.expectedMaxPacketLength);
        }

        private int GetLengthBinOffset(int packetLength)
        {
            return Math.Min(15, (int) Math.Pow((double) packetLength, this.exponent));
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 2)
            {
                goto Label_00D1;
            }
            int lengthBinOffset = this.GetLengthBinOffset(packetLength);
            int iteratorVariable1 = 0;
        Label_PostSwitchInIterator:;
            if (((iteratorVariable1 < 3) && ((packetStartIndex + iteratorVariable1) < frameData.Length)) && (iteratorVariable1 < packetLength))
            {
                int iteratorVariable2 = ConvertHelper.ToHashValue(frameData[packetStartIndex + iteratorVariable1], 4);
                yield return ((iteratorVariable2 << 4) + lengthBinOffset);
                iteratorVariable1++;
                goto Label_PostSwitchInIterator;
            }
        Label_00D1:;
        }

        public string AttributeName
        {
            get
            {
                return "First2PacketsFirst3ByteHashAndPacketLengthMeter";
            }
        }

        public bool IsStateful
        {
            get
            {
                return false;
            }
        }

    }
}

