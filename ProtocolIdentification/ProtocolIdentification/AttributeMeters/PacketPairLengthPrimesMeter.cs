namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class PacketPairLengthPrimesMeter : IAttributeMeter
    {
        private uint previousPacketPairLengthPrime = 0;
        private readonly int[] PRIME_NUMBERS = new int[] { 
            2, 3, 5, 7, 11, 13, 0x11, 0x13, 0x17, 0x1d, 0x1f, 0x25, 0x29, 0x2b, 0x2f, 0x35, 
            0x3b, 0x3d, 0x43, 0x47, 0x49, 0x4f, 0x53, 0x59
         };

        IEnumerable<int> IAttributeMeter.GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            this.previousPacketPairLengthPrime = this.previousPacketPairLengthPrime >> 4;
            uint iteratorVariable0 = 0;
            for (int j = 0; j < 4; j++)
            {
                if ((packetLength % this.PRIME_NUMBERS[j]) == 0)
                {
                    iteratorVariable0 |= ((uint) 1) << j;
                }
            }
            this.previousPacketPairLengthPrime |= iteratorVariable0 << 4;
            yield return (int) this.previousPacketPairLengthPrime;
        }

        public bool IsStateful
        {
            get
            {
                return true;
            }
        }

        string IAttributeMeter.AttributeName
        {
            get
            {
                return "PacketPairLengthPrimesMeter";
            }
        }

    }
}

