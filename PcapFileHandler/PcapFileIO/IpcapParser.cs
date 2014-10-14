namespace pcapFileIO
{
    using System.Collections.Generic;

    public interface IpcapParser
    {
        pcapFrame ReadPcapPacketBlocking();

        IList<pcapFrame.DataLinkTypeEnum> DataLinkTypes { get; }

        List<KeyValuePair<string, string>> Metadata { get; }
    }
}

