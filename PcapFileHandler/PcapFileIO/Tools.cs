namespace pcapFileIO
{
    using System;
    using System.Globalization;

    public class Tools
    {
        public static string GenerateCaptureFileName(DateTime timestamp)
        {
            return ("OU_" + timestamp.ToString("s", DateTimeFormatInfo.InvariantInfo).Replace(':', '-') + ".pcap");
        }
    }
}

