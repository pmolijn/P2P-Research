namespace PacketParser.Events
{
    using PacketParser;
    using System;

    public class CredentialEventArgs : EventArgs
    {
        public NetworkCredential Credential;

        public CredentialEventArgs(NetworkCredential credential)
        {
            this.Credential = credential;
        }
    }
}

