namespace pcapFileIO
{
    public interface IpcapParserFactory
    {
        IpcapParser CreatePCAPParser(IpcapStreamReader pcapStreamReader);
    }
}

