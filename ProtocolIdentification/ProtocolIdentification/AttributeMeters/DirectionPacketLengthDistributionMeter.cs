namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class DirectionPacketLengthDistributionMeter : IAttributeMeter
    {
        private readonly int clientToServerStartIndex = 0;
        private readonly int expectedMaxPacketLength = 0x5dc;
        private readonly double exponent;
        private readonly int serverToClientStartIndex = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);

        public DirectionPacketLengthDistributionMeter()
        {
            this.exponent = Math.Log((double) this.serverToClientStartIndex) / Math.Log((double) this.expectedMaxPacketLength);
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetDirection != AttributeFingerprintHandler.PacketDirection.ClientToServer)
            {
                if (packetDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient)
                {
                    yield return (this.serverToClientStartIndex + this.GetPacketBinNumber(packetLength));
                    goto Label_PostSwitchInIterator;
                }
                yield break;
            }
            yield return (this.clientToServerStartIndex + this.GetPacketBinNumber(packetLength));
        Label_PostSwitchInIterator:;
        }

        private int GetPacketBinNumber(int packetLength)
        {
            return Math.Min(this.serverToClientStartIndex - 1, (int) Math.Pow((double) packetLength, this.exponent));
        }

        public string AttributeName
        {
            get
            {
                return "DirectionPacketLengthDistributionMeter";
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

