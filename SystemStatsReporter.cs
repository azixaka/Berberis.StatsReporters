using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;

namespace Berberis.StatsReporters;

public sealed class SystemStatsReporter
{
    private float _lastSpentMs;
    private long _lastTicks;

    private long _lastGc0;
    private long _lastGc1;
    private long _lastGc2;

    private long _lastPromotedBytes;

    private long _lastCompletedWorkItems;

    private readonly object _syncObj = new();
    private readonly Process _process;
    private readonly int _cpus;

    private readonly Dictionary<string, string> _systemInfo;

    public SystemStatsReporter()
    {
        _process = Process.GetCurrentProcess();
        _cpus = Environment.ProcessorCount;

        ThreadPool.GetMinThreads(out var minThreads, out var minIoThreads);
        ThreadPool.GetMaxThreads(out var maxThreads, out var maxIoThreads);

        var gcInfo = GC.GetGCMemoryInfo(GCKind.Any);

        _systemInfo = new Dictionary<string, string>
        {
                    {"Process Id", _process.Id.ToString()},
                    {"Process Name", _process.ProcessName},
                    {"Start Time", _process.StartTime.ToString("dd/MM/yyyy HH:mm:ss.fff")},
                    {"Command Line", Environment.CommandLine },
                    {"Process Path", Environment.ProcessPath },
                    {"CurrentDirectory", Environment.CurrentDirectory },
                    {"MachineName", Environment.MachineName },
                    {"UserName", Environment.UserName },
                    {"UserDomainName", Environment.UserDomainName },
                    {"CPU Cores", _cpus.ToString()},
                    {"CLR Version", Environment.Version.ToString()},
                    {"FrameworkDescription", RuntimeInformation.FrameworkDescription},
                    {"OS Version", Environment.OSVersion.ToString()},
                    {"OS Architecture", RuntimeInformation.OSArchitecture.ToString()},
                    {"Process Architecture", RuntimeInformation.ProcessArchitecture.ToString()},
                    {"Runtime Identifier", RuntimeInformation.RuntimeIdentifier },
                    {"App Version", AppVersion},
                    {"GC Server Mode", GCSettings.IsServerGC.ToString()},
                    {"GC Latency Mode", GCSettings.LatencyMode.ToString()},
                    {"IsHighResolutionTimer", Stopwatch.IsHighResolution.ToString() },
                    {"TimerFrequency", Stopwatch.Frequency.ToString() },
                    { "ThreadPool.MinWorkerThreads", minThreads.ToString() },
                    { "ThreadPool.MinIOThreads", minIoThreads.ToString() },
                    { "ThreadPool.MaxWorkerThreads", maxThreads.ToString() },
                    { "ThreadPool.MaxIOThreads", maxIoThreads.ToString() },
                    { "TotalAvailableMemoryBytes", gcInfo.TotalAvailableMemoryBytes.ToString() }
        };

        _lastTicks = Stopwatch.GetTimestamp();
    }

    public IReadOnlyDictionary<string, string> SystemInfo => _systemInfo;

    public static string AppVersion => Assembly.GetEntryAssembly()?.GetName().Version?.ToString();

    public SystemStats GetStats()
    {
        _process.Refresh();
        var totalSpent = (float)_process.TotalProcessorTime.TotalMilliseconds;

        float timePassed;
        float intervalSpent;
        float rateCompletedWorkItems, rateGc0, rateGc1, rateGc2;
        long promotedBytesInterval, gc0, gc1, gc2;
        var completedWorkItems = ThreadPool.CompletedWorkItemCount;
        var gcInfo = GC.GetGCMemoryInfo(GCKind.Any);

        lock (_syncObj)
        {
            var ticks = Stopwatch.GetTimestamp();
            timePassed = (float)(ticks - _lastTicks) / Stopwatch.Frequency * 1000;
            _lastTicks = ticks;

            intervalSpent = totalSpent - _lastSpentMs;
            _lastSpentMs = totalSpent;

            var ratio = 1000 / timePassed;

            rateCompletedWorkItems = (completedWorkItems - _lastCompletedWorkItems) * ratio;
            _lastCompletedWorkItems = completedWorkItems;

            gc0 = GC.CollectionCount(0);
            rateGc0 = (gc0 - _lastGc0) * ratio;
            _lastGc0 = gc0;

            gc1 = GC.CollectionCount(1);
            rateGc1 = (gc1 - _lastGc1) * ratio;
            _lastGc1 = gc1;

            gc2 = GC.CollectionCount(2);
            rateGc2 = (gc2 - _lastGc2) * ratio;
            _lastGc2 = gc2;

            promotedBytesInterval = gcInfo.PromotedBytes - _lastPromotedBytes;
            _lastPromotedBytes = gcInfo.PromotedBytes;
        }

        var gcIteration = gcInfo.Index;
        var pauseTimePercentage = (float)gcInfo.PauseTimePercentage;
        var percentUsed = intervalSpent / timePassed;

        return new SystemStats(timePassed, totalSpent, intervalSpent, percentUsed,
            rateGc0, rateGc1, rateGc2,
            gcIteration,
            pauseTimePercentage,
            (float)gcInfo.PauseDurations[0].TotalMilliseconds,
            gcInfo.PauseDurations.Length > 1 ? (float)gcInfo.PauseDurations[1].TotalMilliseconds : 0,
            gc0, gc1, gc2,
            _process.WorkingSet64,
            GC.GetTotalMemory(false),
            _process.Threads.Count,
            ThreadPool.ThreadCount,
            ThreadPool.PendingWorkItemCount,
            completedWorkItems,
            rateCompletedWorkItems,
            gcInfo.Compacted,
            gcInfo.Concurrent,
            promotedBytesInterval,
            Monitor.LockContentionCount);
    }
}
