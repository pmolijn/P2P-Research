namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First4OrderedDirectionPacketSizeMeter : IAttributeMeter
    {
        private readonly SortedList<AttributeFingerprintHandler.PacketDirection, int> directionOffset = new SortedList<AttributeFingerprintHandler.PacketDirection, int>(2);
        private readonly int largestExpectedPacketSize = 0x5dc;
        private readonly int packetOrderIncrement;
        private readonly double packetSizeBinExponent;

        public First4OrderedDirectionPacketSizeMeter()
        {
            this.directionOffset[AttributeFingerprintHandler.PacketDirection.ClientToServer] = 0;
            this.directionOffset[AttributeFingerprintHandler.PacketDirection.ServerToClient] = AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2;
            this.packetOrderIncrement = AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 8;
            this.packetSizeBinExponent = Math.Log((double) this.packetOrderIncrement) / Math.Log((double) this.largestExpectedPacketSize);
        }

        private int GetBinOffsetNumber(int packetLength)
        {
            return Math.Min(this.packetOrderIncrement - 1, (int) Math.Pow((double) packetLength, this.packetSizeBinExponent));
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if ((packetOrderNumberInSession >= 4) || (packetDirection == AttributeFingerprintHandler.PacketDirection.Unknown))
            {
                yield break;
            }
            yield return ((this.directionOffset[packetDirection] + (packetOrderNumberInSession * this.packetOrderIncrement)) + this.GetBinOffsetNumber(packetLength));
        }

        public string AttributeName
        {
            get
            {
                return "First4OrderedDirectionPacketSizeMeter";
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

