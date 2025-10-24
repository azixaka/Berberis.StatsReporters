using System;
using System.Diagnostics;
using System.Threading;

namespace Berberis.StatsReporters;

/// <summary>
/// Tracks ThreadPool latency by measuring scheduling delays across multiple work items.
/// Results are cached to prevent redundant measurements and ensure only one measurement runs at a time.
/// </summary>
public sealed class ThreadPoolLatencyTracker
{
    private readonly ThreadLocal<long> _threadLastValue = new(() => 0, false);
    private readonly int _numberOfMeasurements;
    private readonly object _measureLock = new();
    private readonly TimeSpan _cacheDuration;

    private LatencyStats _cachedStats;
    private long _cachedTimestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadPoolLatencyTracker"/> class.
    /// </summary>
    /// <param name="numberOfMeasurements">The number of work items to schedule for each measurement. Default is 10,000.</param>
    /// <param name="cacheDuration">
    /// How long to cache measurement results. Default is 5 seconds.
    /// Pass <see cref="TimeSpan.Zero"/> to disable caching and measure on every call.
    /// </param>
    public ThreadPoolLatencyTracker(int numberOfMeasurements = 10_000, TimeSpan? cacheDuration = null)
    {
        if (numberOfMeasurements <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfMeasurements));
        _numberOfMeasurements = numberOfMeasurements;
        _cacheDuration = cacheDuration ?? TimeSpan.FromSeconds(5);
    }

    /// <summary>
    /// Measures ThreadPool latency statistics.
    /// If called within the cache duration, returns cached results.
    /// If multiple threads call simultaneously, only one measurement occurs and all callers receive the same result.
    /// </summary>
    /// <returns>Latency statistics including median, P90, P99, and P99.99 percentiles in milliseconds.</returns>
    public LatencyStats Measure()
    {
        lock (_measureLock)
        {
            var now = Stopwatch.GetTimestamp();
            var ageTicks = now - _cachedTimestamp;
            var ageMs = (ageTicks * 1000.0) / Stopwatch.Frequency;

            // Return cached result if fresh enough
            if (ageMs < _cacheDuration.TotalMilliseconds)
            {
                return _cachedStats;
            }

            // Perform actual measurement
            var ctx = new RunContext(_numberOfMeasurements, now);

            // Schedule
            for (int i = 0; i < ctx.Values.Length; i++)
            {
                ThreadPool.QueueUserWorkItem(WorkItem, ctx);
            }

            // Await completion
            var spin = new SpinWait();
            while (Volatile.Read(ref ctx.CompletedOps) < ctx.Values.Length)
            {
                spin.SpinOnce();
            }

            // Ensure memory visibility before reading Values
            Thread.MemoryBarrier();

            // Filter out invalid measurements (zeros) and sort
            Span<long> valid = ctx.Values.AsSpan(0, ctx.ValidCount);
            valid.Sort();

            // Handle edge case where we have no valid measurements
            if (valid.Length == 0)
            {
                var emptyStats = new LatencyStats(0, 0, 0, 0);
                _cachedStats = emptyStats;
                _cachedTimestamp = now;
                return emptyStats;
            }

            decimal medianMs = ToMilliseconds(CalculatePercentile(valid, 50));
            decimal p90Ms = ToMilliseconds(CalculatePercentile(valid, 90));
            decimal p99Ms = ToMilliseconds(CalculatePercentile(valid, 99));
            decimal p9999Ms = ToMilliseconds(CalculatePercentile(valid, 99.99f));

            var stats = new LatencyStats((double)medianMs, (double)p90Ms, (double)p99Ms, (double)p9999Ms);

            // Cache the result
            _cachedStats = stats;
            _cachedTimestamp = now;

            return stats;
        }
    }

    private static decimal ToMilliseconds(long ticks) =>
        (ticks * 1E3m) / Stopwatch.Frequency;

    private void WorkItem(object? state)
    {
        var ctx = (RunContext)state!;
        var now = Stopwatch.GetTimestamp();
        var last = _threadLastValue.Value;

        // Only record measurement if this thread ran before in this measurement cycle
        if (last >= ctx.Epoch)
        {
            var idx = Interlocked.Increment(ref ctx.ValidCount) - 1;
            if ((uint)idx < (uint)ctx.Values.Length)
            {
                ctx.Values[idx] = now - last;
            }
        }

        _threadLastValue.Value = now;
        Interlocked.Increment(ref ctx.CompletedOps);
    }

    private static long CalculatePercentile(Span<long> sortedValues, float percentile)
    {
        if (sortedValues.Length == 0) return 0;
        int index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Length) - 1;
        if (index < 0) index = 0;
        if (index >= sortedValues.Length) index = sortedValues.Length - 1;
        return sortedValues[index];
    }

    private sealed class RunContext
    {
        public readonly long[] Values;
        public readonly long Epoch;
        public int ValidCount;  // Tracks how many valid measurements we have
        public int CompletedOps;

        public RunContext(int n, long epoch)
        {
            Values = new long[n];
            Epoch = epoch;
            ValidCount = 0;
            CompletedOps = 0;
        }
    }
}

/// <summary>
/// Represents ThreadPool latency statistics in milliseconds.
/// </summary>
/// <param name="MedianMs">Median (50th percentile) latency in milliseconds.</param>
/// <param name="P90Ms">90th percentile latency in milliseconds.</param>
/// <param name="P99Ms">99th percentile latency in milliseconds.</param>
/// <param name="P99_99Ms">99.99th percentile latency in milliseconds.</param>
public readonly record struct LatencyStats(double MedianMs, double P90Ms, double P99Ms, double P99_99Ms);
