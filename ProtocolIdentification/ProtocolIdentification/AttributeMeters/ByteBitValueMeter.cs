namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class ByteBitValueMeter : IAttributeMeter
    {
        private readonly int counterValueModulus = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 0x10);
        private const int N_BYTES_TO_PARSE_PER_PACKET = 0x20;
        private const int N_PACKETS_TO_PARSE_PER_SESSION = 8;

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 8)
            {
                int iteratorVariable3;
                int[] iteratorVariable0 = new int[8];
                byte[] destinationArray = new byte[Math.Max(0, Math.Min(Math.Min(packetLength, 0x20), frameData.Length - packetStartIndex))];
                if (destinationArray.Length <= 0)
                {
                    yield break;
                }
                Array.Copy(frameData, packetStartIndex, destinationArray, 0, destinationArray.Length);
                BitArray iteratorVariable2 = new BitArray(destinationArray);
                for (int j = 0; j < iteratorVariable2.Length; j++)
                {
                    if (!iteratorVariable2[j])
                    {
                        iteratorVariable0[j % 8]++;
                    }
                }
                if (packetDirection == AttributeFingerprintHandler.PacketDirection.ClientToServer)
                {
                    iteratorVariable3 = 0;
                }
                else
                {
                    if (packetDirection != AttributeFingerprintHandler.PacketDirection.ServerToClient)
                    {
                        yield break;
                    }
                    iteratorVariable3 = 8;
                }
                for (int i = 0; i < 8; i++)
                {
                    yield return ((this.counterValueModulus * (iteratorVariable3 + i)) + ((iteratorVariable0[i] * (this.counterValueModulus - 1)) / destinationArray.Length));
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "ByteBitValueMeter";
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

