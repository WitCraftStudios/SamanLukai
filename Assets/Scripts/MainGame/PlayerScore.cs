using Unity.Netcode;

public struct PlayerScore : INetworkSerializable, System.IEquatable<PlayerScore>
{
    public ulong clientId;
    public int score;

    public PlayerScore(ulong clientId, int score)
    {
        this.clientId = clientId;
        this.score = score;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref clientId);
        serializer.SerializeValue(ref score);
    }

    public bool Equals(PlayerScore other)
    {
        return clientId == other.clientId && score == other.score;
    }

    public override string ToString() => $"Client {clientId}: {score}";
}
