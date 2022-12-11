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

        _systemInfo = new Dictionary<string, string>
                {
                    {"Process Id", _process.Id.ToString()},
                    {"Process Name", _process.ProcessName},
                    {"Start Time", _process.StartTime.ToString("dd/MM/yyyy HH:mm:ss.fff")},
                    {"Command Line", Environment.CommandLine },
                    {"Process Path", Environment.ProcessPath },
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
                    { "ThreadPool.MinWorkerThreads", minThreads.ToString() },
                    { "ThreadPool.MinIOThreads", minIoThreads.ToString() },
                    { "ThreadPool.MaxWorkerThreads", maxThreads.ToString() },
                    { "ThreadPool.MaxIOThreads", maxIoThreads.ToString() }
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
        float rateGc0, rateGc1, rateGc2;
        long gc0, gc1, gc2;

        lock (_syncObj)
        {
            var ticks = Stopwatch.GetTimestamp();
            timePassed = (float)(ticks - _lastTicks) / Stopwatch.Frequency * 1000;
            _lastTicks = ticks;

            intervalSpent = totalSpent - _lastSpentMs;
            _lastSpentMs = totalSpent;

            gc0 = GC.CollectionCount(0);
            rateGc0 = (gc0 - _lastGc0) * 1000 / timePassed;
            _lastGc0 = gc0;

            gc1 = GC.CollectionCount(1);
            rateGc1 = (gc1 - _lastGc1) * 1000 / timePassed;
            _lastGc1 = gc1;

            gc2 = GC.CollectionCount(2);
            rateGc2 = (gc2 - _lastGc2) * 1000 / timePassed;
            _lastGc2 = gc2;
        }

        var gcInfo = GC.GetGCMemoryInfo(GCKind.Any);
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
            ThreadPool.PendingWorkItemCount);
    }
}
