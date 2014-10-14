namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class PacketLengthDistributionMeterFirst3 : IAttributeMeter
    {
        private const int INSPECTED_PACKETS = 3;

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 3)
            {
                yield break;
            }
            yield return this.GetPacketBinNumber(packetLength, packetOrderNumberInSession);
            yield return this.GetPacketBinNumber(packetLength / 3, packetOrderNumberInSession);
            yield return this.GetPacketBinNumber(packetLength / 5, packetOrderNumberInSession);
        }

        private int GetPacketBinNumber(int packetLength, int packetNumber)
        {
            return ((packetLength % (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 3)) + (packetNumber * (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 3)));
        }

        public string AttributeName
        {
            get
            {
                return "PacketLengthDistributionMeterFirst3";
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

