namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;

    internal class First2OrderedFirst4CharWordsMeter : IAttributeMeter
    {
        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession >= 2)
            {
                yield break;
            }
            StringBuilder iteratorVariable0 = new StringBuilder(4);
            for (int j = 0; ((iteratorVariable0.Length < 4) && (j < packetLength)) && ((packetStartIndex + j) < frameData.Length); j++)
            {
                iteratorVariable0.Append(frameData[packetStartIndex + j]);
            }
            yield return ConvertHelper.ToHashValue((iteratorVariable0.ToString() + packetDirection.ToString()).GetHashCode(), 8);
        }

        public string AttributeName
        {
            get
            {
                return "First2OrderedFirst4CharWordsMeter";
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

