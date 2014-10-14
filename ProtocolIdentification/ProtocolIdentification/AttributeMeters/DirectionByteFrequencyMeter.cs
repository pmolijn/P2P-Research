namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class DirectionByteFrequencyMeter : IAttributeMeter
    {
        private const int clientToServerStartIndex = 0;
        private readonly int directionLength = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);
        private const int MAX_BYTES_TO_PARSE = 100;
        private readonly int serverToClientStartIndex = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 8)
            {
                for (int i = packetStartIndex; ((i < (packetStartIndex + packetLength)) && (i < frameData.Length)) && (i < (packetStartIndex + 100)); i++)
                {
                    if (packetDirection == AttributeFingerprintHandler.PacketDirection.ClientToServer)
                    {
                        yield return (frameData[i] % this.directionLength);
                    }
                    else if (packetDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient)
                    {
                        yield return ((frameData[i] % this.directionLength) + this.serverToClientStartIndex);
                    }
                }
            }
        }

        public string AttributeName
        {
            get
            {
                return "DirectionByteFrequencyMeter";
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

