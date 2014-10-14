namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First4PacketsFirst32BytesEqualityMeter : IAttributeMeter
    {
        private byte[] latest32Bytes;

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 4)
            {
                byte[] destinationArray = new byte[0x20];
                if (frameData.Length > packetStartIndex)
                {
                    Array.Copy(frameData, packetStartIndex, destinationArray, 0, Math.Min(0x20, frameData.Length - packetStartIndex));
                    if (packetOrderNumberInSession > 0)
                    {
                        BitArray iteratorVariable1 = new BitArray(0x20);
                        for (int j = 0; j < 0x20; j++)
                        {
                            iteratorVariable1[j] = this.latest32Bytes[j] == destinationArray[j];
                        }
                        int[] array = new int[1];
                        iteratorVariable1.CopyTo(array, 0);
                        yield return ConvertHelper.ToHashValue(array[0], 8, true);
                    }
                    this.latest32Bytes = destinationArray;
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "First4PacketsFirst32BytesEqualityMeter";
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

