using System;
using System.Diagnostics;
using System.Threading;

namespace Berberis.StatsReporters;

public sealed class ThreadPoolLatencyTracker
{
    private readonly ThreadLocal<long> _threadLastValue = new(() => 0, false);
    private readonly int _numberOfMeasurements;

    public ThreadPoolLatencyTracker(int numberOfMeasurements = 10_000)
    {
        if (numberOfMeasurements <= 0) throw new ArgumentOutOfRangeException(nameof(numberOfMeasurements));
        _numberOfMeasurements = numberOfMeasurements;
    }

    public LatencyStats Measure()
    {
        var ctx = new RunContext(_numberOfMeasurements, Stopwatch.GetTimestamp());

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
            return new LatencyStats(0, 0, 0, 0);
        }

        decimal medianMs = ToMilliseconds(CalculatePercentile(valid, 50));
        decimal p90Ms = ToMilliseconds(CalculatePercentile(valid, 90));
        decimal p99Ms = ToMilliseconds(CalculatePercentile(valid, 99));
        decimal p9999Ms = ToMilliseconds(CalculatePercentile(valid, 99.99f));

        return new LatencyStats((double)medianMs, (double)p90Ms, (double)p99Ms, (double)p9999Ms);
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

public readonly record struct LatencyStats(double MedianMs, double P90Ms, double P99Ms, double P99_99Ms);
