namespace Berberis.StatsReporters;

public readonly struct NetworkStats
{
    public readonly float IntervalMs;
    public readonly long BytesReceived;
    public readonly long BytesSent;
    public readonly float ReceivedBytesPerSecond;
    public readonly float SentBytesPerSecond;

    public NetworkStats(float intervalMs,
        long bytesReceived, long bytesSent,
        float receivedBytesPerSecond, float sentBytesPerSecond)
    {
        IntervalMs = intervalMs;
        BytesReceived = bytesReceived;
        BytesSent = bytesSent;
        ReceivedBytesPerSecond = receivedBytesPerSecond;
        SentBytesPerSecond = sentBytesPerSecond;
    }
}
