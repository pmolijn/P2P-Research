namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class NibblePositionPopularityMeter : IAttributeMeter
    {
        private int[,] nibbleCounters;
        private int nibblesToInspect = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 8);
        private const int PACKETS_TO_INSPECT = 8;

        public NibblePositionPopularityMeter()
        {
            this.nibbleCounters = new int[0x10, this.nibblesToInspect];
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 8)
            {
                for (int j = 0; (((packetStartIndex + (j / 2)) < frameData.Length) && ((j / 2) < packetLength)) && (j < this.nibblesToInspect); j++)
                {
                    int index = packetStartIndex + (j / 2);
                    bool mostSignificantNibble = (j % 2) == 0;
                    byte nibble = ConvertHelper.GetNibble(frameData[index], mostSignificantNibble);
                    int iteratorVariable4 = 0;
                    for (int k = 0; k < 0x10; k++)
                    {
                        if ((this.nibbleCounters[k, j] > this.nibbleCounters[nibble, j]) && ((iteratorVariable4 + 1) < 8))
                        {
                            iteratorVariable4++;
                        }
                    }
                    this.nibbleCounters[nibble, j]++;
                    if (packetOrderNumberInSession > 0)
                    {
                        yield return ((j * 8) + iteratorVariable4);
                    }
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "NibblePositionPopularityMeter";
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

