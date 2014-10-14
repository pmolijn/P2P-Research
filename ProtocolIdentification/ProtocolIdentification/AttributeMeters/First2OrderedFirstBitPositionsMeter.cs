namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First2OrderedFirstBitPositionsMeter : IAttributeMeter
    {
        private readonly int nBytesToCheck = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 0x20);
        private readonly int oneValueOffset = 0;
        private readonly int packetNumberIncrement = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);
        private readonly int zeroValueOffset = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 4);

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 2)
            {
                byte[] destinationArray = new byte[Math.Max(0, Math.Min(Math.Min(packetLength, this.nBytesToCheck), frameData.Length - packetStartIndex))];
                if (destinationArray.Length > 0)
                {
                    Array.Copy(frameData, packetStartIndex, destinationArray, 0, destinationArray.Length);
                    BitArray iteratorVariable1 = new BitArray(destinationArray);
                    for (int i = 0; i < iteratorVariable1.Length; i++)
                    {
                        if (iteratorVariable1[i])
                        {
                            yield return (((packetOrderNumberInSession * this.packetNumberIncrement) + this.oneValueOffset) + i);
                        }
                        else
                        {
                            yield return (((packetOrderNumberInSession * this.packetNumberIncrement) + this.zeroValueOffset) + i);
                        }
                    }
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "First2OrderedFirstBitPositionsMeter";
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

