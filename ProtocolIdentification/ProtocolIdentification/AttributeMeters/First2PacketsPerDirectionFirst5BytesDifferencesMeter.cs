namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class First2PacketsPerDirectionFirst5BytesDifferencesMeter : IAttributeMeter
    {
        private byte[] firstFiveBytesFromClient = new byte[5];
        private byte[] firstFiveBytesFromServer = new byte[5];
        private bool firstPacketFromClientReceived = false;
        private bool firstPacketFromServerReceived = false;
        private bool secondPacketFromClientReceived = false;
        private bool secondPacketFromServerReceived = false;

        private int CalculateDistance(byte b1, byte b2)
        {
            int num;
            if (b1 > b2)
            {
                num = b1 - b2;
            }
            else
            {
                num = b2 - b1;
            }
            return Math.Min(num, (AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2) - 1);
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (packetDirection != AttributeFingerprintHandler.PacketDirection.ClientToServer)
            {
                if (packetDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient)
                {
                    if (!this.firstPacketFromServerReceived)
                    {
                        this.firstPacketFromServerReceived = true;
                        Array.Copy(frameData, packetStartIndex, this.firstFiveBytesFromServer, 0, Math.Min(5, frameData.Length - packetStartIndex));
                    }
                    else if (!this.secondPacketFromServerReceived)
                    {
                        this.secondPacketFromServerReceived = true;
                        for (int i = 0; (i < 5) && (frameData.Length > (packetStartIndex + i)); i++)
                        {
                            yield return ((AttributeFingerprintHandler.Fingerprint.FINGERPRINT_LENGTH / 2) + this.CalculateDistance(this.firstFiveBytesFromServer[i], frameData[packetStartIndex + i]));
                        }
                    }
                }
                yield break;
            }
            if (this.firstPacketFromClientReceived)
            {
                if (!this.secondPacketFromClientReceived)
                {
                    this.secondPacketFromClientReceived = true;
                    for (int j = 0; (j < 5) && (frameData.Length > (packetStartIndex + j)); j++)
                    {
                        yield return this.CalculateDistance(this.firstFiveBytesFromClient[j], frameData[packetStartIndex + j]);
                    }
                }
                yield break;
            }
            this.firstPacketFromClientReceived = true;
            Array.Copy(frameData, packetStartIndex, this.firstFiveBytesFromClient, 0, Math.Min(5, frameData.Length - packetStartIndex));
        }

        public string AttributeName
        {
            get
            {
                return "First2PacketsPerDirectionFirst5BytesDifferencesMeter";
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

