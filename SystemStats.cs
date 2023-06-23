namespace Berberis.StatsReporters;

public readonly struct SystemStats
{
    public readonly float IntervalMs;
    public readonly float CpuTimeTotalMs;
    public readonly float CpuTimeIntMs;
    public readonly float CpuLoad;
    public readonly float Gc0Collections;
    public readonly float Gc1Collections;
    public readonly float Gc2Collections;
    public readonly long GcIterationIndex;
    public readonly float PauseTimePercentage;
    public readonly float GcTimeBlockingMs;
    public readonly float GcTimeBackgroundMs;
    public readonly long TotalGc0s;
    public readonly long TotalGc1s;
    public readonly long TotalGc2s;
    public readonly float WorkSetBytes;
    public readonly float GcMemory;
    public readonly int NumberOfThreads;
    public readonly int ThreadPoolThreads;
    public readonly long PendingThreadPoolWorkItems;
    public readonly long CompletedThreadPoolWorkItems;
    public readonly float CompletedThreadPoolWorkItemsPerSecond;
    public readonly bool GcCompacted;
    public readonly bool GcBackground;
    public readonly long PromotedBytesInterval;
    public readonly long LockContentions;

    public SystemStats(float intervalMs,
        float cpuTimeTotalMs,
        float cpuTimeIntMs,
        float cpuLoad,
        float gc0Collections,
        float gc1Collections,
        float gc2Collections,
        long gcIterationIndex,
        float pauseTimePercentage,
        float gcTimeBlockingMs,
        float gcTimeBackgroundMs,
        long totalGc0s,
        long totalGc1s,
        long totalGc2s,
        float workSetBytes,
        float gcMemory,
        int numberOfThreads,
        int threadPoolThreads,
        long pendingThreadPoolWorkItems,
        long completedThreadPoolWorkItems,
        float completedThreadPoolWorkItemsPerSecond,
        bool gcCompacted,
        bool gcBackground,
        long promotedBytesInterval,
        long lockContentions)
    {
        IntervalMs = intervalMs;
        CpuTimeTotalMs = cpuTimeTotalMs;
        CpuTimeIntMs = cpuTimeIntMs;
        CpuLoad = cpuLoad;
        Gc0Collections = gc0Collections;
        Gc1Collections = gc1Collections;
        Gc2Collections = gc2Collections;
        GcIterationIndex = gcIterationIndex;
        PauseTimePercentage = pauseTimePercentage;
        GcTimeBlockingMs = gcTimeBlockingMs;
        GcTimeBackgroundMs = gcTimeBackgroundMs;
        TotalGc0s = totalGc0s;
        TotalGc1s = totalGc1s;
        TotalGc2s = totalGc2s;
        WorkSetBytes = workSetBytes;
        GcMemory = gcMemory;
        NumberOfThreads = numberOfThreads;
        ThreadPoolThreads = threadPoolThreads;
        PendingThreadPoolWorkItems = pendingThreadPoolWorkItems;
        CompletedThreadPoolWorkItems = completedThreadPoolWorkItems;
        CompletedThreadPoolWorkItemsPerSecond = completedThreadPoolWorkItemsPerSecond;
        GcCompacted = gcCompacted;
        GcBackground = gcBackground;
        PromotedBytesInterval = promotedBytesInterval;
        LockContentions = lockContentions;
    }
}
