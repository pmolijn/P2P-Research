namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class BytePairsReocurringIn32FirstBytesMeter : IAttributeMeter
    {
        private List<ushort> bytePairsFromPreviousPacket = new List<ushort>();

        private ushort ConvertToUshort(byte b1, byte b2)
        {
            ushort num = b1;
            num = (ushort) (num << 8);
            return (ushort) (num + b2);
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            List<ushort> iteratorVariable0 = new List<ushort>();
            for (int i = 0; ((((packetStartIndex + (2 * i)) + 1) < frameData.Length) && (((2 * i) + 1) < packetLength)) && (i < 0x10); i++)
            {
                ushort item = this.ConvertToUshort(frameData[packetStartIndex + (i * 2)], frameData[(packetStartIndex + (i * 2)) + 1]);
                if (!iteratorVariable0.Contains(item))
                {
                    iteratorVariable0.Add(item);
                }
            }
            foreach (ushort iteratorVariable1 in iteratorVariable0)
            {
                if (!this.bytePairsFromPreviousPacket.Contains(iteratorVariable1))
                {
                    continue;
                }
                yield return ConvertHelper.ToHashValue(iteratorVariable1, 8);
            }
            this.bytePairsFromPreviousPacket = iteratorVariable0;
        }

        public string AttributeName
        {
            get
            {
                return "BytePairsReocurringIn32FirstBytesMeter";
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

