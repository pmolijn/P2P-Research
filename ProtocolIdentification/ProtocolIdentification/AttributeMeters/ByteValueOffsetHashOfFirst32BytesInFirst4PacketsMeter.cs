namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class ByteValueOffsetHashOfFirst32BytesInFirst4PacketsMeter : IAttributeMeter
    {
        private readonly int hashSizeInBits = ((int) Math.Log((double) AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH, 2.0));

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 4)
            {
                goto Label_00BA;
            }
            int iteratorVariable0 = 0;
        Label_PostSwitchInIterator:;
            if ((iteratorVariable0 < Math.Min(packetLength, 0x20)) && ((packetStartIndex + iteratorVariable0) < frameData.Length))
            {
                yield return ConvertHelper.ToHashValue((int) (frameData[packetStartIndex + iteratorVariable0] + (0x100 * iteratorVariable0)), this.hashSizeInBits);
                iteratorVariable0++;
                goto Label_PostSwitchInIterator;
            }
        Label_00BA:;
        }

        public string AttributeName
        {
            get
            {
                return "ByteValueOffsetHashOfFirst32BytesInFirst4PacketsMeter";
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

