using System;
using System.Diagnostics;
using System.Threading;

namespace Berberis.StatsReporters;

/// <summary>
/// Tracks throughput, bandwidth, and service time metrics for a named source.
/// Thread-safe for concurrent recording; statistics are interval-based.
/// </summary>
public sealed class StatsReporter
{
    // Hot fields: Updated frequently by multiple threads concurrently
    // Grouped together and padded to prevent false sharing with cold fields
    private long _totalMessages;
    private long _totalServiceTicks;
    private double _totalServiceTime;
    private long _totalBytes;

    // Padding to ensure hot fields are on separate cache line from cold fields
    // Cache lines are typically 64 bytes; 7 longs = 56 bytes padding
#pragma warning disable CS0169 // Field is never used - intentional padding for cache line alignment
    private long _padding1, _padding2, _padding3, _padding4, _padding5, _padding6, _padding7;
#pragma warning restore CS0169

    // Cold fields: Only accessed during GetStats() under lock
    private long _lastMessages;
    private long _lastServiceTicks;
    private long _lastBytes;
    private long _lastTicks;

    private volatile bool _changed;

    /// <summary>
    /// Gets the name of this reporter's data source.
    /// </summary>
    public string Source { get; }

    private readonly Action<StatsReporter> _disposeAction;
    private readonly object _syncObj = new();

    public StatsReporter(string source) : this(source, null) { }

    /// <summary>
    /// Creates a reporter for the specified source.
    /// </summary>
    /// <param name="source">Name identifying this metric source.</param>
    /// <param name="disposeAction">Optional cleanup action when disposed.</param>
    public StatsReporter(string source, Action<StatsReporter> disposeAction)
    {
        Source = source;
        _disposeAction = disposeAction;

        _lastTicks = Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Starts timing an operation. Returns a timestamp to pass to <see cref="Stop(long)"/>.
    /// </summary>
    public long Start()
    {
        return Stopwatch.GetTimestamp();
    }

    /// <summary>
    /// Records completion of an operation started with <see cref="Start"/>.
    /// </summary>
    /// <param name="startTicks">Timestamp from <see cref="Start"/>.</param>
    public void Stop(long startTicks)
    {
        Interlocked.Increment(ref _totalMessages);
        Interlocked.Add(ref _totalServiceTicks, Stopwatch.GetTimestamp() - startTicks);

#pragma warning disable CS0420 // Reference to volatile field - Volatile.Write provides correct semantics
        Volatile.Write(ref _changed, true);
#pragma warning restore CS0420
    }

    /// <summary>
    /// Records completion of an operation with byte count.
    /// </summary>
    /// <param name="startTicks">Timestamp from <see cref="Start"/>.</param>
    /// <param name="bytes">Number of bytes processed.</param>
    public void Stop(long startTicks, long bytes)
    {
        Interlocked.Increment(ref _totalMessages);
        Interlocked.Add(ref _totalServiceTicks, Stopwatch.GetTimestamp() - startTicks);
        Interlocked.Add(ref _totalBytes, bytes);

#pragma warning disable CS0420 // Reference to volatile field - Volatile.Write provides correct semantics
        Volatile.Write(ref _changed, true);
#pragma warning restore CS0420
    }

    /// <summary>
    /// Records pre-calculated metrics without using <see cref="Start"/>/<see cref="Stop"/>.
    /// </summary>
    /// <param name="units">Number of operations completed.</param>
    /// <param name="serviceTimeMs">Total service time in milliseconds.</param>
    /// <param name="bytes">Total bytes processed.</param>
    public void Record(long units, float serviceTimeMs, long bytes)
    {
        Interlocked.Add(ref _totalMessages, units);

        // WARNING: Floating-point equality in CompareExchange loop is unreliable and may cause
        // infinite loops or incorrect totals. Consider replacing with lock-based approach.
        double currTotal, newTotal;
        do
        {
            currTotal = _totalServiceTime;
            newTotal = currTotal + serviceTimeMs;
        } while (currTotal != Interlocked.CompareExchange(ref _totalServiceTime, newTotal, currTotal));

        Interlocked.Add(ref _totalBytes, bytes);

#pragma warning disable CS0420 // Reference to volatile field - Volatile.Write provides correct semantics
        Volatile.Write(ref _changed, true);
#pragma warning restore CS0420
    }

    /// <summary>
    /// Computes interval-based statistics since last call. Resets interval counters.
    /// </summary>
    public Stats GetStats()
    {
        var totalMesssages = Interlocked.Read(ref _totalMessages);
        var totalServiceTicks = Interlocked.Read(ref _totalServiceTicks);
        var totalBytes = Interlocked.Read(ref _totalBytes);

        long intervalMessages;
        long intervalSvcTicks;
        long intervalBytes;
        float timePassed;

        lock (_syncObj)
        {
            intervalMessages = totalMesssages - _lastMessages;
            intervalSvcTicks = totalServiceTicks - _lastServiceTicks;
            intervalBytes = totalBytes - _lastBytes;

            _lastMessages = totalMesssages;
            _lastServiceTicks = totalServiceTicks;
            _lastBytes = totalBytes;

            var ticks = Stopwatch.GetTimestamp();
            timePassed = (float)(ticks - _lastTicks) / Stopwatch.Frequency;
            _lastTicks = ticks;

            _changed = false;
        }

        var intervalSvcTimeMs = intervalSvcTicks / (float)Stopwatch.Frequency * 1000;
        var avgServiceTime = intervalMessages == 0 ? 0 : intervalSvcTimeMs / intervalMessages;

        return new Stats(timePassed * 1000,
            intervalMessages / timePassed,
            totalMesssages,
            intervalBytes / timePassed,
            totalBytes,
            avgServiceTime);
    }

    /// <summary>
    /// Gets whether any metrics have changed since last <see cref="GetStats"/> call.
    /// </summary>
    public bool IsChanged => _changed;

    public void Dispose() => _disposeAction?.Invoke(this);

    /// <summary>
    /// Returns elapsed time in seconds since the given timestamp.
    /// </summary>
    public static float ElapsedSince(long ticks)
    {
        return (Stopwatch.GetTimestamp() - ticks) / (float)Stopwatch.Frequency;
    }
}
