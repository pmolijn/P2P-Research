namespace PacketParser.Packets
{
    using PacketParser;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Threading;

    internal class SslPacket : AbstractPacket
    {
        private SslPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Secure Socket Layer")
        {
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (includeSelfReference)
            {
                yield return this;
            }
            int iteratorVariable0 = 0;
            while ((this.PacketStartIndex + iteratorVariable0) < this.PacketEndIndex)
            {
                AbstractPacket iteratorVariable1;
                try
                {
                    iteratorVariable1 = new TlsRecordPacket(this.ParentFrame, this.PacketStartIndex + iteratorVariable0, this.PacketEndIndex);
                }
                catch
                {
                    iteratorVariable1 = new RawPacket(this.ParentFrame, this.PacketStartIndex + iteratorVariable0, this.PacketEndIndex);
                }
                iteratorVariable0 += iteratorVariable1.PacketByteCount;
                yield return iteratorVariable1;
                foreach (AbstractPacket iteratorVariable2 in iteratorVariable1.GetSubPackets(false))
                {
                    yield return iteratorVariable2;
                }
            }
        }

        public static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            bool flag = TlsRecordPacket.TryParse(parentFrame, packetStartIndex, packetEndIndex, out result);
            if (flag)
            {
                try
                {
                    result = new SslPacket(parentFrame, packetStartIndex, packetEndIndex);
                }
                catch
                {
                    result = null;
                }
            }
            return (flag && (result != null));
        }

    }
}

