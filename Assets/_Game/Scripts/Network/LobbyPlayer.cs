using System;
using NuclearProtection.Core;
using Unity.Collections;
using Unity.Netcode;

namespace NuclearProtection.Network
{
    /// <summary>Un joueur du lobby, synchronisé à tous les clients.</summary>
    public struct LobbyPlayer : INetworkSerializable, IEquatable<LobbyPlayer>
    {
        public ulong ClientId;
        public FixedString32Bytes Name;
        public TeamId Team;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref Name);
            serializer.SerializeValue(ref Team);
        }

        public bool Equals(LobbyPlayer other) =>
            ClientId == other.ClientId && Name.Equals(other.Name) && Team == other.Team;
    }
}
