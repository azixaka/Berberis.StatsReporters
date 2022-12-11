namespace Berberis.StatsReporters;

public readonly struct ServiceTimeStats
{
    /// <summary>
    /// Interval window in milliseconds for which these calculation were made
    /// </summary>
    public readonly float IntervalMs;

    /// <summary>
    /// Processing message rate observed in this interval, in msg/s
    /// </summary>
    public readonly float ProcessRate;

    /// <summary>
    /// Total number of processed messages
    /// </summary>
    public readonly long TotalProcessedMessages;

    /// <summary>
    /// Average service time in this interval, in milliseconds, i.e how long it took on average to process an operation
    /// </summary>
    public readonly float AvgServiceTimeMs;

    /// <summary>
    /// Min service time in this interval, in milliseconds, i.e how long it took on average to process an operation
    /// </summary>
    public readonly float MinServiceTimeMs;

    /// <summary>
    /// Max service time in this interval, in milliseconds, i.e how long it took on average to process an operation
    /// </summary>
    public readonly float MaxServiceTimeMs;

    /// <summary>
    /// N-th percentile service time in milliseconds, i.e how long it took on average to process an operation
    /// </summary>
    public readonly (float percentile, float value)[] PercentileValues;

    public ServiceTimeStats(float intervalMs,
                    float processRate,
                    long totalProcessedMessages,
                    float avgServiceTimeMs,
                    float minServiceTimeMs,
                    float maxServiceTimeMs,
                    (float percentile, float value)[] percentiles)
    {
        IntervalMs = intervalMs;
        ProcessRate = processRate;
        TotalProcessedMessages = totalProcessedMessages;
        AvgServiceTimeMs = avgServiceTimeMs;
        MinServiceTimeMs = minServiceTimeMs;
        MaxServiceTimeMs = maxServiceTimeMs;
        PercentileValues = percentiles;
    }
}
