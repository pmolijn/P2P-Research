using PacketParser;
using PacketParser.Packets;
using pcapFileIO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;

namespace pcapX
{
    internal class Program
    {
        private const int DEFAULT_FILE_BUFFER_SIZE = 0x2710;
        private const int DEFAULT_PARALLEL_SESSIONS = 0x2710;
        private static readonly DateTime EPOCH = new DateTime(0x7b2, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private static Type ipv4Type = typeof(IPv4Packet);
        private static Type ipv6Type = typeof(IPv6Packet);
        private static PopularityList<string, IFrameWriter> pcapWriters;
        private static Type rawPacketType = typeof(RawPacket);
        private static Type tcpPacket = typeof(TcpPacket);
        private static Type udpPacket = typeof(UdpPacket);

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            foreach (IFrameWriter writer in pcapWriters.GetValueEnumerator())
            {
                try
                {
                    writer.Close();
                }
                catch
                {
                    Console.Error.WriteLine("Error closing file " + writer.Filename);
                }
            }
        }

        private static string GetFilename(string filePathAndName)
        {
            if (filePathAndName.Contains(Path.DirectorySeparatorChar.ToString()) && ((filePathAndName.LastIndexOf(Path.DirectorySeparatorChar) + 1) < filePathAndName.Length))
            {
                return filePathAndName.Substring(filePathAndName.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            }
            return filePathAndName;
        }

        private static IEnumerable<string> GetGroupStrings(Frame frame, SplitMode splitMode, int splitArgument, IDictionary<IPAddress, IPAddress> ipFilter, IDictionary<ushort, ushort> portFilter)
        {
            string iteratorVariable0 = null;
            ushort? iteratorVariable1 = null;
            ushort? iteratorVariable2 = null;
            IPAddress key = null;
            IPAddress destinationIPAddress = null;
            foreach (AbstractPacket packet in frame.PacketList)
            {
                if (packet.GetType() == rawPacketType)
                {
                    break;
                }
                if (packet.GetType() == ipv4Type)
                {
                    key = ((IPv4Packet) packet).SourceIPAddress;
                    destinationIPAddress = ((IPv4Packet) packet).DestinationIPAddress;
                    if (((portFilter != null) && (portFilter.Count != 0)) || (((splitMode != SplitMode.NoSplit) && (splitMode != SplitMode.Host)) && (splitMode != SplitMode.HostPair)))
                    {
                        continue;
                    }
                    break;
                }
                if (packet.GetType() == ipv6Type)
                {
                    key = ((IPv6Packet) packet).SourceIPAddress;
                    destinationIPAddress = ((IPv6Packet) packet).DestinationIPAddress;
                    if (((portFilter != null) && (portFilter.Count != 0)) || (((splitMode != SplitMode.NoSplit) && (splitMode != SplitMode.Host)) && (splitMode != SplitMode.HostPair)))
                    {
                        continue;
                    }
                    break;
                }
                if (packet.GetType() == tcpPacket)
                {
                    iteratorVariable1 = new ushort?(((TcpPacket) packet).SourcePort);
                    iteratorVariable2 = new ushort?(((TcpPacket) packet).DestinationPort);
                    iteratorVariable0 = "TCP";
                    break;
                }
                if (packet.GetType() == udpPacket)
                {
                    iteratorVariable1 = new ushort?(((UdpPacket) packet).SourcePort);
                    iteratorVariable2 = new ushort?(((UdpPacket) packet).DestinationPort);
                    iteratorVariable0 = "UDP";
                    break;
                }
            }
            if (((splitMode == SplitMode.Packets) || (splitMode == SplitMode.Session)) || ((key != null) && (destinationIPAddress != null)))
            {
                if (splitMode == SplitMode.Session)
                {
                    if (iteratorVariable0 == null)
                    {
                        goto Label_074E;
                    }
                    ushort? nullable = iteratorVariable1;
                    int? nullable3 = nullable.HasValue ? new int?(nullable.GetValueOrDefault()) : null;
                    if (!nullable3.HasValue || (key == null))
                    {
                        goto Label_074E;
                    }
                }
                if ((((ipFilter == null) || (ipFilter.Count <= 0)) || (ipFilter.ContainsKey(key) || ipFilter.ContainsKey(destinationIPAddress))) && (((portFilter == null) || (portFilter.Count <= 0)) || ((iteratorVariable1.HasValue && iteratorVariable2.HasValue) && (portFilter.ContainsKey(iteratorVariable1.Value) || portFilter.ContainsKey(iteratorVariable2.Value)))))
                {
                    if (splitMode == SplitMode.Host)
                    {
                        yield return ("Host_" + GetIpString(key));
                        yield return ("Host_" + GetIpString(destinationIPAddress));
                    }
                    else if (splitMode == SplitMode.HostPair)
                    {
                        if (IsLowToHigh(key, 0, destinationIPAddress, 0))
                        {
                            yield return ("HostPair_" + GetIpString(key) + "_" + GetIpString(destinationIPAddress));
                        }
                        else
                        {
                            yield return ("HostPair_" + GetIpString(destinationIPAddress) + "_" + GetIpString(key));
                        }
                    }
                    else if (splitMode == SplitMode.Flow)
                    {
                        yield return (iteratorVariable0 + "_" + GetIpString(key) + "_" + iteratorVariable1.ToString() + "_" + GetIpString(destinationIPAddress) + "_" + iteratorVariable2.ToString());
                    }
                    else if (splitMode == SplitMode.NoSplit)
                    {
                        yield return "NoSplit";
                    }
                    else if (splitMode == SplitMode.Packets)
                    {
                        yield return ("Packets_" + (frame.FrameNumber / splitArgument));
                    }
                    else if (splitMode == SplitMode.Seconds)
                    {
                        long iteratorVariable5 = frame.Timestamp.Subtract(EPOCH).Ticks / 0x989680L;
                        yield return ("Seconds_" + ((iteratorVariable5 / ((long) splitArgument)) * splitArgument));
                    }
                    else if (IsLowToHigh(key, iteratorVariable1.Value, destinationIPAddress, iteratorVariable2.Value))
                    {
                        yield return (iteratorVariable0 + "_" + GetIpString(key) + "_" + iteratorVariable1.ToString() + "_" + GetIpString(destinationIPAddress) + "_" + iteratorVariable2.ToString());
                    }
                    else
                    {
                        yield return (iteratorVariable0 + "_" + GetIpString(destinationIPAddress) + "_" + iteratorVariable2.ToString() + "_" + GetIpString(key) + "_" + iteratorVariable1.ToString());
                    }
                }
            }
        Label_074E:;
        }

        private static string GetIpString(IPAddress ip)
        {
            return ip.ToString().Replace('.', '-').Replace(':', '-');
        }

        private static Type GetPacketBaseType(pcapFrame.DataLinkTypeEnum dataLinkType)
        {
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_ETHERNET)
            {
                return typeof(Ethernet2Packet);
            }
            if (((dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP) || (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP_2)) || (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_RAW_IP_3))
            {
                return typeof(IPv4Packet);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_IEEE_802_11)
            {
                return typeof(IEEE_802_11Packet);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_IEEE_802_11_WLAN_RADIOTAP)
            {
                return typeof(IEEE_802_11RadiotapPacket);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_CHDLC)
            {
                return typeof(CiscoHdlcPacket);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_SLL)
            {
                return typeof(LinuxCookedCapture);
            }
            if (dataLinkType == pcapFrame.DataLinkTypeEnum.WTAP_ENCAP_NULL)
            {
                return typeof(NullLoopbackPacket);
            }
            Console.Error.WriteLine("No packet type found for " + dataLinkType.ToString());
            throw new Exception("No packet type found for " + dataLinkType.ToString());
        }

