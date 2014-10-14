namespace NetworkWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class WinPCapAdapter : IAdapter
    {
        private string description;
        private string ipAddress;
        private string netmask;
        private string npfName;

        internal WinPCapAdapter(Device device)
        {
            this.ipAddress = device.Address;
            this.description = device.Description;
            this.npfName = device.Name;
            this.netmask = device.Netmask;
        }

        public static List<IAdapter> GetAdapters()
        {
            List<IAdapter> list = new List<IAdapter>();
            foreach (Device device in WinPCapWrapper.FindAllDevs())
            {
                list.Add(new WinPCapAdapter(device));
            }
            return list;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder("WinPcap: " + this.description);
            if ((this.ipAddress != null) && (this.ipAddress.Length > 6))
            {
                builder.Append(" (" + this.ipAddress + ")");
            }
            if (this.npfName.Contains("{"))
            {
                builder.Append(" " + this.npfName.Substring(this.npfName.IndexOf('{')));
            }
            else
            {
                builder.Append(" " + this.npfName);
            }
            return builder.ToString();
        }

        internal string NPFName
        {
            get
            {
                return this.npfName;
            }
        }
    }
}

