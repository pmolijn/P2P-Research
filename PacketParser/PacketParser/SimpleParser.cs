namespace PacketParser
{
    using pcapFileIO;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

    [ClassInterface(ClassInterfaceType.AutoDual), ComVisible(true), Guid("4544709D-BB0E-4f24-96F4-7A762996ACFA")]
    public class SimpleParser : ISimpleParser
    {
        public void Parse(string pcapFileName)
        {
            using (pcapFileReader reader = new pcapFileReader(pcapFileName))
            {
                ThreadStart start = new ThreadStart(reader.ThreadStart);
                new Thread(start);
                PacketHandler handler = new PacketHandler(Path.GetFullPath(Process.GetCurrentProcess().MainModule.FileName), Environment.CurrentDirectory);
                handler.StartBackgroundThreads();
                int num = 0;
                foreach (pcapFrame frame in reader.PacketEnumerator())
                {
                    while (((num % 100) == 0) && (handler.FramesInQueue > 0x3e8))
                    {
                        Thread.Sleep(100);
                    }
                    Frame frame2 = handler.GetFrame(frame.Timestamp, frame.Data, frame.DataLinkType);
                    handler.AddFrameToFrameParsingQueue(frame2);
                    num++;
                }
            }
        }
    }
}

