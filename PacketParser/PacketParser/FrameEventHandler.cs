﻿namespace PacketParser
{
    using PacketParser.Events;
    using System;
    using System.Runtime.CompilerServices;

    public delegate void FrameEventHandler(object sender, FrameEventArgs fe);
}

