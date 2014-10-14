using System;

namespace pcapFileIO
{

    internal class pcapParserFactory : IpcapParserFactory
    {
        public IpcapParser CreatePCAPParser(IpcapStreamReader pcapStreamReader)
        {
            return new pcapParser(pcapStreamReader);
        }
    }
}

