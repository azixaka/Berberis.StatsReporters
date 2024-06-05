using System.Diagnostics;
using System.Threading;
using System;

namespace Berberis.StatsReporters;

public sealed class ThreadPoolLatencyTracker
{
    private ThreadLocal<long> threadLastValue = new(false);

    private long[] values;
    private int globalIndex;
    private long epoch;

    private int completedOps;

    public ThreadPoolLatencyTracker(int numberOfMeasurements = 10_000)
    {
        values = new long[numberOfMeasurements];
    }

    public LatencyStats Measure()
    {
        //SetUp
        epoch = Stopwatch.GetTimestamp();
        globalIndex = -1;
        completedOps = 0;

        //Schedule
        for (int i = 0; i < values.Length; i++)
        {
            ThreadPool.QueueUserWorkItem(WorkItem);
        }

        //Await completion
        if (completedOps < values.Length)
        {
            var spinWait = new SpinWait();

            while (completedOps < values.Length && spinWait.Count < 1000)
            {
                spinWait.SpinOnce();
            }
        }

        //Process results
        Array.Sort(values);

        decimal median = (CalculatePercentile(values, 50) * 1E3m) / Stopwatch.Frequency;
        decimal p90 = (CalculatePercentile(values, 90) * 1E3m) / Stopwatch.Frequency;
        decimal p99 = (CalculatePercentile(values, 99) * 1E3m) / Stopwatch.Frequency;
        decimal p99_99 = (CalculatePercentile(values, 99.99f) * 1E3m) / Stopwatch.Frequency;

        return new LatencyStats((double)median, (double)p90, (double)p99, (double)p99_99);
    }

    private void WorkItem(object _)
    {
        var ts = Stopwatch.GetTimestamp();
        var index = Interlocked.Increment(ref globalIndex);

        var last = threadLastValue.Value;

        if (last >= epoch)
            values[index] = ts - last;

        Interlocked.Increment(ref completedOps);
        threadLastValue.Value = Stopwatch.GetTimestamp();
    }

    private static long CalculatePercentile(long[] sortedValues, float percentile)
    {
        int index = (int)Math.Ceiling((percentile / 100.0) * sortedValues.Length) - 1;
        return sortedValues[index];
    }
}

public readonly record struct LatencyStats(double MedianMs, double P90Ms, double P99Ms, double P99_99Ms);
