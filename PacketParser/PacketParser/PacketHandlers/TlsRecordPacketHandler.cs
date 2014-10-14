namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.FileTransfer;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography.X509Certificates;

    internal class TlsRecordPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public TlsRecordPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            bool flag = false;
            TcpPacket tcpPacket = null;
            foreach (AbstractPacket packet2 in packetList)
            {
                if (packet2.GetType() == typeof(TcpPacket))
                {
                    tcpPacket = (TcpPacket) packet2;
                }
            }
            int payloadDataLength = 0;
            if (tcpPacket != null)
            {
                foreach (AbstractPacket packet3 in packetList)
                {
                    if (packet3.GetType() == typeof(TlsRecordPacket))
                    {
                        TlsRecordPacket tlsRecordPacket = (TlsRecordPacket) packet3;
                        if (tlsRecordPacket.TlsRecordIsComplete)
                        {
                            this.ExtractTlsRecordData(tlsRecordPacket, tcpPacket, sourceHost, destinationHost, base.MainPacketHandler);
                            flag = true;
                            payloadDataLength += tlsRecordPacket.Length + 5;
                        }
                        else if (tlsRecordPacket.Length > 0xfa0)
                        {
                            flag = true;
                            payloadDataLength = tcpPacket.PayloadDataLength;
                        }
                    }
                }
            }
            if (flag)
            {
                return payloadDataLength;
            }
            return 0;
        }

        private void ExtractTlsRecordData(TlsRecordPacket tlsRecordPacket, TcpPacket tcpPacket, NetworkHost sourceHost, NetworkHost destinationHost, PacketHandler mainPacketHandler)
        {
            foreach (AbstractPacket packet in tlsRecordPacket.GetSubPackets(false))
            {
                if (packet.GetType() == typeof(TlsRecordPacket.HandshakePacket))
                {
                    TlsRecordPacket.HandshakePacket packet2 = (TlsRecordPacket.HandshakePacket) packet;
                    if (packet2.MessageType == TlsRecordPacket.HandshakePacket.MessageTypes.Certificate)
                    {
                        for (int i = 0; i < packet2.CertificateList.Count; i++)
                        {
                            string subject;
                            string str4;
                            byte[] data = packet2.CertificateList[i];
                            X509Certificate certificate = null;
                            try
                            {
                                certificate = new X509Certificate(data);
                                subject = certificate.Subject;
                            }
                            catch
                            {
                                subject = "Unknown_x509_Certificate_Subject";
                                certificate = null;
                            }
                            if (subject.Length > 0x1c)
                            {
                                subject = subject.Substring(0, 0x1c);
                            }
                            if (subject.Contains("="))
                            {
                                subject = subject.Substring(subject.IndexOf('=') + 1);
                            }
                            if (subject.Contains(","))
                            {
                                subject = subject.Substring(0, subject.IndexOf(','));
                            }
                            while (subject.EndsWith(".") || subject.EndsWith(" "))
                            {
                                subject = subject.Substring(0, subject.Length - 1);
                            }
                            string filename = subject + ".cer";
                            string fileLocation = "/";
                            if (certificate != null)
                            {
                                str4 = "TLS Certificate: " + certificate.Subject;
                            }
                            else
                            {
                                str4 = "TLS Certificate: Unknown x509 Certificate";
                            }
                            FileStreamAssembler assembler = new FileStreamAssembler(mainPacketHandler.FileStreamAssemblerList, sourceHost, tcpPacket.SourcePort, destinationHost, tcpPacket.DestinationPort, true, FileStreamTypes.TlsCertificate, filename, fileLocation, data.Length, data.Length, str4, null, tlsRecordPacket.ParentFrame.FrameNumber, tlsRecordPacket.ParentFrame.Timestamp);
                            mainPacketHandler.FileStreamAssemblerList.Add(assembler);
                            if (assembler.TryActivate())
                            {
                                assembler.AddData(data, tcpPacket.SequenceNumber);
                            }
                        }
                    }
                }
            }
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.Ssl;
            }
        }
    }
}

