namespace PacketParser.Utils
{
    using System;
    using System.Collections.Generic;
    using System.Net;

    public static class IpAddressUtil
    {
        private static byte[] ipv4ReservedClassA = new byte[] { 
            0, 10, 0x7f, 240, 0xf1, 0xf2, 0xf3, 0xf4, 0xf5, 0xf6, 0xf7, 0xf8, 0xf9, 250, 0xfb, 0xfc, 
            0xfd, 0xfe, 0xff
         };
        private static List<byte> ipv4ReservedClassAList = new List<byte>(ipv4ReservedClassA);

        public static bool IsIanaReserved(IPAddress ipAddress)
        {
            byte[] addressBytes = ipAddress.GetAddressBytes();
            return ((addressBytes.Length == 4) && ipv4ReservedClassAList.Contains(addressBytes[0]));
        }
    }
}

