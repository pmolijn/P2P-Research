namespace PacketParser.Packets
{
    using PacketParser;
    using System;
    using System.Text;

    internal abstract class NetBiosPacket : AbstractPacket
    {
        internal NetBiosPacket(Frame parentFrame, int packetStartIndex, int packetEndIndex, string packetTypeDescription) : base(parentFrame, packetStartIndex, packetEndIndex, packetTypeDescription)
        {
        }

        internal static string DecodeNetBiosName(Frame parentFrame, ref int frameIndex)
        {
            int num = frameIndex;
            StringBuilder builder = new StringBuilder("");
            byte num2 = parentFrame.Data[frameIndex];
            if (!parentFrame.QuickParse && (num2 > 0x3f))
            {
                parentFrame.Errors.Add(new Frame.Error(parentFrame, frameIndex, frameIndex, "NetBios Name label is larger than 63 : " + num2));
            }
            frameIndex++;
            for (byte i = 0; i < num2; i = (byte) (i + 2))
            {
                byte num4 = parentFrame.Data[frameIndex];
                byte num5 = parentFrame.Data[frameIndex + 1];
                char ch = (char) (((num4 - 0x41) << 4) + (num5 - 0x41));
                if ((i == (num2 - 2)) && (frameIndex == ((num + 1) + 30)))
                {
                    if ((((byte) ch) != 0) && (((byte) ch) != 0x20))
                    {
                        builder.Append("<" + ((byte) ch).ToString("X2") + ">");
                    }
                }
                else if ((ch != ' ') && (ch != '\0'))
                {
                    builder.Append(ch);
                }
                frameIndex += 2;
            }
            while (((parentFrame.Data[frameIndex] != 0) && (frameIndex < (num + 0xff))) && (frameIndex < parentFrame.Data.Length))
            {
                builder.Append(".");
                num2 = parentFrame.Data[frameIndex];
                if (!parentFrame.QuickParse && (num2 > 0x3f))
                {
                    parentFrame.Errors.Add(new Frame.Error(parentFrame, frameIndex, frameIndex, "NetBios Name label is larger than 63 : " + num2));
                }
                frameIndex++;
                for (byte j = 0; j < num2; j = (byte) (j + 1))
                {
                    builder.Append((char) parentFrame.Data[frameIndex]);
                    frameIndex++;
                }
            }
            frameIndex++;
            return builder.ToString();
        }
    }
}

