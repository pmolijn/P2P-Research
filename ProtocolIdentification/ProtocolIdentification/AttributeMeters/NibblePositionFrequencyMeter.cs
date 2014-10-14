namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class NibblePositionFrequencyMeter : IAttributeMeter
    {
        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 8)
            {
                goto Label_00EA;
            }
            int iteratorVariable0 = 0;
        Label_PostSwitchInIterator:;
            if ((((packetStartIndex + (iteratorVariable0 / 2)) < frameData.Length) && ((iteratorVariable0 / 2) < packetLength)) && (iteratorVariable0 < (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 0x10)))
            {
                int index = packetStartIndex + (iteratorVariable0 / 2);
                bool mostSignificantNibble = (iteratorVariable0 % 2) == 0;
                byte nibble = ConvertHelper.GetNibble(frameData[index], mostSignificantNibble);
                yield return ((iteratorVariable0 * 0x10) + nibble);
                iteratorVariable0++;
                goto Label_PostSwitchInIterator;
            }
        Label_00EA:;
        }

        public string AttributeName
        {
            get
            {
                return "NibblePositionFrequencyMeter";
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

