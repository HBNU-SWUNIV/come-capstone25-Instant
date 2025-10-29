using System;
using Players.Common;
using Scriptable;
using Unity.Collections;
using Unity.Netcode;

namespace Players.Structs
{
    public struct PlayerData : INetworkSerializable, IEquatable<PlayerData>
    {
        public ulong clientId;
        public FixedString32Bytes name;
        public AnimalType type;
        public Role role;

        public PlayerData(ulong id, FixedString32Bytes name, AnimalType type, Role role)
        {
            clientId = id;
            this.name = name;
            this.type = type;
            this.role = role;
        }

        public bool Equals(PlayerData other)
        {
            return clientId == other.clientId && name.Equals(other.name) &&
                   type.Equals(other.type) && role.Equals(other.role);
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref name);
            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref role);
        }
    }
}