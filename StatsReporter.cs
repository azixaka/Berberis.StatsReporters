using System;
using System.Diagnostics;
using System.Threading;

namespace Berberis.StatsReporters;

public sealed class StatsReporter
{
    private long _totalMessages;
    private long _lastMessages;

    private long _totalServiceTicks;
    private long _lastServiceTicks;

    private double _totalServiceTime;

    private long _totalBytes;
    private long _lastBytes;

    private long _lastTicks;

    private volatile bool _changed;

    public string Source { get; }

    private readonly Action<StatsReporter> _disposeAction;
    private readonly object _syncObj = new();

    public StatsReporter(string source) : this(source, null) { }

    public StatsReporter(string source, Action<StatsReporter> disposeAction)
    {
        Source = source;
        _disposeAction = disposeAction;
    }

    public long Start()
    {
        return Stopwatch.GetTimestamp();
    }

    public void Stop(long startTicks)
    {
        Interlocked.Increment(ref _totalMessages);
        Interlocked.Add(ref _totalServiceTicks, Stopwatch.GetTimestamp() - startTicks);

        _changed = true;
    }

    public void Stop(long startTicks, long bytes)
    {
        Interlocked.Increment(ref _totalMessages);
        Interlocked.Add(ref _totalServiceTicks, Stopwatch.GetTimestamp() - startTicks);
        Interlocked.Add(ref _totalBytes, bytes);

        _changed = true;
    }

    public void Record(long units, float serviceTimeMs, long bytes)
    {
        Interlocked.Add(ref _totalMessages, units);

        double currTotal, newTotal;
        do
        {
            currTotal = _totalServiceTime;
            newTotal = currTotal + serviceTimeMs;
        } while (currTotal != Interlocked.CompareExchange(ref _totalServiceTime, newTotal, currTotal));


        Interlocked.Exchange(ref _totalServiceTime, _totalServiceTime + serviceTimeMs);
        Interlocked.Add(ref _totalBytes, bytes);

        _changed = true;
    }

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
            intervalBytes,
            totalBytes,
            avgServiceTime);
    }

    public bool IsChanged => _changed;

    public void Dispose() => _disposeAction?.Invoke(this);

    public static float ElapsedSince(long ticks)
    {
        return (Stopwatch.GetTimestamp() - ticks) / (float)Stopwatch.Frequency;
    }
}
