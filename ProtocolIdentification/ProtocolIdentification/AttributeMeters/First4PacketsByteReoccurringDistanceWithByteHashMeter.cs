namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First4PacketsByteReoccurringDistanceWithByteHashMeter : IAttributeMeter
    {
        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 4)
            {
                int hashBitCount = 4;
                int[] iteratorVariable1 = new int[0x100];
                for (int j = 0; j < iteratorVariable1.Length; j++)
                {
                    iteratorVariable1[j] = -2147483648;
                }
                for (int i = 0; ((i < packetLength) && ((packetStartIndex + i) < frameData.Length)) && (i < 0x20); i++)
                {
                    int iteratorVariable3 = (packetStartIndex + i) - iteratorVariable1[frameData[packetStartIndex + i]];
                    if ((iteratorVariable3 > 0) && (iteratorVariable3 < 0x11))
                    {
                        int iteratorVariable4 = ConvertHelper.ToHashValue(frameData[packetStartIndex + i], hashBitCount);
                        yield return (((iteratorVariable4 << 4) + iteratorVariable3) - 1);
                    }
                    iteratorVariable1[frameData[packetStartIndex + i]] = packetStartIndex + i;
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "First4PacketsByteReoccurringDistanceWithByteHashMeter";
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

