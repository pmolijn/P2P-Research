namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First4OrderedDirectionFirstNByteNibblesMeter : IAttributeMeter
    {
        private readonly int clientToServerOffset = 0;
        private readonly int nBytesToParsePerPacket;
        private readonly int packetOrderOffsetIncrement;
        private readonly int serverToClientOffset = (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2);

        public First4OrderedDirectionFirstNByteNibblesMeter()
        {
            this.packetOrderOffsetIncrement = this.serverToClientOffset / 4;
            this.nBytesToParsePerPacket = this.packetOrderOffsetIncrement / 0x10;
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetOrderNumberInSession < 4)
            {
                int serverToClientOffset;
                if (packetDirection != AttributeFingerprintHandler.PacketDirection.ClientToServer)
                {
                    if (packetDirection != AttributeFingerprintHandler.PacketDirection.ServerToClient)
                    {
                        goto Label_010B;
                    }
                    serverToClientOffset = this.serverToClientOffset;
                }
                else
                {
                    serverToClientOffset = this.clientToServerOffset;
                }
                for (int i = 0; ((i < packetLength) && (i < this.nBytesToParsePerPacket)) && ((packetStartIndex + i) < frameData.Length); i++)
                {
                    yield return (((serverToClientOffset + (this.packetOrderOffsetIncrement * packetOrderNumberInSession)) + (i * 0x10)) + ConvertHelper.ToByteNibble(frameData[packetStartIndex + i]));
                }
            }
        Label_010B:;
        }

        public string AttributeName
        {
            get
            {
                return "First4OrderedDirectionFirstNByteNibblesMeter";
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

