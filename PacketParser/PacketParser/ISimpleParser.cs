namespace PacketParser
{
    using System;
    using System.Runtime.InteropServices;

    [Guid("BBB92AA6-718C-4123-8187-F407D18600C0")]
    public interface ISimpleParser
    {
        [DispId(1)]
        void Parse(string pcapFileName);
    }
}

