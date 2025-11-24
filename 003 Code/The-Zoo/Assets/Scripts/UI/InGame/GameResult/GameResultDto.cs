using Players.Common;
using Unity.Collections;
using Unity.Netcode;

namespace UI.InGame.GameResult
{
    public struct GameResultDto : INetworkSerializable
    {
        public ulong clientId;
        public Role role;
        public FixedString32Bytes name;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref clientId);
            serializer.SerializeValue(ref role);
            serializer.SerializeValue(ref name);
        }

        public override string ToString()
        {
            return $"Client: {clientId} Role: {role} Name: {name}";
        }
    }
}