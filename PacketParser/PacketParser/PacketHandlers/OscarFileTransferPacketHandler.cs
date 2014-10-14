namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.FileTransfer;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;

    internal class OscarFileTransferPacketHandler : AbstractPacketHandler, ITcpSessionPacketHandler
    {
        public OscarFileTransferPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
        }

        public int ExtractData(NetworkTcpSession tcpSession, NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            OscarFileTransferPacket packet = null;
            TcpPacket packet2 = null;
            int parsedBytesCount = 0;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(OscarFileTransferPacket))
                {
                    packet = (OscarFileTransferPacket) packet3;
                }
                else if (packet3.GetType() == typeof(TcpPacket))
                {
                    packet2 = (TcpPacket) packet3;
                }
            }
            if ((packet != null) && (packet2 != null))
            {
                parsedBytesCount = packet.ParsedBytesCount;
                if (packet.Type == OscarFileTransferPacket.CommandType.SendRequest)
                {
                    if (base.MainPacketHandler.FileStreamAssemblerList.ContainsAssembler(sourceHost, packet2.SourcePort, destinationHost, packet2.DestinationPort, true))
                    {
                        FileStreamAssembler assembler = base.MainPacketHandler.FileStreamAssemblerList.GetAssembler(sourceHost, packet2.SourcePort, destinationHost, packet2.DestinationPort, true);
                        base.MainPacketHandler.FileStreamAssemblerList.Remove(assembler, true);
                    }
                    FileStreamAssembler assembler2 = new FileStreamAssembler(base.MainPacketHandler.FileStreamAssemblerList, sourceHost, packet2.SourcePort, destinationHost, packet2.DestinationPort, true, FileStreamTypes.OscarFileTransfer, packet.FileName, "", (int) packet.TotalFileSize, (int) packet.TotalFileSize, packet.FileName, "", packet.ParentFrame.FrameNumber, packet.ParentFrame.Timestamp);
                    base.MainPacketHandler.FileStreamAssemblerList.Add(assembler2);
                    return parsedBytesCount;
                }
                if (packet.Type == OscarFileTransferPacket.CommandType.ReceiveAccept)
                {
                    if (base.MainPacketHandler.FileStreamAssemblerList.ContainsAssembler(destinationHost, packet2.DestinationPort, sourceHost, packet2.SourcePort, true))
                    {
                        FileStreamAssembler assembler3 = base.MainPacketHandler.FileStreamAssemblerList.GetAssembler(destinationHost, packet2.DestinationPort, sourceHost, packet2.SourcePort, true);
                        if (assembler3 != null)
                        {
                            assembler3.TryActivate();
                        }
                    }
                    return parsedBytesCount;
                }
                if ((packet.Type == OscarFileTransferPacket.CommandType.TransferComplete) && base.MainPacketHandler.FileStreamAssemblerList.ContainsAssembler(destinationHost, packet2.DestinationPort, sourceHost, packet2.SourcePort, true))
                {
                    FileStreamAssembler assembler4 = base.MainPacketHandler.FileStreamAssemblerList.GetAssembler(destinationHost, packet2.DestinationPort, sourceHost, packet2.SourcePort, true);
                    base.MainPacketHandler.FileStreamAssemblerList.Remove(assembler4, true);
                }
            }
            return parsedBytesCount;
        }

        public void Reset()
        {
        }

        public ApplicationLayerProtocol HandledProtocol
        {
            get
            {
                return ApplicationLayerProtocol.OscarFileTransfer;
            }
        }
    }
}

