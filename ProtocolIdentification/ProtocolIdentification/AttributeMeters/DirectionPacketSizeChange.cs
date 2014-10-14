namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class DirectionPacketSizeChange : IAttributeMeter
    {
        private int previousClientToServerPacketSize;
        private int previousServerToClientPacketSize;

        IEnumerable<int> IAttributeMeter.GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            int iteratorVariable0 = 0;
            int iteratorVariable1 = 0;
            if (packetDirection != AttributeFingerprintHandler.PacketDirection.ClientToServer)
            {
                if (packetDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient)
                {
                    iteratorVariable0 = packetLength - this.previousServerToClientPacketSize;
                    this.previousServerToClientPacketSize = packetLength;
                    iteratorVariable1 = AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2;
                }
            }
            else
            {
                iteratorVariable0 = packetLength - this.previousClientToServerPacketSize;
                this.previousClientToServerPacketSize = packetLength;
            }
            if (iteratorVariable0 < 0)
            {
                iteratorVariable0 = -iteratorVariable0;
            }
            iteratorVariable0 = iteratorVariable0 % (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);
            yield return (iteratorVariable0 + iteratorVariable1);
        }

        public bool IsStateful
        {
            get
            {
                return true;
            }
        }

        string IAttributeMeter.AttributeName
        {
            get
            {
                return "DirectionPacketSizeChange";
            }
        }

    }
}

