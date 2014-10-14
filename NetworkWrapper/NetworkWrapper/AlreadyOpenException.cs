namespace NetworkWrapper
{
    using System;

    public class AlreadyOpenException : Exception
    {
        public override string Message
        {
            get
            {
                return "Device attached to object already open. Close first before reopening";
            }
        }
    }
}

