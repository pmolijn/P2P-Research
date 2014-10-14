namespace PacketParser.Packets
{
    using PacketParser;
    using PacketParser.Utils;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;

    public class IEC_60870_5_104Packet : AbstractPacket, ISessionPacket
    {
        private const byte APDU_START_MAGIC_VALUE = 0x68;
        private byte apduLength;
        private int asduAddress;
        private byte[] asduData;
        private byte asduInformationObjectCount;
        private byte? asduTypeID;
        private byte causeOfTransmission;
        private bool causeOfTransmissionNegativeConfirm;
        private bool causeOfTransmissionTest;
        private static readonly SystemSettings defaultSystemSettings = new SystemSettings(true, 2, 3);
        private const int maxApduLength = 0xfd;
        private const int minApduLenght = 4;
        private SystemSettings systemSettings;

        internal IEC_60870_5_104Packet(Frame parentFrame, int packetStartIndex, int packetEndIndex) : base(parentFrame, packetStartIndex, packetEndIndex, "IEC 60870-5-104")
        {
            this.systemSettings = defaultSystemSettings;
            if ((packetEndIndex - packetStartIndex) > 4)
            {
                if (parentFrame.Data[packetStartIndex] != 0x68)
                {
                    throw new Exception("APCI must start with 0x68 (104)");
                }
                this.apduLength = parentFrame.Data[packetStartIndex + 1];
                if ((this.apduLength >= 4) && (this.apduLength <= 0xfd))
                {
                    int length = this.apduLength - 4;
                    if (length > 0)
                    {
                        int index = packetStartIndex + 6;
                        this.asduTypeID = new byte?(parentFrame.Data[index]);
                        this.asduInformationObjectCount = (byte) (parentFrame.Data[index + 1] & 0x7f);
                        this.causeOfTransmission = (byte) (parentFrame.Data[index + 2] & 0x3f);
                        this.causeOfTransmissionNegativeConfirm = (parentFrame.Data[index + 2] & 0x40) == 0x40;
                        this.causeOfTransmissionTest = (parentFrame.Data[index + 2] & 0x80) == 0x80;
                        int startIndex = index + 3;
                        if (this.systemSettings.causeOfTransmissionHasOriginatorAddress)
                        {
                            startIndex++;
                        }
                        this.asduAddress = (int) ByteConverter.ToUInt32(parentFrame.Data, startIndex, this.systemSettings.asduAddressLength, true);
                        this.asduData = new byte[length];
                        Array.Copy(parentFrame.Data, index, this.asduData, 0, length);
                    }
                }
            }
        }

        public override IEnumerable<AbstractPacket> GetSubPackets(bool includeSelfReference)
        {
            if (!includeSelfReference)
            {
                yield break;
            }
            yield return this;
        }

        public new static bool TryParse(Frame parentFrame, int packetStartIndex, int packetEndIndex, out AbstractPacket result)
        {
            result = null;
            try
            {
                if (parentFrame.Data[packetStartIndex] != 0x68)
                {
                    return false;
                }
                result = new IEC_60870_5_104Packet(parentFrame, packetStartIndex, packetEndIndex);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal byte ApduLength
        {
            get
            {
                return this.apduLength;
            }
        }

        internal byte[] AsduData
        {
            get
            {
                return this.asduData;
            }
        }

        internal byte AsduInformationObjectCount
        {
            get
            {
                return this.asduInformationObjectCount;
            }
        }

        internal byte? AsduTypeID
        {
            get
            {
                return this.asduTypeID;
            }
        }

        internal CauseOfTransmissionEnum CauseOfTransmission
        {
            get
            {
                return (CauseOfTransmissionEnum) this.causeOfTransmission;
            }
        }

        internal bool CauseOfTransmissionNegativeConfirm
        {
            get
            {
                return this.causeOfTransmissionNegativeConfirm;
            }
        }

        internal bool CauseOfTransmissionTest
        {
            get
            {
                return this.causeOfTransmissionTest;
            }
        }

        public bool PacketHeaderIsComplete
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public int ParsedBytesCount
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        internal SystemSettings Settings
        {
            get
            {
                return this.systemSettings;
            }
        }


        internal enum CauseOfTransmissionEnum : byte
        {
            act = 6,
            actcon = 7,
            actterm = 10,
            back = 2,
            deact = 8,
            deactcon = 9,
            file = 13,
            init = 4,
            inro1 = 0x15,
            inro10 = 30,
            inro11 = 0x1f,
            inro12 = 0x20,
            inro13 = 0x21,
            inro14 = 0x22,
            inro15 = 0x23,
            inro16 = 0x24,
            inro2 = 0x16,
            inro3 = 0x17,
            inro4 = 0x18,
            inro5 = 0x19,
            inro6 = 0x1a,
            inro7 = 0x1b,
            inro8 = 0x1c,
            inro9 = 0x1d,
            inrogen = 20,
            not_used = 0,
            per_cyc = 1,
            req = 5,
            reqco1 = 0x26,
            reqco2 = 0x27,
            reqco3 = 40,
            reqco4 = 0x29,
            reqcogen = 0x25,
            retloc = 12,
            retrem = 11,
            spont = 3,
            unknown_cause_of_transmission = 0x2d,
            unknown_common_address_of_ASDU = 0x2e,
            unknown_information_object_address = 0x2f,
            unknown_type_identification = 0x2c
        }

        internal class CP56Time2a : IEC_60870_5_104Packet.IInformationElement
        {
            private DateTime timestamp;

            public CP56Time2a(byte[] asduBytes, int offset)
            {
                int millisecond = ByteConverter.ToUInt16(asduBytes, offset, true);
                int second = millisecond / 0x3e8;
                millisecond = millisecond % 0x3e8;
                offset += 2;
                int minute = asduBytes[offset] & 0x3f;
                offset++;
                int hour = asduBytes[offset] & 0x1f;
                offset++;
                int day = asduBytes[offset] & 0x1f;
                offset++;
                int month = asduBytes[offset] & 15;
                offset++;
                int year = 0x7d0 + (asduBytes[offset] & 0x7f);
                offset++;
                this.timestamp = new DateTime(year, month, day, hour, minute, second, millisecond);
            }

            public override string ToString()
            {
                return this.timestamp.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss'.'fff");
            }

            public int Length
            {
                get
                {
                    return 7;
                }
            }

            public string ShortName
            {
                get
                {
                    return "CP56Time2a";
                }
            }
        }

        internal class DCO : IEC_60870_5_104Packet.IInformationElement
        {
            private int dcs;
            private IEC_60870_5_104Packet.QOC qoc;

            public DCO(byte[] asduBytes, int offset)
            {
                this.dcs = asduBytes[offset] & 3;
                this.qoc = new IEC_60870_5_104Packet.QOC(asduBytes, offset);
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                if (this.dcs == 1)
                {
                    builder.Append("OFF");
                }
                else if (this.dcs == 2)
                {
                    builder.Append("ON");
                }
                else
                {
                    builder.Append(this.dcs.ToString());
                }
                builder.Append(" (" + this.qoc.ToString() + ")");
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "DCO";
                }
            }
        }

        internal class DIQ : IEC_60870_5_104Packet.IInformationElement
        {
            private DpiState dpi;
            private IEC_60870_5_104Packet.QualityDescriptorNibble qd;

            public DIQ(byte[] asduBytes, int offset)
            {
                byte num = asduBytes[offset];
                this.dpi = ((DpiState) num) & DpiState.INDETERMINATE;
                this.qd = new IEC_60870_5_104Packet.QualityDescriptorNibble(asduBytes, offset);
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append(this.dpi.ToString());
                builder.Append(" (" + this.qd.ToString() + ")");
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "DIQ";
                }
            }

            internal enum DpiState
            {
                INTERMEDIATE,
                OFF,
                ON,
                INDETERMINATE
            }
        }

        private interface IInformationElement
        {
            string ToString();

            int Length { get; }

            string ShortName { get; }
        }

        internal class NVA : IEC_60870_5_104Packet.SVA
        {
            private static NumberFormatInfo nfiSingleton;
            private const double NORMALIZATION_FACTOR = 3.0517578125E-05;

            public NVA(byte[] asduBytes, int offset) : base(asduBytes, offset)
            {
            }

            public override string ToString()
            {
                double num = base.Value * 3.0517578125E-05;
                return num.ToString("P", this.PercentFormat);
            }

            internal NumberFormatInfo PercentFormat
            {
                get
                {
                    if (nfiSingleton == null)
                    {
                        nfiSingleton = new CultureInfo("en-US", false).NumberFormat;
                        nfiSingleton.PercentDecimalDigits = 3;
                    }
                    return nfiSingleton;
                }
            }

            public override string ShortName
            {
                get
                {
                    return "NVA";
                }
            }
        }

        internal class QDS : IEC_60870_5_104Packet.IInformationElement
        {
            private bool ov;
            private IEC_60870_5_104Packet.QualityDescriptorNibble qd;

            public QDS(byte[] asduBytes, int offset)
            {
                this.ov = (asduBytes[offset] & 1) == 1;
                this.qd = new IEC_60870_5_104Packet.QualityDescriptorNibble(asduBytes, offset);
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                if (this.ov)
                {
                    builder.Append("Overflow, ");
                }
                else
                {
                    builder.Append("No Overflow, ");
                }
                builder.Append(this.qd.ToString());
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "QDS";
                }
            }
        }

        internal class QOC : IEC_60870_5_104Packet.IInformationElement
        {
            private int qu;
            private bool select;

            public QOC(byte[] asduBytes, int offset)
            {
                this.qu = (asduBytes[offset] & 0x7c) >> 2;
                this.select = (asduBytes[offset] & 0x80) == 0x80;
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("Qualifier: ");
                builder.Append(this.qu.ToString());
                if (this.qu == 1)
                {
                    builder.Append(" (short pulse)");
                }
                else if (this.qu == 2)
                {
                    builder.Append(" (long pulse)");
                }
                else if (this.qu == 3)
                {
                    builder.Append(" (persistent output)");
                }
                builder.Append(", ");
                if (this.select)
                {
                    builder.Append(" Select");
                }
                else
                {
                    builder.Append(" Execute");
                }
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "QOC";
                }
            }
        }

        internal class QOI : IEC_60870_5_104Packet.IInformationElement
        {
            private byte qoi;

            public QOI(byte[] asduBytes, int offset)
            {
                this.qoi = asduBytes[offset];
            }

            public override string ToString()
            {
                if (this.qoi == 20)
                {
                    return "Station interrogation (global)";
                }
                if ((this.qoi > 20) && (this.qoi < 0x25))
                {
                    int num = this.qoi - 20;
                    return ("Interrogation of group " + num.ToString());
                }
                return ("QOI " + this.qoi.ToString());
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "QOI";
                }
            }
        }

        internal class QOS : IEC_60870_5_104Packet.IInformationElement
        {
            private int ql;
            private bool select;

            public QOS(byte[] asduBytes, int offset)
            {
                this.ql = asduBytes[offset] & 0x7f;
                this.select = (asduBytes[offset] & 0x80) == 0x80;
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                builder.Append("QL: ");
                builder.Append(this.ql.ToString());
                builder.Append(", ");
                if (this.select)
                {
                    builder.Append("Select");
                }
                else
                {
                    builder.Append("Execute");
                }
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "QOS";
                }
            }
        }

        internal class QPM : IEC_60870_5_104Packet.IInformationElement
        {
            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "QPM";
                }
            }
        }

        internal class QualityDescriptorNibble : IEC_60870_5_104Packet.IInformationElement
        {
            private bool bl;
            private bool iv;
            private bool nt;
            private bool sb;

            public QualityDescriptorNibble(byte[] asduBytes, int offset)
            {
                byte num = asduBytes[offset];
                this.bl = (num & 0x10) == 0x10;
                this.sb = (num & 0x20) == 0x20;
                this.nt = (num & 0x40) == 0x40;
                this.iv = (num & 0x80) == 0x80;
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                if (this.bl)
                {
                    builder.Append("Blocked");
                }
                else
                {
                    builder.Append("Not Blocked");
                }
                builder.Append(", ");
                if (this.sb)
                {
                    builder.Append("Substituted");
                }
                else
                {
                    builder.Append("Not Substituted");
                }
                builder.Append(", ");
                if (this.nt)
                {
                    builder.Append("Not Topical");
                }
                else
                {
                    builder.Append("Topical");
                }
                builder.Append(", ");
                if (this.iv)
                {
                    builder.Append("Invalid");
                }
                else
                {
                    builder.Append("Valid");
                }
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return null;
                }
            }
        }

        internal class R32_IEEE_STD_754 : IEC_60870_5_104Packet.IInformationElement
        {
            private float floatValue;

            public R32_IEEE_STD_754(byte[] asduBytes, int offset)
            {
                this.floatValue = BitConverter.ToSingle(asduBytes, offset);
            }

            public override string ToString()
            {
                return this.floatValue.ToString();
            }

            public int Length
            {
                get
                {
                    return 4;
                }
            }

            public string ShortName
            {
                get
                {
                    return "R32-IEEE STD 754";
                }
            }
        }

        internal class SCO : IEC_60870_5_104Packet.IInformationElement
        {
            private IEC_60870_5_104Packet.QOC qoc;
            private bool scs;

            public SCO(byte[] asduBytes, int offset)
            {
                this.scs = (asduBytes[offset] & 1) == 1;
                this.qoc = new IEC_60870_5_104Packet.QOC(asduBytes, offset);
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                if (this.scs)
                {
                    builder.Append("ON ");
                }
                else
                {
                    builder.Append("OFF ");
                }
                builder.Append("(" + this.qoc.ToString() + ")");
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "SCO";
                }
            }
        }

        internal class SIQ : IEC_60870_5_104Packet.IInformationElement
        {
            private IEC_60870_5_104Packet.QualityDescriptorNibble qd;
            private bool spi;

            public SIQ(byte[] asduBytes, int offset)
            {
                byte num = asduBytes[offset];
                this.spi = (num & 1) == 1;
                this.qd = new IEC_60870_5_104Packet.QualityDescriptorNibble(asduBytes, offset);
            }

            public override string ToString()
            {
                StringBuilder builder = new StringBuilder();
                if (this.spi)
                {
                    builder.Append("ON");
                }
                else
                {
                    builder.Append("OFF");
                }
                builder.Append(" (" + this.qd.ToString() + ")");
                return builder.ToString();
            }

            public int Length
            {
                get
                {
                    return 1;
                }
            }

            public string ShortName
            {
                get
                {
                    return "SIQ";
                }
            }
        }

        internal class SVA : IEC_60870_5_104Packet.IInformationElement
        {
            private short value;

            public SVA(byte[] asduBytes, int offset)
            {
                this.value = (short) (asduBytes[offset] + (asduBytes[offset + 1] << 8));
            }

            public override string ToString()
            {
                return this.value.ToString();
            }

            public int Length
            {
                get
                {
                    return 2;
                }
            }

            public virtual string ShortName
            {
                get
                {
                    return "SVA";
                }
            }

            internal short Value
            {
                get
                {
                    return this.value;
                }
            }
        }

        internal class SystemSettings
        {
            internal readonly int asduAddressLength;
            internal readonly bool causeOfTransmissionHasOriginatorAddress;
            internal readonly int ioaLength;

            internal SystemSettings(bool causeOfTransmissionHasOriginatorAddress, int asduAddressLength, int ioaLenght)
            {
                this.causeOfTransmissionHasOriginatorAddress = causeOfTransmissionHasOriginatorAddress;
                this.asduAddressLength = asduAddressLength;
                this.ioaLength = ioaLenght;
            }
        }
    }
}

