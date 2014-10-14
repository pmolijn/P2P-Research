namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class ByteFrequencyOfFirstPacketBytesMeter : IAttributeMeter
    {
        private const int MAX_BYTES_TO_PARSE = 100;

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 8)
            {
                goto Label_009E;
            }
            int index = packetStartIndex;
        Label_PostSwitchInIterator:;
            if (((index < (packetStartIndex + packetLength)) && (index < frameData.Length)) && (index < 100))
            {
                yield return (frameData[index] % AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH);
                index++;
                goto Label_PostSwitchInIterator;
            }
        Label_009E:;
        }

        public string AttributeName
        {
            get
            {
                return "ByteFrequencyOfFirstPacketBytesMeter";
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

