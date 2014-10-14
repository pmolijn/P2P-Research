namespace NetworkWrapper
{
    using System;

    public class PcapHeader
    {
        internal WinPCapNative.pcap_pkthdr _Pkhdr;

        public PcapHeader()
        {
            this._Pkhdr = new WinPCapNative.pcap_pkthdr();
            this._Pkhdr = new WinPCapNative.pcap_pkthdr();
        }

        public PcapHeader(WinPCapNative.pcap_pkthdr value)
        {
            this._Pkhdr = new WinPCapNative.pcap_pkthdr();
            this._Pkhdr = value;
        }

        public PcapHeader(WinPCapNative.timeval tv, int plength, int clength)
        {
            this._Pkhdr = new WinPCapNative.pcap_pkthdr();
            this._Pkhdr.caplen = plength;
            this._Pkhdr.len = clength;
            this._Pkhdr.ts = tv;
        }

        public int CaptureLength
        {
            get
            {
                return this._Pkhdr.caplen;
            }
            set
            {
                this._Pkhdr.caplen = value;
            }
        }

        public int MicroSeconds
        {
            get
            {
                return (int) this._Pkhdr.ts.tv_usec;
            }
            set
            {
                this._Pkhdr.ts.tv_usec = (uint) value;
            }
        }

        public int PacketLength
        {
            get
            {
                return this._Pkhdr.len;
            }
            set
            {
                this._Pkhdr.len = value;
            }
        }

        public int Seconds
        {
            get
            {
                return (int) this._Pkhdr.ts.tv_sec;
            }
            set
            {
                this._Pkhdr.ts.tv_sec = (uint) value;
            }
        }

        public virtual DateTime TimeStamp
        {
            get
            {
                DateTime time = new DateTime(1970, 1, 1).AddSeconds((double) this._Pkhdr.ts.tv_sec);
                time.AddMilliseconds((double) this._Pkhdr.ts.tv_usec);
                return time;
            }
        }

        public WinPCapNative.timeval Timeval
        {
            get
            {
                return this._Pkhdr.ts;
            }
            set
            {
                this._Pkhdr.ts = value;
            }
        }
    }
}

