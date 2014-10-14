namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.Events;
    using PacketParser.Packets;
    using PacketParser.Utils;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;

    internal class IEC_104_PacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public IEC_104_PacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        private int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEC_60870_5_104Packet iec104Packet)
        {
            if ((iec104Packet.AsduData != null) && (iec104Packet.AsduData.Length > 0))
            {
                int startIndex = 3;
                if (iec104Packet.Settings.causeOfTransmissionHasOriginatorAddress)
                {
                    startIndex++;
                }
                startIndex += iec104Packet.Settings.asduAddressLength;
                NameValueCollection parameters = new NameValueCollection();
                string details = string.Concat(new object[] { "IEC 60870-5-104 ASDU Type ID ", iec104Packet.AsduTypeID.ToString(), ", CauseTX ", (byte) iec104Packet.CauseOfTransmission, " (", iec104Packet.CauseOfTransmission.ToString(), ")" });
                if (iec104Packet.CauseOfTransmissionNegativeConfirm)
                {
                    details = details + " NEGATIVE";
                }
                if (iec104Packet.CauseOfTransmissionTest)
                {
                    details = details + " TEST";
                }
                try
                {
                    while ((parameters.Count < iec104Packet.AsduInformationObjectCount) && (startIndex < (iec104Packet.AsduData.Length - iec104Packet.Settings.ioaLength)))
                    {
                        uint num2 = ByteConverter.ToUInt32(iec104Packet.AsduData, startIndex, iec104Packet.Settings.ioaLength, true);
                        startIndex += iec104Packet.Settings.ioaLength;
                        if (iec104Packet.AsduTypeID.Value != 1)
                        {
                            if (iec104Packet.AsduTypeID.Value != 3)
                            {
                                if (iec104Packet.AsduTypeID.Value != 9)
                                {
                                    if (iec104Packet.AsduTypeID.Value != 11)
                                    {
                                        if (iec104Packet.AsduTypeID.Value != 13)
                                        {
                                            if (iec104Packet.AsduTypeID.Value != 30)
                                            {
                                                if (iec104Packet.AsduTypeID.Value != 0x1f)
                                                {
                                                    if (iec104Packet.AsduTypeID.Value != 0x22)
                                                    {
                                                        if (iec104Packet.AsduTypeID.Value != 0x2d)
                                                        {
                                                            if (iec104Packet.AsduTypeID.Value != 0x2e)
                                                            {
                                                                if (iec104Packet.AsduTypeID.Value != 50)
                                                                {
                                                                    if (iec104Packet.AsduTypeID.Value != 0x3a)
                                                                    {
                                                                        if (iec104Packet.AsduTypeID.Value != 0x3b)
                                                                        {
                                                                            if (iec104Packet.AsduTypeID.Value != 0x3d)
                                                                            {
                                                                                if (iec104Packet.AsduTypeID.Value != 0x3f)
                                                                                {
                                                                                    if (iec104Packet.AsduTypeID.Value != 100)
                                                                                    {
                                                                                        int num3 = ((iec104Packet.AsduData.Length - startIndex) + iec104Packet.Settings.ioaLength) / (iec104Packet.AsduInformationObjectCount - parameters.Count);
                                                                                        int nBytesToRead = num3 - iec104Packet.Settings.ioaLength;
                                                                                        if (nBytesToRead <= 0)
                                                                                        {
                                                                                            if (nBytesToRead != 0)
                                                                                            {
                                                                                                throw new Exception();
                                                                                            }
                                                                                            parameters.Add(num2.ToString(), "");
                                                                                        }
                                                                                        else
                                                                                        {
                                                                                            string str2 = ByteConverter.ReadHexString(iec104Packet.AsduData, nBytesToRead, startIndex);
                                                                                            startIndex += nBytesToRead;
                                                                                            parameters.Add(num2.ToString(), str2);
                                                                                        }
                                                                                    }
                                                                                    else
                                                                                    {
                                                                                        IEC_60870_5_104Packet.QOI qoi = new IEC_60870_5_104Packet.QOI(iec104Packet.AsduData, startIndex);
                                                                                        startIndex += qoi.Length;
                                                                                        parameters.Add(num2.ToString(), qoi.ToString());
                                                                                    }
                                                                                }
                                                                                else
                                                                                {
                                                                                    IEC_60870_5_104Packet.R32_IEEE_STD_754 r_ieee_std_3 = new IEC_60870_5_104Packet.R32_IEEE_STD_754(iec104Packet.AsduData, startIndex);
                                                                                    startIndex += r_ieee_std_3.Length;
                                                                                    IEC_60870_5_104Packet.QOS qos3 = new IEC_60870_5_104Packet.QOS(iec104Packet.AsduData, startIndex);
                                                                                    startIndex += qos3.Length;
                                                                                    IEC_60870_5_104Packet.CP56Time2a timea7 = new IEC_60870_5_104Packet.CP56Time2a(iec104Packet.AsduData, startIndex);
                                                                                    startIndex += timea7.Length;
                                                                                    parameters.Add(num2.ToString(), r_ieee_std_3.ToString() + " (" + qos3.ToString() + ") " + timea7.ToString());
                                                                                }
                                                                            }
                                                                            else
                                                                            {
                                                                                IEC_60870_5_104Packet.NVA nva3 = new IEC_60870_5_104Packet.NVA(iec104Packet.AsduData, startIndex);
                                                                                startIndex += nva3.Length;
                                                                                IEC_60870_5_104Packet.QOS qos2 = new IEC_60870_5_104Packet.QOS(iec104Packet.AsduData, startIndex);
                                                                                startIndex += qos2.Length;
                                                                                IEC_60870_5_104Packet.CP56Time2a timea6 = new IEC_60870_5_104Packet.CP56Time2a(iec104Packet.AsduData, startIndex);
                                                                                startIndex += timea6.Length;
                                                                                parameters.Add(num2.ToString(), nva3.ToString() + " (" + qos2.ToString() + ") " + timea6.ToString());
                                                                            }
                                                                        }
                                                                        else
                                                                        {
                                                                            IEC_60870_5_104Packet.DCO dco2 = new IEC_60870_5_104Packet.DCO(iec104Packet.AsduData, startIndex);
                                                                            startIndex += dco2.Length;
                                                                            IEC_60870_5_104Packet.CP56Time2a timea5 = new IEC_60870_5_104Packet.CP56Time2a(iec104Packet.AsduData, startIndex);
                                                                            startIndex += timea5.Length;
                                                                            parameters.Add(num2.ToString(), dco2.ToString() + " " + timea5.ToString());
                                                                        }
                                                                    }
                                                                    else
                                                                    {
                                                                        IEC_60870_5_104Packet.SCO sco2 = new IEC_60870_5_104Packet.SCO(iec104Packet.AsduData, startIndex);
                                                                        startIndex += sco2.Length;
                                                                        IEC_60870_5_104Packet.CP56Time2a timea4 = new IEC_60870_5_104Packet.CP56Time2a(iec104Packet.AsduData, startIndex);
                                                                        startIndex += timea4.Length;
                                                                        parameters.Add(num2.ToString(), sco2.ToString() + " " + timea4.ToString());
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    IEC_60870_5_104Packet.R32_IEEE_STD_754 r_ieee_std_2 = new IEC_60870_5_104Packet.R32_IEEE_STD_754(iec104Packet.AsduData, startIndex);
                                                                    startIndex += r_ieee_std_2.Length;
                                                                    IEC_60870_5_104Packet.QOS qos = new IEC_60870_5_104Packet.QOS(iec104Packet.AsduData, startIndex);
                                                                    startIndex += qos.Length;
                                                                    parameters.Add(num2.ToString(), r_ieee_std_2.ToString() + " (" + qos.ToString() + ")");
                                                                }
                                                            }
                                                            else
                                                            {
                                                                IEC_60870_5_104Packet.DCO dco = new IEC_60870_5_104Packet.DCO(iec104Packet.AsduData, startIndex);
                                                                startIndex += dco.Length;
                                                                parameters.Add(num2.ToString(), dco.ToString());
                                                            }
                                                        }
                                                        else
                                                        {
                                                            IEC_60870_5_104Packet.SCO sco = new IEC_60870_5_104Packet.SCO(iec104Packet.AsduData, startIndex);
                                                            startIndex += sco.Length;
                                                            parameters.Add(num2.ToString(), sco.ToString());
                                                        }
                                                    }
                                                    else
                                                    {
                                                        IEC_60870_5_104Packet.NVA nva2 = new IEC_60870_5_104Packet.NVA(iec104Packet.AsduData, startIndex);
                                                        startIndex += nva2.Length;
                                                        IEC_60870_5_104Packet.QDS qds4 = new IEC_60870_5_104Packet.QDS(iec104Packet.AsduData, startIndex);
                                                        startIndex += qds4.Length;
                                                        IEC_60870_5_104Packet.CP56Time2a timea3 = new IEC_60870_5_104Packet.CP56Time2a(iec104Packet.AsduData, startIndex);
                                                        startIndex += timea3.Length;
                                                        parameters.Add(num2.ToString(), nva2.ToString() + " (" + qds4.ToString() + ") " + timea3.ToString());
                                                    }
                                                }
                                                else
                                                {
                                                    IEC_60870_5_104Packet.DIQ diq2 = new IEC_60870_5_104Packet.DIQ(iec104Packet.AsduData, startIndex);
                                                    startIndex += diq2.Length;
                                                    IEC_60870_5_104Packet.CP56Time2a timea2 = new IEC_60870_5_104Packet.CP56Time2a(iec104Packet.AsduData, startIndex);
                                                    startIndex += timea2.Length;
                                                    parameters.Add(num2.ToString(), diq2.ToString() + " " + timea2.ToString());
                                                }
                                            }
                                            else
                                            {
                                                IEC_60870_5_104Packet.SIQ siq2 = new IEC_60870_5_104Packet.SIQ(iec104Packet.AsduData, startIndex);
                                                startIndex += siq2.Length;
                                                IEC_60870_5_104Packet.CP56Time2a timea = new IEC_60870_5_104Packet.CP56Time2a(iec104Packet.AsduData, startIndex);
                                                startIndex += timea.Length;
                                                parameters.Add(num2.ToString(), siq2.ToString() + " " + timea.ToString());
                                            }
                                        }
                                        else
                                        {
                                            IEC_60870_5_104Packet.R32_IEEE_STD_754 r_ieee_std_ = new IEC_60870_5_104Packet.R32_IEEE_STD_754(iec104Packet.AsduData, startIndex);
                                            startIndex += r_ieee_std_.Length;
                                            IEC_60870_5_104Packet.QDS qds3 = new IEC_60870_5_104Packet.QDS(iec104Packet.AsduData, startIndex);
                                            startIndex += qds3.Length;
                                            parameters.Add(num2.ToString(), r_ieee_std_.ToString() + " (" + qds3.ToString() + ")");
                                        }
                                    }
                                    else
                                    {
                                        IEC_60870_5_104Packet.SVA sva = new IEC_60870_5_104Packet.SVA(iec104Packet.AsduData, startIndex);
                                        startIndex += sva.Length;
                                        IEC_60870_5_104Packet.QDS qds2 = new IEC_60870_5_104Packet.QDS(iec104Packet.AsduData, startIndex);
                                        startIndex += qds2.Length;
                                        parameters.Add(num2.ToString(), sva.ToString() + " (" + qds2.ToString() + ")");
                                    }
                                }
                                else
                                {
                                    IEC_60870_5_104Packet.NVA nva = new IEC_60870_5_104Packet.NVA(iec104Packet.AsduData, startIndex);
                                    startIndex += nva.Length;
                                    IEC_60870_5_104Packet.QDS qds = new IEC_60870_5_104Packet.QDS(iec104Packet.AsduData, startIndex);
                                    startIndex += qds.Length;
                                    parameters.Add(num2.ToString(), nva.ToString() + " (" + qds.ToString() + ")");
                                }
                                continue;
                            }
                            IEC_60870_5_104Packet.DIQ diq = new IEC_60870_5_104Packet.DIQ(iec104Packet.AsduData, startIndex);
                            startIndex += diq.Length;
                            parameters.Add(num2.ToString(), diq.ToString());
                        }
                        else
                        {
                            IEC_60870_5_104Packet.SIQ siq = new IEC_60870_5_104Packet.SIQ(iec104Packet.AsduData, startIndex);
                            startIndex += siq.Length;
                            parameters.Add(num2.ToString(), siq.ToString());
                            continue;
                        }
                    }
                }
                catch (Exception)
                {
                    base.MainPacketHandler.OnAnomalyDetected("Incorrect IEC 60870-5-104 ASDU Information Object in Frame " + iec104Packet.ParentFrame.FrameNumber, iec104Packet.ParentFrame.Timestamp);
                }
                ushort sourcePort = 0;
                ushort destinationPort = 0;
                if (tcpSession.ClientHost.IPAddress.Equals(tcpSession.ServerHost.IPAddress))
                {
                    foreach (AbstractPacket packet in iec104Packet.ParentFrame.PacketList)
                    {
                        if (packet is TcpPacket)
                        {
                            TcpPacket packet2 = packet as TcpPacket;
                            sourcePort = packet2.SourcePort;
                            destinationPort = packet2.DestinationPort;
                        }
                    }
                }
                else if (tcpSession.ClientHost.IPAddress.Equals(sourceHost.IPAddress))
                {
                    sourcePort = tcpSession.ClientTcpPort;
                    destinationPort = tcpSession.ServerTcpPort;
                }
                else
                {
                    sourcePort = tcpSession.ServerTcpPort;
                    destinationPort = tcpSession.ClientTcpPort;
                }
                if (parameters.Count > 0)
                {
                    base.MainPacketHandler.OnParametersDetected(new ParametersEventArgs(iec104Packet.ParentFrame.FrameNumber, sourceHost, destinationHost, sourcePort.ToString(), destinationPort.ToString(), parameters, iec104Packet.ParentFrame.Timestamp, details));
                }
            }
            return Math.Min(iec104Packet.ApduLength + 2, iec104Packet.PacketLength);
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            int num = 0;
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(IEC_60870_5_104Packet))
                {
                    num = this.ExtractData(tcpSession, sourceHost, destinationHost, (IEC_60870_5_104Packet) packet);
                }
            }
            return num;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.IEC_104;
            }
        }
    }
}

