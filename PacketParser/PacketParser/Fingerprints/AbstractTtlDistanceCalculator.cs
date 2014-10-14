namespace PacketParser.Fingerprints
{
    using PacketParser.Packets;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    internal abstract class AbstractTtlDistanceCalculator : ITtlDistanceCalculator
    {
        protected AbstractTtlDistanceCalculator()
        {
        }

        public virtual byte GetOriginalTimeToLive(byte ipTimeToLive)
        {
            if (ipTimeToLive > 0x80)
            {
                return 0xff;
            }
            if (ipTimeToLive > 0x40)
            {
                return 0x80;
            }
            if (ipTimeToLive > 0x20)
            {
                return 0x40;
            }
            return 0x20;
        }

        public virtual byte GetTtlDistance(byte ipTimeToLive)
        {
            return (byte) (this.GetOriginalTimeToLive(ipTimeToLive) - ipTimeToLive);
        }

        public virtual bool TryGetTtlDistance(out byte ttlDistance, IEnumerable<AbstractPacket> packetList)
        {
            foreach (AbstractPacket packet in packetList)
            {
                if (packet.GetType() == typeof(IPv4Packet))
                {
                    ttlDistance = this.GetTtlDistance(((IPv4Packet) packet).TimeToLive);
                    return true;
                }
            }
            ttlDistance = 0;
            return false;
        }
    }
}

