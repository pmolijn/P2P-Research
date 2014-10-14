namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using PacketParser.FileTransfer;
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    internal class TftpPacketHandler : AbstractPacketHandler, IPacketHandler
    {
        private PopularityList<string, ushort> tftpSessionBlksizeList;

        public TftpPacketHandler(PacketHandler mainPacketHandler) : base(mainPacketHandler)
        {
            this.tftpSessionBlksizeList = new PopularityList<string, ushort>(100);
        }

        public void ExtractData(ref NetworkHost sourceHost, NetworkHost destinationHost, IEnumerable<AbstractPacket> packetList)
        {
            UdpPacket packet = null;
            TftpPacket tftpPacket = null;
            FileStreamAssembler assembler;
            foreach (AbstractPacket packet3 in packetList)
            {
                if (packet3.GetType() == typeof(UdpPacket))
                {
                    packet = (UdpPacket) packet3;
                }
                else if (packet3.GetType() == typeof(TftpPacket))
                {
                    tftpPacket = (TftpPacket) packet3;
                }
            }
            if ((packet != null) && (this.TryGetFileStreamAssembler(out assembler, base.MainPacketHandler.FileStreamAssemblerList, sourceHost, packet.SourcePort, destinationHost, packet.DestinationPort) || ((tftpPacket != null) && this.TryCreateNewAssembler(out assembler, base.MainPacketHandler.FileStreamAssemblerList, tftpPacket, sourceHost, packet.SourcePort, destinationHost))))
            {
                string key = this.GetTftpSessionId(sourceHost, packet.SourcePort, destinationHost, packet.DestinationPort);
                ushort blksize = 0x200;
                if (this.tftpSessionBlksizeList.ContainsKey(key))
                {
                    blksize = this.tftpSessionBlksizeList[key];
                }
                if ((tftpPacket == null) || (tftpPacket.Blksize != blksize))
                {
                    try
                    {
                        tftpPacket = new TftpPacket(packet.ParentFrame, packet.PacketStartIndex + 8, packet.PacketEndIndex, blksize);
                        if (tftpPacket.Blksize != blksize)
                        {
                            if (this.tftpSessionBlksizeList.ContainsKey(key))
                            {
                                this.tftpSessionBlksizeList[key] = tftpPacket.Blksize;
                            }
                            else
                            {
                                this.tftpSessionBlksizeList.Add(key, tftpPacket.Blksize);
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        if (assembler != null)
                        {
                            base.MainPacketHandler.OnAnomalyDetected("Error parsing TFTP packet: " + exception.Message, packet.ParentFrame.Timestamp);
                        }
                    }
                }
                if (tftpPacket != null)
                {
                    this.ExtractFileData(assembler, base.MainPacketHandler.FileStreamAssemblerList, sourceHost, packet.SourcePort, destinationHost, packet.DestinationPort, tftpPacket);
                }
            }
        }

        private void ExtractFileData(FileStreamAssembler assembler, FileStreamAssemblerList fileStreamAssemblerList, NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort, TftpPacket tftpPacket)
        {
            if (tftpPacket.OpCode == TftpPacket.OpCodes.Data)
            {
                if (!assembler.IsActive)
                {
                    if ((assembler.SourcePort != sourcePort) || (assembler.DestinationPort != destinationPort))
                    {
                        fileStreamAssemblerList.Remove(assembler, true);
                        assembler = new FileStreamAssembler(fileStreamAssemblerList, sourceHost, sourcePort, destinationHost, destinationPort, false, FileStreamTypes.TFTP, assembler.Filename, assembler.FileLocation, assembler.Details, tftpPacket.ParentFrame.FrameNumber, tftpPacket.ParentFrame.Timestamp);
                        fileStreamAssemblerList.Add(assembler);
                    }
                    assembler.TryActivate();
                }
                if (((assembler.SourceHost == sourceHost) && (assembler.SourcePort == sourcePort)) && ((assembler.DestinationHost == destinationHost) && (assembler.DestinationPort == destinationPort)))
                {
                    assembler.AddData(tftpPacket.DataBlock, tftpPacket.DataBlockNumber);
                    if (tftpPacket.DataBlockIsLast)
                    {
                        assembler.FinishAssembling();
                    }
                }
            }
        }

        private string GetTftpSessionId(NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort)
        {
            string str = sourceHost.IPAddress.ToString() + "\t" + sourcePort.ToString();
            string strB = destinationHost.IPAddress.ToString() + "\t" + destinationPort.ToString();
            if (str.CompareTo(strB) > 0)
            {
                return (str + "\t" + strB);
            }
            return (strB + "\t" + str);
        }

        public void Reset()
        {
            this.tftpSessionBlksizeList.Clear();
        }

        private bool TryCreateNewAssembler(out FileStreamAssembler assembler, FileStreamAssemblerList fileStreamAssemblerList, TftpPacket tftpPacket, NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost)
        {
            assembler = null;
            if (tftpPacket.OpCode == TftpPacket.OpCodes.ReadRequest)
            {
                try
                {
                    assembler = new FileStreamAssembler(fileStreamAssemblerList, destinationHost, 0x45, sourceHost, sourcePort, false, FileStreamTypes.TFTP, tftpPacket.Filename, "", tftpPacket.OpCode.ToString() + " " + tftpPacket.Mode.ToString() + " " + tftpPacket.Filename, tftpPacket.ParentFrame.FrameNumber, tftpPacket.ParentFrame.Timestamp);
                    fileStreamAssemblerList.Add(assembler);
                }
                catch (Exception)
                {
                    if (assembler != null)
                    {
                        assembler.Clear();
                        assembler = null;
                    }
                    return false;
                }
                return true;
            }
            if (tftpPacket.OpCode == TftpPacket.OpCodes.WriteRequest)
            {
                try
                {
                    assembler = new FileStreamAssembler(fileStreamAssemblerList, sourceHost, sourcePort, destinationHost, 0x45, false, FileStreamTypes.TFTP, tftpPacket.Filename, "", tftpPacket.OpCode.ToString() + " " + tftpPacket.Mode.ToString() + " " + tftpPacket.Filename, tftpPacket.ParentFrame.FrameNumber, tftpPacket.ParentFrame.Timestamp);
                    fileStreamAssemblerList.Add(assembler);
                }
                catch (Exception)
                {
                    if (assembler != null)
                    {
                        assembler.Clear();
                        assembler = null;
                    }
                    return false;
                }
                return true;
            }
            assembler = null;
            return false;
        }

        private bool TryGetFileStreamAssembler(out FileStreamAssembler assembler, FileStreamAssemblerList fileStreamAssemblerList, NetworkHost sourceHost, ushort sourcePort, NetworkHost destinationHost, ushort destinationPort)
        {
            if (fileStreamAssemblerList.ContainsAssembler(sourceHost, sourcePort, destinationHost, destinationPort, false))
            {
                assembler = fileStreamAssemblerList.GetAssembler(sourceHost, sourcePort, destinationHost, destinationPort, false);
                return true;
            }
            if (fileStreamAssemblerList.ContainsAssembler(sourceHost, 0x45, destinationHost, destinationPort, false))
            {
                assembler = fileStreamAssemblerList.GetAssembler(sourceHost, 0x45, destinationHost, destinationPort, false);
                return true;
            }
            if (fileStreamAssemblerList.ContainsAssembler(sourceHost, sourcePort, destinationHost, 0x45, false))
            {
                assembler = fileStreamAssemblerList.GetAssembler(sourceHost, sourcePort, destinationHost, 0x45, false);
                return true;
            }
            assembler = null;
            return false;
        }
    }
}

