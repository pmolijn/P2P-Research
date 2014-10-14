namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First2PacketsFirst16ByteHashCountsMeter : IAttributeMeter
    {
        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 2)
            {
                int iteratorVariable0 = 0x10;
                byte[] iteratorVariable1 = new byte[iteratorVariable0];
                for (int j = 0; ((j < 0x10) && ((j + packetStartIndex) < frameData.Length)) && (j < packetLength); j++)
                {
                    int index = ConvertHelper.ToHashValue(frameData[packetStartIndex + j], 4);
                    iteratorVariable1[index] = (byte) (iteratorVariable1[index] + 1);
                }
                for (int i = 0; i < iteratorVariable1.Length; i++)
                {
                    yield return ((i << 4) ^ iteratorVariable1[i]);
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "First2PacketsFirst16ByteHashCountsMeter";
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

