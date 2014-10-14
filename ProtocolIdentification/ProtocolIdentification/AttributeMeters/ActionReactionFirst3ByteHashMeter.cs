namespace ProtocolIdentification.AttributeMeters
{
    using ProtocolIdentification;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Threading;

    internal class ActionReactionFirst3ByteHashMeter : IAttributeMeter
    {
        private byte[] clientToServerFirstByteTriad = new byte[0];
        private const int MAX_STATE_TRANSITIONS_TO_USE = 5;
        private byte[] serverToClientFirstByteTriad = new byte[0];
        private int stateTransitionCount = 0;
        private bool waitingForPacketFromClient = true;
        private bool waitingForPacketFromServer = true;

        private int GetActionReactionPairHash(byte[] action, byte[] reaction)
        {
            byte[] destinationArray = new byte[action.Length + reaction.Length];
            Array.Copy(action, 0, destinationArray, 0, action.Length);
            Array.Copy(reaction, 0, destinationArray, action.Length, reaction.Length);
            return ConvertHelper.ToHashValue(destinationArray, 8);
        }

        private int GetActionReactionPairHash(byte[] action, byte[] reaction, string mutationPassword)
        {
            int hashCode = mutationPassword.GetHashCode();
            for (int i = 0; i < action.Length; i++)
            {
                action[i] = (byte) ((action[i] + hashCode) + i);
            }
            for (int j = 0; j < reaction.Length; j++)
            {
                reaction[j] = (byte) ((reaction[j] - hashCode) - j);
            }
            return this.GetActionReactionPairHash(action, reaction);
        }

        public IEnumerable<int> GetMeasurements(byte[] frameData, int packetStartIndex, int packetLength, DateTime packetTimestamp, AttributeFingerprintHandler.PacketDirection packetDirection, int packetOrderNumberInSession)
        {
            if (this.stateTransitionCount >= 5)
            {
                yield break;
            }
            if ((packetDirection != AttributeFingerprintHandler.PacketDirection.ClientToServer) || !this.waitingForPacketFromClient)
            {
                if ((packetDirection == AttributeFingerprintHandler.PacketDirection.ServerToClient) && this.waitingForPacketFromServer)
                {
                    int iteratorVariable1 = Math.Min(frameData.Length - packetStartIndex, packetLength);
                    this.serverToClientFirstByteTriad = new byte[Math.Min(3, iteratorVariable1)];
                    Array.Copy(frameData, packetStartIndex, this.serverToClientFirstByteTriad, 0, this.serverToClientFirstByteTriad.Length);
                    this.waitingForPacketFromServer = false;
                    this.waitingForPacketFromClient = true;
                    this.stateTransitionCount++;
                    yield return this.GetActionReactionPairHash(this.clientToServerFirstByteTriad, this.serverToClientFirstByteTriad);
                    yield return this.GetActionReactionPairHash(this.clientToServerFirstByteTriad, this.serverToClientFirstByteTriad, "something");
                    yield return this.GetActionReactionPairHash(this.clientToServerFirstByteTriad, this.serverToClientFirstByteTriad, "else");
                    goto Label_PostSwitchInIterator;
                }
                yield break;
            }
            int iteratorVariable0 = Math.Min(frameData.Length - packetStartIndex, packetLength);
            this.clientToServerFirstByteTriad = new byte[Math.Min(3, iteratorVariable0)];
            Array.Copy(frameData, packetStartIndex, this.clientToServerFirstByteTriad, 0, this.clientToServerFirstByteTriad.Length);
            this.waitingForPacketFromClient = false;
            this.waitingForPacketFromServer = true;
            this.stateTransitionCount++;
            yield return this.GetActionReactionPairHash(this.serverToClientFirstByteTriad, this.clientToServerFirstByteTriad);
            yield return this.GetActionReactionPairHash(this.serverToClientFirstByteTriad, this.clientToServerFirstByteTriad, "foo");
            yield return this.GetActionReactionPairHash(this.serverToClientFirstByteTriad, this.clientToServerFirstByteTriad, "bar");
        Label_PostSwitchInIterator:;
        }

        public string AttributeName
        {
            get
            {
                return "ActionReactionFirst3ByteHashMeter";
            }
        }

        public bool IsStateful
        {
            get
            {
                return true;
            }
        }

    }
}

