namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class PacketLengthDistributionMeter : IAttributeMeter
    {
        IEnumerable<int> IAttributeMeter.GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            yield return (packetLength % AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH);
        }

        public bool IsStateful
        {
            get
            {
                return false;
            }
        }

        string IAttributeMeter.AttributeName
        {
            get
            {
                return "PacketLengthDistributionMeter";
            }
        }

    }
}

