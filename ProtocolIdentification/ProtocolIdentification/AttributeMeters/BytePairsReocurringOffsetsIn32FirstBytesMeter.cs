namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class BytePairsReocurringOffsetsIn32FirstBytesMeter : IAttributeMeter
    {
        private List<ushort> bytePairsFromPreviousPacket = new List<ushort>();
        private int directionPairLength = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 9);
        private AttributeFingerprintHandler.PacketDirection previousPacketDirection = AttributeFingerprintHandler.PacketDirection.Unknown;

        private ushort ConvertToUshort(byte b1, byte b2)
        {
            ushort num = b1;
            num = (ushort) (num << 8);
            return (ushort) (num + b2);
        }

        private int GetDirectionPairOffset(AttributeFingerprintHandler.PacketDirection previousDirection, AttributeFingerprintHandler.PacketDirection currentDirection)
        {
            int num = 0;
            if (this.previousPacketDirection == AttributeFingerprintHandler.PacketDirection.ClientToServer)
            {
                num += 3 * this.directionPairLength;
            }
            else if (this.previousPacketDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient)
            {
                num += 6 * this.directionPairLength;
            }
            if (currentDirection == AttributeFingerprintHandler.PacketDirection.ClientToServer)
            {
                return (num + this.directionPairLength);
            }
            if (currentDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient)
            {
                num += 2 * this.directionPairLength;
            }
            return num;
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
                if (this.bytePairsFromPreviousPacket.Contains(item))
                {
                    yield return (this.GetDirectionPairOffset(this.previousPacketDirection, packetDirection) + Math.Min(i, this.directionPairLength - 1));
                }
            }
            this.bytePairsFromPreviousPacket = iteratorVariable0;
            this.previousPacketDirection = packetDirection;
        }

        public string AttributeName
        {
            get
            {
                return "BytePairsReocurringOffsetsIn32FirstBytesMeter";
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

