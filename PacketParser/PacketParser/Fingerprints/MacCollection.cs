namespace PacketParser.Fingerprints
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.NetworkInformation;
    using System.Text;

    public class MacCollection
    {
        private SortedDictionary<string, string> macFullDictionary;
        private SortedDictionary<string, string> macPrefixDictionary;
        private static MacCollection singletonInstance = null;
        private static readonly char[] WHITESPACE = new char[] { ' ', '\t' };

        private MacCollection(string macFingerprintFilename, MacFingerprintFileFormat format)
        {
            FileStream stream = new FileStream(macFingerprintFilename, FileMode.Open, FileAccess.Read);
            StreamReader reader = new StreamReader(stream);
            this.macPrefixDictionary = new SortedDictionary<string, string>();
            this.macFullDictionary = new SortedDictionary<string, string>();
            this.macFullDictionary.Add("FF:FF:FF:FF:FF:FF", "Broadcast");
            while (!reader.EndOfStream)
            {
                string str = reader.ReadLine();
                if ((str.Length > 0) && (str[0] != '#'))
                {
                    string key = null;
                    string str3 = null;
                    if ((format == MacFingerprintFileFormat.Ettercap) && (str.Length > 10))
                    {
                        key = str.Substring(0, 8);
                        str3 = str.Substring(10);
                    }
                    else if ((format == MacFingerprintFileFormat.Nmap) && (str.Length > 7))
                    {
                        key = str.Substring(0, 2) + ":" + str.Substring(2, 2) + ":" + str.Substring(4, 2);
                        str3 = str.Substring(7);
                    }
                    else if (((format == MacFingerprintFileFormat.IEEE_OUI) && (str.Length > 15)) && (str.Contains("(hex)") && (str.TrimStart(WHITESPACE)[2] == '-')))
                    {
                        str = str.TrimStart(WHITESPACE);
                        key = str.Substring(0, 8).Replace('-', ':');
                        str3 = str.Substring(str.LastIndexOf('\t') + 1);
                    }
                    if (((key != null) && (str3 != null)) && !this.macPrefixDictionary.ContainsKey(key))
                    {
                        this.macPrefixDictionary.Add(key, str3);
                    }
                }
            }
        }

        public static MacCollection GetMacCollection(string applicationExecutablePath)
        {
            if (singletonInstance == null)
            {
                singletonInstance = new MacCollection(string.Concat(new object[] { Path.GetDirectoryName(applicationExecutablePath), Path.DirectorySeparatorChar, "Fingerprints", Path.DirectorySeparatorChar, "oui.txt" }), MacFingerprintFileFormat.IEEE_OUI);
            }
            return singletonInstance;
        }

        public string GetMacVendor(PhysicalAddress macAddress)
        {
            return this.GetMacVendor(macAddress.GetAddressBytes());
        }

        public string GetMacVendor(string macAddress)
        {
            string key = macAddress.Substring(0, 2) + ":" + macAddress.Substring(3, 2) + ":" + macAddress.Substring(6, 2);
            if (this.macPrefixDictionary.ContainsKey(key))
            {
                return this.macPrefixDictionary[key];
            }
            if (this.macFullDictionary.ContainsKey(macAddress))
            {
                return this.macFullDictionary[macAddress];
            }
            return "Unknown";
        }

        public string GetMacVendor(byte[] macAddress)
        {
            StringBuilder builder = new StringBuilder();
            foreach (byte num in macAddress)
            {
                builder.Append(num.ToString("X2"));
                builder.Append(":");
            }
            if (builder.Length > 0)
            {
                builder.Remove(builder.Length - 1, 1);
            }
            return this.GetMacVendor(builder.ToString());
        }

        public enum MacFingerprintFileFormat
        {
            Ettercap,
            Nmap,
            IEEE_OUI
        }
    }
}

