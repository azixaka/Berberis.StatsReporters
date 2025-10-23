using System.Collections.Generic;

namespace Berberis.StatsReporters;

public readonly struct NetworkStats
{
    public readonly float IntervalMs;
    public readonly long BytesReceived;
    public readonly long BytesSent;
    public readonly float ReceivedBytesPerSecond;
    public readonly float SentBytesPerSecond;

    public readonly KeyValuePair<string, InterfaceStats>[] InterfaceStats;

    public NetworkStats(float intervalMs, KeyValuePair<string, InterfaceStats>[] interfaceStats,
        InterfaceStats primaryInterface)
    {
        IntervalMs = intervalMs;
        InterfaceStats = interfaceStats;
        BytesReceived = primaryInterface.BytesReceived;
        BytesSent = primaryInterface.BytesSent;
        ReceivedBytesPerSecond = primaryInterface.ReceivedBytesPerSecond;
        SentBytesPerSecond = primaryInterface.SentBytesPerSecond;
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