namespace PacketParser.Fingerprints
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using PacketParser.Packets;

    public interface IOsFingerprinter
    {
        bool TryGetOperatingSystems(out IList<string> osList, IEnumerable<AbstractPacket> packetList);

        string Name { get; }
    }
}

