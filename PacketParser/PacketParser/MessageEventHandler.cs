namespace PacketParser
{
    using PacketParser.Events;
    using System;
    using System.Runtime.CompilerServices;

    public delegate void MessageEventHandler(object sender, MessageEventArgs me);
}