        private static IEnumerable<FileInfo> GetPcapFilesRecursively(DirectoryInfo di)
        {
            foreach (FileInfo iteratorVariable0 in di.GetFiles("*.*cap"))
            {
                yield return iteratorVariable0;
            }
            foreach (DirectoryInfo iteratorVariable1 in di.GetDirectories())
            {
                foreach (FileInfo iteratorVariable2 in GetPcapFilesRecursively(iteratorVariable1))
                {
                    yield return iteratorVariable2;
                }
            }
        }

        private static bool IsLowToHigh(IPAddress sourceIp, ushort sourcePort, IPAddress destinationIp, ushort destinationPort)
        {
            byte[] addressBytes = sourceIp.GetAddressBytes();
            byte[] buffer2 = destinationIp.GetAddressBytes();
            for (int i = 0; (i < addressBytes.Length) && (i < buffer2.Length); i++)
            {
                if (addressBytes[i] < buffer2[i])
                {
                    return true;
                }
                if (addressBytes[i] > buffer2[i])
                {
                    return false;
                }
            }
            return (sourcePort <= destinationPort);
        }

        private static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                PrintUsage(Console.Error);
            }
            else
            {
                SplitMode session = SplitMode.Session;
                int splitArgument = -1;
                FileType pcap = FileType.pcap;
                string path = null;
                string outputDirectory = null;
                bool deletePreviousOutput = false;
                bool lazyFileCreator = false;
                int parallelSessions = DEFAULT_PARALLEL_SESSIONS;
                int fileBufferSize = DEFAULT_FILE_BUFFER_SIZE;
                Dictionary<IPAddress, IPAddress> ipFilter = new Dictionary<IPAddress, IPAddress>();
                Dictionary<ushort, ushort> portFilter = new Dictionary<ushort, ushort>();
                bool flag3 = false;
                try
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        if (args[i].Equals("-i", StringComparison.InvariantCultureIgnoreCase))
                        {
                            path = args[++i];
                        }
                        else if (args[i].Equals("-o", StringComparison.InvariantCultureIgnoreCase))
                        {
                            outputDirectory = args[++i];
                        }
                        else if (args[i].Equals("-d", StringComparison.InvariantCultureIgnoreCase))
                        {
                            deletePreviousOutput = true;
                        }
                        else if (args[i].Equals("-p", StringComparison.InvariantCultureIgnoreCase))
                        {
                            parallelSessions = int.Parse(args[++i]);
                        }
                        else if (args[i].Equals("-b", StringComparison.InvariantCultureIgnoreCase))
                        {
                            fileBufferSize = int.Parse(args[++i]);
                        }
                        else if (args[i].Equals("-s", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string str3 = args[++i];
                            if (str3.Equals("flow", StringComparison.InvariantCultureIgnoreCase))
                            {
                                session = SplitMode.Flow;
                            }
                            else if (str3.Equals("host", StringComparison.InvariantCultureIgnoreCase))
                            {
                                session = SplitMode.Host;
                            }
                            else if (str3.Equals("hostpair", StringComparison.InvariantCultureIgnoreCase))
                            {
                                session = SplitMode.HostPair;
                            }
                            else if (str3.Equals("session", StringComparison.InvariantCultureIgnoreCase))
                            {
                                session = SplitMode.Session;
                            }
                            else if (str3.Equals("nosplit", StringComparison.InvariantCultureIgnoreCase))
                            {
                                session = SplitMode.NoSplit;
                            }
                            else if (str3.Equals("seconds", StringComparison.InvariantCultureIgnoreCase))
                            {
                                session = SplitMode.Seconds;
                                splitArgument = int.Parse(args[++i]);
                            }
                            else if (str3.Equals("packets", StringComparison.InvariantCultureIgnoreCase))
                            {
                                session = SplitMode.Packets;
                                splitArgument = int.Parse(args[++i]);
                            }
                        }
                        else if (args[i].Equals("-ip", StringComparison.InvariantCultureIgnoreCase))
                        {
                            IPAddress address;
                            string ipString = args[++i];
                            if (IPAddress.TryParse(ipString, out address))
                            {
                                ipFilter.Add(address, address);
                            }
                        }
                        else if (args[i].Equals("-port", StringComparison.InvariantCultureIgnoreCase))
                        {
                            ushort num5;
                            string s = args[++i];
                            if (ushort.TryParse(s, out num5))
                            {
                                portFilter.Add(num5, num5);
                            }
                        }
                        else if (args[i].Equals("-y", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string str6 = args[++i];
                            if (str6.Equals("L7", StringComparison.InvariantCultureIgnoreCase))
                            {
                                pcap = FileType.L7;
                            }
                            else if (str6.Equals("pcap", StringComparison.InvariantCultureIgnoreCase))
                            {
                                pcap = FileType.pcap;
                            }
                        }
                        else if (args[i].Equals("-z", StringComparison.InvariantCultureIgnoreCase))
                        {
                            lazyFileCreator = true;
                        }
                        else if (args[i].Equals("-recursive", StringComparison.InvariantCultureIgnoreCase))
                        {
                            flag3 = true;
                        }
                        else if ((((args.Length == 1) && (args[i] != null)) && (args[i].Contains("cap") || args[i].Contains("dmp"))) && System.IO.File.Exists(args[i]))
                        {
                            path = args[i];
                        }
                        else
                        {
                            Console.Error.WriteLine("Unknown argument : " + args[i]);
                            PrintUsage(Console.Error);
                            return;
                        }
                    }
                }
                catch
                {
                    PrintUsage(Console.Error);
                    return;
                }
                if (!flag3 || Directory.Exists(path))
                {
                    if ((!flag3 && !System.IO.File.Exists(path)) && (path.Trim() != "-"))
                    {
                        Console.Error.WriteLine("Input file does not exist");
                        PrintUsage(Console.Error);
                    }
                    else if (flag3)
                    {
                        DirectoryInfo di = new DirectoryInfo(path);
                        foreach (FileInfo info2 in GetPcapFilesRecursively(di))
                        {
                            Split(info2.FullName, session, splitArgument, lazyFileCreator, outputDirectory, parallelSessions, fileBufferSize, pcap, deletePreviousOutput, ipFilter, portFilter);
                        }
                    }
                    else
                    {
                        Split(path, session, splitArgument, lazyFileCreator, outputDirectory, parallelSessions, fileBufferSize, pcap, deletePreviousOutput, ipFilter, portFilter);
                    }
                }
                else
                {
                    Console.Error.WriteLine("Input directory does not exist");
                    PrintUsage(Console.Error);
                }
            }
        }

        private static void ParsePcapStream(pcapStreamReader observationReader, string outputDirectory, int parallelSessions, int fileBufferSize, SplitMode splitMode, int splitArgument, FileType outputFileType, IDictionary<IPAddress, IPAddress> ipFilter, IDictionary<ushort, ushort> portFilter)
        {
            Type packetBaseType = GetPacketBaseType(observationReader.FileDataLinkType[0]);
            string filename = "pcapX";
            PercentReadDelegate delegate2 = null;
            if (observationReader is pcapFileReader)
            {
                pcapFileReader fileReader = (pcapFileReader) observationReader;
                filename = GetFilename(fileReader.Filename);
                delegate2 = () => fileReader.PercentRead;
            }
            pcapWriters = new PopularityList<string, IFrameWriter>(parallelSessions);
            pcapWriters.PopularityLost += new PopularityList<string, IFrameWriter>.PopularityLostEventHandler(Program.pcapWriters_PopularityLost);
            Console.CancelKeyPress += new ConsoleCancelEventHandler(Program.Console_CancelKeyPress);
            Console.Out.WriteLine("Extracting pcap file into seperate pcap files...");
            int num = 0;
            int num2 = 0;
            foreach (pcapFrame frame in observationReader.PacketEnumerator())
            {
                if ((delegate2 != null) && (delegate2() > num))
                {
                    num = delegate2();
                    try
                    {
                        Console.CursorLeft = 0;
                    }
                    catch (IOException)
                    {
                    }
                    Console.Write(num.ToString() + "%");
                    Console.Out.Flush();
                }
                foreach (string str2 in GetGroupStrings(new Frame(frame.Timestamp, frame.Data, packetBaseType, num2++, false, true), splitMode, splitArgument, ipFilter, portFilter))
                {
                    if (str2 != null)
                    {
                        if (!pcapWriters.ContainsKey(str2))
                        {
                            try
                            {
                                if (outputFileType != FileType.pcap)
                                {
                                    if (outputFileType != FileType.L7)
                                    {
                                        throw new Exception("pcapX cannot handle specified output file type");
                                    }
                                    pcapWriters.Add(str2, new FramePayloadWriter(string.Concat(new object[] { outputDirectory, Path.DirectorySeparatorChar, filename, ".", str2, ".bin" }), FileMode.Append, fileBufferSize, packetBaseType));
                                }
                                else
                                {
                                    pcapWriters.Add(str2, new pcapFileWriter(string.Concat(new object[] { outputDirectory, Path.DirectorySeparatorChar, filename, ".", str2, ".pcap" }), observationReader.FileDataLinkType[0], FileMode.Append, fileBufferSize));
                                }
                            }
                            catch (IOException exception)
                            {
                                int count = pcapWriters.Count;
                                if (count > 100)
                                {
                                    Console.Out.WriteLine("\nError creating new output file!");
                                    Console.Out.WriteLine("\npcapX does currently have " + count + " file handles open in parallel.");
                                    Console.Out.WriteLine("\nTry limiting the number of parallel file handles with the -p switch");
                                    Console.Out.WriteLine("\nFor example: \"pcapX -r dumpfile.pcap -p " + (count - 1) + "\"\n");
                                    Console.Out.Flush();
                                }
                                throw exception;
                            }
                        }
                        pcapWriters[str2].WriteFrame(frame, false);
                    }
                }
            }
            Console.Out.WriteLine("\nPlease wait while closing all file handles...");
            foreach (IFrameWriter writer in pcapWriters.GetValueEnumerator())
            {
                writer.Close();
            }
        }

        private static void pcapWriters_PopularityLost(string key, IFrameWriter writer)
        {
            writer.Close();
        }

        private static void PrintUsage(TextWriter output)
        {
            output.WriteLine("Usage: pcapX [OPTIONS]...");
            output.WriteLine("");
            output.WriteLine("OPTIONS:");
            output.WriteLine("-i <input_file> : Set the pcap file to read from.");
            output.WriteLine("                  Use \"-i -\" to read from stdin");
            output.WriteLine("-o <output_directory> : Manually specify output directory");
            //output.WriteLine("-d : Delete previous output data");
            //output.WriteLine("-p <nr_parallel_sessions> : Set the number of parallel sessions to keep in");
            //output.WriteLine("   memory (default = " + DEFAULT_PARALLEL_SESSIONS + "). More sessions might be needed to split pcap");
            //output.WriteLine("   files from busy links such as an Internet backbone link, this will however");
            //output.WriteLine("   require more memory");
            //output.WriteLine("-b <file_buffer_bytes> : Set the number of bytes to buffer for each");
            //output.WriteLine("   session/output file (default = " + DEFAULT_FILE_BUFFER_SIZE + "). Larger buffers will speed up the");
            //output.WriteLine("   process due to fewer disk write operations, but will occupy more memory.");
            output.WriteLine("-s <GROUP> : Split traffic and group packets to pcap files based on <GROUP>");
            output.WriteLine("   Possible values for <GROUP> are:");
            output.WriteLine("             flow        : Flow, i.e. unidirectional traffic for each 5-tuple,");
            output.WriteLine("                           is grouped together");
            output.WriteLine("             host        : Traffic grouped to one file per host. Most packets");
            output.WriteLine("                           will end up in two files.");
            output.WriteLine("             hostpair    : Traffic grouped based on host-pairs communicating");
            output.WriteLine("             nosplit     : Do not split traffic. Only create ONE output pcap.");
            output.WriteLine("   (default) session     : Packets for each session (bi-directional flow) are");
            output.WriteLine("                           grouped");
            output.WriteLine("             seconds <s> : Split on time, new file after <s> seconds.");
            output.WriteLine("             packets <c> : Split on packet count, new file after <c> packets.");
            output.WriteLine("-ip <IP address to filter on>");
            output.WriteLine("-port <port number to filter on>");
            output.WriteLine("-y <FILETYPE> : Output file type for extracted data. Possible values");
            output.WriteLine("   for <FILETYPE> are:");
            output.WriteLine("             L7   : Only store application layer data");
            output.WriteLine("   (default) pcap : Store complete pcap frames");
            output.WriteLine("-z : Lazy file creation, i.e. only split if needed");
            output.WriteLine("-recursive : Search pcap files in sub-directories recursively");
            output.WriteLine("");
            output.WriteLine("Example 1: pcapX -r dumpfile.pcap");
            output.WriteLine("Example 2: pcapX -r dumpfile.pcap -o session_directory");
            output.WriteLine("Example 3: pcapX -r dumpfile.pcap -s hostpair");
            output.WriteLine("Example 4: pcapX -r dumpfile.pcap -s flow -y L7");
            output.WriteLine("Example 5: pcapX -r dumpfile.pcap -s seconds 3600");
            output.WriteLine("Example 6: pcapX -r dumpfile.pcap -ip 1.2.3.4 -port 80 -port 443 -s nosplit");
            output.WriteLine(@"Example 7: pcapX -r C:\pcaps\ -recursive -s host -port 53 -o DNS_dir");
            //output.WriteLine("Example 8: tcpdump -n -s0 -U -i eth0 -w - | mono pcapX.exe -r -");
        }

        private static void Split(pcapStreamReader observationReader, string pcapFilename, SplitMode splitMode, int splitArgument, string outputDirectory, int parallelSessions, int fileBufferSize, FileType outputFileType, bool deletePreviousOutput, Dictionary<IPAddress, IPAddress> ipFilter, Dictionary<ushort, ushort> portFilter)
        {
            if (outputDirectory == null)
            {
                outputDirectory = GetFilename(pcapFilename);
                if (outputDirectory.Contains("."))
                {
                    outputDirectory = outputDirectory.Substring(0, outputDirectory.LastIndexOf('.'));
                }
            }
            if (deletePreviousOutput && Directory.Exists(outputDirectory))
            {
                Console.Out.WriteLine("Removing previous files in output directory " + outputDirectory.ToString());
                Directory.Delete(outputDirectory, true);
            }
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }
            ParsePcapStream(observationReader, outputDirectory, parallelSessions, fileBufferSize, splitMode, splitArgument, outputFileType, ipFilter, portFilter);
        }

        private static void Split(string pcapFilename, SplitMode splitMode, int splitArgument, bool lazyFileCreator, string outputDirectory, int parallelSessions, int fileBufferSize, FileType outputFileType, bool deletePreviousOutput, Dictionary<IPAddress, IPAddress> ipFilter, Dictionary<ushort, ushort> portFilter)
        {
            if (pcapFilename.Trim() == "-")
            {
                using (Stream stream = Console.OpenStandardInput())
                {
                    using (pcapStreamReader reader = new pcapStreamReader(stream))
                    {
                        Split(reader, "pcapX", splitMode, splitArgument, outputDirectory, parallelSessions, fileBufferSize, outputFileType, deletePreviousOutput, ipFilter, portFilter);
                    }
                    return;
                }
            }
            if (!lazyFileCreator || SplitNeeded(pcapFilename, splitMode, splitArgument))
            {
                using (pcapFileReader reader2 = new pcapFileReader(pcapFilename, 0xfa0, null))
                {
                    Split(reader2, reader2.Filename, splitMode, splitArgument, outputDirectory, parallelSessions, fileBufferSize, outputFileType, deletePreviousOutput, ipFilter, portFilter);
                }
            }
        }

        private static bool SplitNeeded(string pcapFilename, SplitMode splitMode, int splitArgument)
        {
            using (pcapFileReader reader = new pcapFileReader(pcapFilename, 0xfa0, null))
            {
                Type packetBaseType = GetPacketBaseType(reader.FileDataLinkType[0]);
                string str = null;
                int num = 0;
                foreach (pcapFrame frame in reader.PacketEnumerator())
                {
                    foreach (string str2 in GetGroupStrings(new Frame(frame.Timestamp, frame.Data, packetBaseType, num++, false, true), splitMode, splitArgument, null, null))
                    {
                        if (str == null)
                        {
                            str = str2;
                        }
                        else if (str != str2)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }



        private enum FileType
        {
            pcap,
            L7
        }

        private delegate int PercentReadDelegate();

        private enum SplitMode
        {
            Flow,
            Host,
            HostPair,
            Session,
            NoSplit,
            Seconds,
            Packets
        }
    }
}

