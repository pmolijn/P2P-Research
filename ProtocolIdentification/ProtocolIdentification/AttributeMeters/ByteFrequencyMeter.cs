namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class ByteFrequencyMeter : IAttributeMeter
    {
        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 5)
            {
                goto Label_0091;
            }
            int index = packetStartIndex;
        Label_PostSwitchInIterator:;
            if ((index < (packetStartIndex + packetLength)) && (index < frameData.Length))
            {
                yield return (frameData[index] % AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH);
                index++;
                goto Label_PostSwitchInIterator;
            }
        Label_0091:;
        }

        public string AttributeName
        {
            get
            {
                return "ByteFrequencyMeter";
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

