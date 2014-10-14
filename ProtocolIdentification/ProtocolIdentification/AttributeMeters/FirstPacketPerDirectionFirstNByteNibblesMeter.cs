namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class FirstPacketPerDirectionFirstNByteNibblesMeter : IAttributeMeter
    {
        private int clientToServerOffset = 0;
        private bool clientToServerPacketReceived = false;
        private int nBytesToParsePerPacket = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 0x20);
        private int serverToClientOffset = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);
        private bool serverToClientPacketReceived = false;

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if ((packetDirection != AttributeFingerprintHandler.PacketDirection.ClientToServer) || this.clientToServerPacketReceived)
            {
                if ((packetDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient) && !this.serverToClientPacketReceived)
                {
                    this.serverToClientPacketReceived = true;
                    for (int i = 0; (i < Math.Min(this.nBytesToParsePerPacket, packetLength)) && ((packetStartIndex + i) < frameData.Length); i++)
                    {
                        yield return (((this.serverToClientOffset + (i * 0x10)) + ConvertHelper.ToByteNibble(frameData[packetStartIndex + i])) % AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH);
                    }
                }
                goto Label_01B1;
            }
            this.clientToServerPacketReceived = true;
            int iteratorVariable0 = 0;
        Label_PostSwitchInIterator:;
            if ((iteratorVariable0 < Math.Min(this.nBytesToParsePerPacket, packetLength)) && ((packetStartIndex + iteratorVariable0) < frameData.Length))
            {
                yield return (((this.clientToServerOffset + (iteratorVariable0 * 0x10)) + ConvertHelper.ToByteNibble(frameData[packetStartIndex + iteratorVariable0])) % AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH);
                iteratorVariable0++;
                goto Label_PostSwitchInIterator;
            }
        Label_01B1:;
        }

        public string AttributeName
        {
            get
            {
                return "FirstPacketPerDirectionFirstNByteNibblesMeter";
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

