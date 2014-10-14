namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First4OrderedDirectionInterPacketDelayMeter : IAttributeMeter
    {
        private double delayBinExponent;
        private SortedList<AttributeFingerprintHandler.PacketDirection, int> directionOffset = new SortedList<AttributeFingerprintHandler.PacketDirection, int>(3);
        private int largestMicroSecondTimeValue = 0x3938700;
        private DateTime lastPacketTimestamp = DateTime.MinValue;
        private int packetOrderIncrement;
        private int smallestMicroSecondTimeValue = 0x10;

        public First4OrderedDirectionInterPacketDelayMeter()
        {
            this.directionOffset[AttributeFingerprintHandler.PacketDirection.ClientToServer] = 0;
            this.directionOffset[AttributeFingerprintHandler.PacketDirection.ServerToClient] = AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2;
            this.packetOrderIncrement = AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 8;
            this.delayBinExponent = Math.Log((double) this.packetOrderIncrement) / Math.Log((double) this.largestMicroSecondTimeValue);
        }

        private int GetBinOffsetNumber(TimeSpan interPacketDelay)
        {
            return Math.Min(this.packetOrderIncrement - 1, (int) Math.Pow(Math.Max((double) 0.0, (double) ((10.0 * interPacketDelay.Ticks) - this.smallestMicroSecondTimeValue)), this.delayBinExponent));
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            TimeSpan interPacketDelay = packetTimestamp.Subtract(this.lastPacketTimestamp);
            this.lastPacketTimestamp = packetTimestamp;
            if ((packetOrderNumberInSession >= 4) || (packetDirection == AttributeFingerprintHandler.PacketDirection.Unknown))
            {
                yield break;
            }
            yield return ((this.directionOffset[packetDirection] + (packetOrderNumberInSession * this.packetOrderIncrement)) + this.GetBinOffsetNumber(interPacketDelay));
        }

        public string AttributeName
        {
            get
            {
                return "First4OrderedDirectionInterPacketDelayMeter";
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

