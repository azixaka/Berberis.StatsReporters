namespace Berberis.StatsReporters;

public readonly struct NetworkStats
{
    public readonly float IntervalMs;

    public readonly (string Name, InterfaceStats Stats)[] InterfaceStats;

    public NetworkStats(float intervalMs, (string Name, InterfaceStats Stats)[] interfaceStats)
    {
        IntervalMs = intervalMs;
        InterfaceStats = interfaceStats;
    }
}

public readonly struct InterfaceStats
{
    public readonly long BytesReceived;
    public readonly long BytesSent;
    public readonly float ReceivedBytesPerSecond;
    public readonly float SentBytesPerSecond;

    public InterfaceStats(long bytesReceived, long bytesSent, float receivedBytesPerSecond, float sentBytesPerSecond)
    {
        BytesReceived = bytesReceived;
        BytesSent = bytesSent;
        ReceivedBytesPerSecond = receivedBytesPerSecond;
        SentBytesPerSecond = sentBytesPerSecond;
    }
}
