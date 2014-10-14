namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    public class SyslogPacket : AbstractPacket
    {
        private string syslogMessage;

        internal SyslogPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "Syslog")
        {
            if (packetEndIndex >= packetStartIndex)
            {
                this.syslogMessage = ByteConverter.ReadString(parentFrame.Data, packetStartIndex, (packetEndIndex - packetStartIndex) + 1);
                if (!base.ParentFrame.QuickParse)
                {
                    base.Attributes.Add("Message", this.syslogMessage);
                }
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        internal string SyslogMessage
        {
            get
            {
                return this.syslogMessage;
            }
        }
    }
}

