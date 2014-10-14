namespace NetworkWrapper
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public class WinPCapWrapper : WinPCapNative
    {
        private WinPCapNative.dispatcher_handler callback;
        private int datalink;
        private bool disposed;
        private IntPtr dumper = IntPtr.Zero;
        private StringBuilder errbuf = new StringBuilder(0x100);
        //private string fname = "";
        private Thread ListenThread;
        private string m_attachedDevice;
        private bool m_islistening;
        private bool m_isopen;
        //private int maxb;
        //private int maxp;
        private IntPtr pcap_t = IntPtr.Zero;

        public event WinPCapNative.EndCaptureEventHandler EndCapture;

        public event WinPCapNative.PacketArrivalEventHandler PacketArrival;

        public virtual void Close()
        {
            this.StopDump();
            if (this.IsListening)
            {
                this.StopListen();
            }
            this.m_isopen = false;
            this.m_attachedDevice = null;
            if (this.pcap_t != IntPtr.Zero)
            {
                WinPCapNative.pcap_close(this.pcap_t);
                this.pcap_t = IntPtr.Zero;
            }
        }

        private void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (!disposing && (this.ListenThread == null))
                {
                    if (this.pcap_t != IntPtr.Zero)
                    {
                        WinPCapNative.pcap_close(this.pcap_t);
                        this.pcap_t = IntPtr.Zero;
                    }
                }
                else if (this.ListenThread.IsAlive)
                {
                    this.ListenThread = null;
                }
            }
            this.disposed = true;
        }

        private void DumpPacket(object sender, IntPtr header, IntPtr data)
        {
            if (this.dumper != IntPtr.Zero)
            {
                WinPCapNative.pcap_dump(this.dumper, header, data);
            }
        }

        public static List<Device> FindAllDevs()
        {
            WinPCapNative.pcap_if _if;
            List<Device> list = new List<Device>();
            _if.addresses = IntPtr.Zero;
            _if.description = new StringBuilder().ToString();
            _if.flags = 0;
            _if.name = new StringBuilder().ToString();
            _if.next = IntPtr.Zero;
            IntPtr zero = IntPtr.Zero;
            IntPtr alldevsp = IntPtr.Zero;
            StringBuilder errbuf = new StringBuilder(0x100);
            if (WinPCapNative.pcap_findalldevs(ref zero, errbuf) == -1)
            {
                return null;
            }
            alldevsp = zero;
            while (zero.ToInt32() != 0)
            {
                Device item = new Device();
                list.Add(item);
                _if = (WinPCapNative.pcap_if) Marshal.PtrToStructure(zero, typeof(WinPCapNative.pcap_if));
                item.Name = _if.name;
                item.Description = _if.description;
                if (_if.addresses.ToInt32() != 0)
                {
                    string[] strArray;
                    WinPCapNative.pcap_addr _addr = (WinPCapNative.pcap_addr) Marshal.PtrToStructure(_if.addresses, typeof(WinPCapNative.pcap_addr));
                    if (_addr.addr.ToInt32() != 0)
                    {
                        WinPCapNative.sockaddr sockaddr = (WinPCapNative.sockaddr) Marshal.PtrToStructure(_addr.addr, typeof(WinPCapNative.sockaddr));
                        strArray = new string[] { sockaddr.addr[0].ToString(), ".", sockaddr.addr[1].ToString(), ".", sockaddr.addr[2].ToString(), ".", sockaddr.addr[3].ToString() };
                        item.Address = string.Concat(strArray);
                    }
                    if (_addr.netmask.ToInt32() != 0)
                    {
                        WinPCapNative.sockaddr sockaddr2 = (WinPCapNative.sockaddr) Marshal.PtrToStructure(_addr.netmask, typeof(WinPCapNative.sockaddr));
                        strArray = new string[] { sockaddr2.addr[0].ToString(), ".", sockaddr2.addr[1].ToString(), ".", sockaddr2.addr[2].ToString(), ".", sockaddr2.addr[3].ToString() };
                        item.Netmask = string.Concat(strArray);
                    }
                }
                zero = _if.next;
            }
            WinPCapNative.pcap_freealldevs(alldevsp);
            return list;
        }

        private void GetDatalink()
        {
            this.datalink = WinPCapNative.pcap_datalink(this.pcap_t);
        }

        private void Loop()
        {
            this.callback = new WinPCapNative.dispatcher_handler(this.LoopCallback);
            IntPtr zero = IntPtr.Zero;
            new HandleRef(this.callback, zero);
            WinPCapNative.pcap_loop(this.pcap_t, 0, this.callback, IntPtr.Zero);
        }

        private void LoopCallback(IntPtr param, IntPtr header, IntPtr pkt_data)
        {
            Marshal.PtrToStringAnsi(param);
            WinPCapNative.pcap_pkthdr _pkthdr = (WinPCapNative.pcap_pkthdr) Marshal.PtrToStructure(header, typeof(WinPCapNative.pcap_pkthdr));
            byte[] destination = new byte[_pkthdr.caplen];
            Marshal.Copy(pkt_data, destination, 0, _pkthdr.caplen);
            Marshal.PtrToStringAnsi(pkt_data);
        }

        private void MonitorDump()
        {
            if ((WinPCapNative.pcap_live_dump_ended(this.pcap_t, 1) != 0) && (this.EndCapture != null))
            {
                this.EndCapture(this);
            }
        }

        public virtual bool Open(string source, int snaplen, int flags, int read_timeout)
        {
            if (this.pcap_t != IntPtr.Zero)
            {
                throw new AlreadyOpenException();
            }
            this.pcap_t = WinPCapNative.pcap_open(source, snaplen, flags, read_timeout, IntPtr.Zero, this.errbuf);
            if (this.pcap_t.ToInt32() != 0)
            {
                this.m_isopen = true;
                this.m_attachedDevice = source;
                this.GetDatalink();
                return true;
            }
            this.m_isopen = false;
            this.m_attachedDevice = null;
            return false;
        }

        private bool OpenLive(string source, int snaplen, int promisc, int to_ms)
        {
            this.pcap_t = WinPCapNative.pcap_open_live(source, snaplen, promisc, to_ms, this.errbuf);
            if (this.pcap_t.ToInt32() == 0)
            {
                return false;
            }
            return true;
        }

        public virtual WinPCapNative.PCAP_NEXT_EX_STATE ReadNextInternal(out PcapHeader p, out byte[] packet_data)
        {
            IntPtr ptr;
            return this.ReadNextInternal(out p, out packet_data, out ptr, out ptr);
        }

        private WinPCapNative.PCAP_NEXT_EX_STATE ReadNextInternal(out PcapHeader p, out byte[] packet_data, out IntPtr pkthdr, out IntPtr pktdata)
        {
            pkthdr = IntPtr.Zero;
            pktdata = IntPtr.Zero;
            p = null;
            packet_data = null;
            if (this.pcap_t.ToInt32() == 0)
            {
                this.errbuf = new StringBuilder("No adapter is currently open");
                return WinPCapNative.PCAP_NEXT_EX_STATE.ERROR;
            }
            switch (WinPCapNative.pcap_next_ex(this.pcap_t, ref pkthdr, ref pktdata))
            {
                case 1:
                {
                    WinPCapNative.pcap_pkthdr _pkthdr = (WinPCapNative.pcap_pkthdr) Marshal.PtrToStructure(pkthdr, typeof(WinPCapNative.pcap_pkthdr));
                    p = new PcapHeader();
                    p.CaptureLength = _pkthdr.caplen;
                    p.PacketLength = _pkthdr.len;
                    p.Timeval = _pkthdr.ts;
                    packet_data = new byte[p.PacketLength];
                    Marshal.Copy(pktdata, packet_data, 0, p.PacketLength);
                    return WinPCapNative.PCAP_NEXT_EX_STATE.SUCCESS;
                }
                case 0:
                    return WinPCapNative.PCAP_NEXT_EX_STATE.TIMEOUT;

                case -1:
                    return WinPCapNative.PCAP_NEXT_EX_STATE.ERROR;

                case -2:
                    return WinPCapNative.PCAP_NEXT_EX_STATE.EOF;
            }
            return WinPCapNative.PCAP_NEXT_EX_STATE.UNKNOWN;
        }

        private void ReadNextLoop()
        {
            while (true)
            {
                IntPtr ptr;
                IntPtr ptr2;
                PcapHeader p = null;
                byte[] buffer = null;
                if ((this.ReadNextInternal(out p, out buffer, out ptr, out ptr2) == WinPCapNative.PCAP_NEXT_EX_STATE.SUCCESS) && (this.PacketArrival != null))
                {
                    this.PacketArrival(this, p, buffer);
                }
            }
        }

        public virtual bool SendPacket(byte[] packet_data)
        {
            return (WinPCapNative.pcap_sendpacket(this.pcap_t, packet_data, packet_data.Length) == 0);
        }

        public virtual bool SetKernelBuffer(int bytes)
        {
            return (WinPCapNative.pcap_setbuff(this.pcap_t, bytes) == 0);
        }

        public virtual bool SetMinToCopy(int size)
        {
            return (WinPCapNative.pcap_setmintocopy(this.pcap_t, size) == 0);
        }

        public virtual bool StartDump(string filename)
        {
            if (this.pcap_t == IntPtr.Zero)
            {
                return false;
            }
            try
            {
                this.dumper = WinPCapNative.pcap_dump_open(this.pcap_t, filename);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public virtual void StartListen()
        {
            if (this.ListenThread != null)
            {
                this.ListenThread.Abort();
            }
            this.ListenThread = new Thread(new ThreadStart(this.ReadNextLoop));
            this.ListenThread.Start();
            this.m_islistening = true;
        }

        public virtual void StopDump()
        {
            if (this.dumper != IntPtr.Zero)
            {
                WinPCapNative.pcap_dump_close(this.dumper);
                this.dumper = IntPtr.Zero;
            }
        }

        public virtual void StopListen()
        {
            if ((this.ListenThread != null) && this.ListenThread.IsAlive)
            {
                this.ListenThread.Abort();
            }
            this.ListenThread = null;
            this.m_islistening = false;
        }

        public virtual string AttachedDevice
        {
            get
            {
                return this.m_attachedDevice;
            }
        }

        public int DataLink
        {
            get
            {
                return this.datalink;
            }
        }

        public virtual bool IsListening
        {
            get
            {
                return this.m_islistening;
            }
        }

        public virtual bool IsOpen
        {
            get
            {
                return this.m_isopen;
            }
        }

        public virtual string LastError
        {
            get
            {
                return this.errbuf.ToString();
            }
        }
    }
}

