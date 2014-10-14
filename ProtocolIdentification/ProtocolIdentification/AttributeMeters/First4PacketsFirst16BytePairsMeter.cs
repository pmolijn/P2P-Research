namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First4PacketsFirst16BytePairsMeter : IAttributeMeter
    {
        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 4)
            {
                goto Label_00E3;
            }
            int iteratorVariable0 = 1;
        Label_PostSwitchInIterator:;
            if ((((packetStartIndex + iteratorVariable0) < frameData.Length) && (iteratorVariable0 < packetLength)) && (iteratorVariable0 < 0x11))
            {
                int data = frameData[(packetStartIndex + iteratorVariable0) - 1];
                data = data << 8;
                data += frameData[packetStartIndex + iteratorVariable0];
                yield return ConvertHelper.ToHashValue(data, 8);
                iteratorVariable0++;
                goto Label_PostSwitchInIterator;
            }
        Label_00E3:;
        }

        public string AttributeName
        {
            get
            {
                return "First4PacketsFirst16BytePairsMeter";
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

