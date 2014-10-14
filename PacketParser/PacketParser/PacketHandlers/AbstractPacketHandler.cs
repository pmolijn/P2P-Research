namespace PacketParser.PacketHandlers
{
    using PacketParser;
    using System;

    internal abstract class AbstractPacketHandler
    {
        private PacketHandler mainPacketHandler;

        internal AbstractPacketHandler(PacketHandler mainPacketHandler)
        {
            this.mainPacketHandler = mainPacketHandler;
        }

        internal PacketHandler MainPacketHandler
        {
            get
            {
                return this.mainPacketHandler;
            }
        }
    }
}

