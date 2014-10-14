namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class FirstBitPositionsMeter : IAttributeMeter
    {
        private readonly int nBytesToCheck = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 0x10);
        private readonly int oneValueOffset = 0;
        private readonly int zeroValueOffset = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if ((packetOrderNumberInSession < 8) && (frameData.Length > packetStartIndex))
            {
                byte[] destinationArray = new byte[Math.Min(Math.Min(packetLength, this.nBytesToCheck), frameData.Length - packetStartIndex)];
                Array.Copy(frameData, packetStartIndex, destinationArray, 0, destinationArray.Length);
                BitArray iteratorVariable1 = new BitArray(destinationArray);
                for (int i = 0; i < iteratorVariable1.Length; i++)
                {
                    if (iteratorVariable1[i])
                    {
                        yield return (this.oneValueOffset + i);
                    }
                    else
                    {
                        yield return (this.zeroValueOffset + i);
                    }
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "FirstBitPositionsMeter";
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

