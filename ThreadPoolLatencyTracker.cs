using System.Diagnostics;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace Berberis.StatsReporters;

public sealed class ThreadPoolLatencyTracker
{
    private ThreadLocal<long> threadLastValue = new(false);

    private long[] values;
    private int globalIndex;
    private long epoch;

    private int completedOps;

    public ThreadPoolLatencyTracker(int numberOfMeasurements = 100_000)
    {
        values = new long[numberOfMeasurements];
    }

    public ValueTask<LatencyStats> Measure()
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

            while (completedOps < values.Length)
            {
                spinWait.SpinOnce();
            }
        }

        //Process results
        Array.Sort(values);

        decimal median = (CalculatePercentile(values, 50) * 1E3m) / Stopwatch.Frequency;
        decimal percentile90 = (CalculatePercentile(values, 90) * 1E3m) / Stopwatch.Frequency;
        decimal percentile99 = (CalculatePercentile(values, 99) * 1E3m) / Stopwatch.Frequency;
        decimal percentile9999 = (CalculatePercentile(values, 99.99f) * 1E3m) / Stopwatch.Frequency;

        return ValueTask.FromResult(new LatencyStats((double)median, (double)percentile90, (double)percentile99, (double)percentile9999));
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

public readonly record struct LatencyStats(double MedianMs, double Percentile90Ms, double Percentile99Ms, double Percentile9999Ms);
