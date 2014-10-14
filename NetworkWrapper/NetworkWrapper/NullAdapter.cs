namespace NetworkWrapper
{
    using System;

    public class NullAdapter : IAdapter
    {
        public override string ToString()
        {
            return "--- Select a network adapter in the list ---";
        }
    }
}

