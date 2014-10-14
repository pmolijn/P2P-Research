namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First2PacketsFirst8ByteHashDirectionCountsMeter : IAttributeMeter
    {
        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 2)
            {
                int iteratorVariable0;
                if (packetDirection != AttributeFingerprintHandler.PacketDirection.ClientToServer)
                {
                    if (packetDirection != AttributeFingerprintHandler.PacketDirection.ServerToClient)
                    {
                        goto Label_011C;
                    }
                    iteratorVariable0 = 0;
                }
                else
                {
                    iteratorVariable0 = 8;
                }
                int iteratorVariable1 = 0x10;
                byte[] iteratorVariable2 = new byte[iteratorVariable1];
                for (int j = 0; ((j < 8) && ((j + packetStartIndex) < frameData.Length)) && (j < packetLength); j++)
                {
                    int index = ConvertHelper.ToHashValue(frameData[packetStartIndex + j], 4);
                    iteratorVariable2[index] = (byte) (iteratorVariable2[index] + 1);
                }
                for (int i = 0; i < iteratorVariable2.Length; i++)
                {
                    yield return (((i << 4) ^ iteratorVariable0) ^ iteratorVariable2[i]);
                }
            }
        Label_011C:;
        }

        public string AttributeName
        {
            get
            {
                return "First2PacketsFirst8ByteHashDirectionCountsMeter";
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

