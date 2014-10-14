namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class AccumulatedDirectionBytesMeter : IAttributeMeter
    {
        private int accumulatedBytesCount = 0;
        private const int BYTE_CHUNK_UNIT_SIZE = 0x40;
        private int directionChanges = 0;
        private AttributeFingerprintHandler.PacketDirection lastDirection = AttributeFingerprintHandler.PacketDirection.Unknown;

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if ((this.directionChanges >= 4) || ((packetDirection == this.lastDirection) && ((this.accumulatedBytesCount / 0x40) >= (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 4))))
            {
                yield break;
            }
            if (packetDirection != this.lastDirection)
            {
                if (this.lastDirection != AttributeFingerprintHandler.PacketDirection.Unknown)
                {
                    this.directionChanges++;
                    if (this.directionChanges >= 4)
                    {
                        yield break;
                    }
                }
                this.lastDirection = packetDirection;
                this.accumulatedBytesCount = 0;
            }
            this.accumulatedBytesCount += packetLength;
            yield return ((this.directionChanges << 6) + Math.Min((int) (this.accumulatedBytesCount / 0x40), (int) ((AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 4) - 1)));
        }

        public string AttributeName
        {
            get
            {
                return "AccumulatedDirectionBytesMeter";
            }
        }

        public bool IsStateful
        {
            get
            {
                return true;
            }
        }

    }
}

