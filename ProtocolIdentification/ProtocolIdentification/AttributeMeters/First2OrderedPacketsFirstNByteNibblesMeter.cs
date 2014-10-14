namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First2OrderedPacketsFirstNByteNibblesMeter : IAttributeMeter
    {
        private readonly int nBytesToParsePerPacket = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 0x20);
        private readonly int packetOrderOffsetIncrement = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 2)
            {
                goto Label_00CE;
            }
            int iteratorVariable0 = 0;
        Label_PostSwitchInIterator:;
            if ((iteratorVariable0 < Math.Min(this.nBytesToParsePerPacket, packetLength)) && ((packetStartIndex + iteratorVariable0) < frameData.Length))
            {
                yield return ((((packetOrderNumberInSession * this.packetOrderOffsetIncrement) + (iteratorVariable0 * 0x10)) + ConvertHelper.ToByteNibble(frameData[packetStartIndex + iteratorVariable0])) % AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH);
                iteratorVariable0++;
                goto Label_PostSwitchInIterator;
            }
        Label_00CE:;
        }

        public string AttributeName
        {
            get
            {
                return "First2OrderedPacketsFirstNByteNibblesMeter";
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

