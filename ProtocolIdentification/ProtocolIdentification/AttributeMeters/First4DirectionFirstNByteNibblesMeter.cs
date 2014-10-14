namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First4DirectionFirstNByteNibblesMeter : IAttributeMeter
    {
        private readonly int clientToServerOffset = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);
        private readonly int nByteNibbles = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 0x20);
        private readonly int serverToClientOffset = 0;

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 4)
            {
                for (int i = 0; ((i < this.nByteNibbles) && (i < packetLength)) && ((packetStartIndex + i) < frameData.Length); i++)
                {
                    if (packetDirection == AttributeFingerprintHandler.PacketDirection.ClientToServer)
                    {
                        yield return ((this.clientToServerOffset + (i * 0x10)) + ConvertHelper.ToByteNibble(frameData[packetStartIndex + i]));
                    }
                    else if (packetDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient)
                    {
                        yield return ((this.serverToClientOffset + (i * 0x10)) + ConvertHelper.ToByteNibble(frameData[packetStartIndex + i]));
                    }
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "First4DirectionFirstNByteNibblesMeter";
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

