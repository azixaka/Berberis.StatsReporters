using System.Diagnostics;
using System.Threading;

namespace Berberis.StatsReporters;

/// <summary>
/// Tracks service time statistics with EWMA and configurable percentiles.
/// Thread-safe for concurrent recording.
/// </summary>
public sealed class ServiceTimeTracker
{
    /// <summary>
    /// Returns current high-resolution timestamp for timing operations.
    /// </summary>
    public static long GetTicks() => Stopwatch.GetTimestamp();

    private readonly float MsRatio = 1000f / Stopwatch.Frequency;
    private long _totalMessages;
    private long _lastMessages;

    private long _lastTicks;
    private readonly object _syncObj = new();

    private readonly ExponentialWeightedMovingAverage _svcTimeEwma;
    private readonly MovingPercentile[] _svcTimePercentiles;

    /// <summary>
    /// Creates a service time tracker with optional percentile tracking.
    /// </summary>
    /// <param name="ewmaWindowSize">Window size for exponential weighted moving average. Default is 50.</param>
    /// <param name="percentileOptions">Optional percentiles to track (e.g., P95, P99).</param>
    public ServiceTimeTracker(int ewmaWindowSize = 50, PercentileOptions[] percentileOptions = null)
    {
        _svcTimeEwma = new(ewmaWindowSize);

        PercentileOptions = percentileOptions;

        if (percentileOptions != null)
        {
            _svcTimePercentiles = new MovingPercentile[percentileOptions.Length];

            for (int i = 0; i < _svcTimePercentiles.Length; i++)
            {
                _svcTimePercentiles[i] = new MovingPercentile(percentileOptions[i]);
            }
        }

        _lastTicks = GetTicks();
    }

    /// <summary>
    /// Gets the configured percentile options.
    /// </summary>
    public PercentileOptions[] PercentileOptions { get; }

    /// <summary>
    /// Records service time for an operation. Returns elapsed ticks.
    /// </summary>
    /// <param name="startTicks">Timestamp from <see cref="GetTicks"/>.</param>
    public long RecordServiceTime(long startTicks) => RecordServiceTime(startTicks, GetTicks());

    /// <summary>
    /// Records service time between two timestamps. Returns elapsed ticks.
    /// </summary>
    /// <param name="startTicks">Start timestamp.</param>
    /// <param name="endTicks">End timestamp.</param>
    public long RecordServiceTime(long startTicks, long endTicks)
    {
        var svcTime = endTicks - startTicks;

        Interlocked.Increment(ref _totalMessages);

        lock (_syncObj)
        {
            _svcTimeEwma.NewSample(svcTime);

            if (_svcTimePercentiles != null)
            {
                for (int i = 0; i < _svcTimePercentiles.Length; i++)
                {
                    _svcTimePercentiles[i].NewSample(svcTime, _svcTimeEwma.AverageValue);
                }
            }
        }

        return svcTime;
    }

    /// <summary>
    /// Computes service time statistics.
    /// </summary>
    /// <param name="reset">If true, resets interval counters and moving averages.</param>
    public ServiceTimeStats GetStats(bool reset)
    {
        var ticks = GetTicks();

        var totalMesssages = Interlocked.Read(ref _totalMessages);

        long intervalMessages;

        float timePassed;

        float svcAvg;
        float svcMin;
        float svcMax;

        (float percentile, float value)[] percentileValues = _svcTimePercentiles != null
                                                                ? new (float percentile, float value)[_svcTimePercentiles.Length]
                                                                : null;

        lock (_syncObj)
        {
            intervalMessages = totalMesssages - _lastMessages;

            timePassed = ticks - _lastTicks;

            svcAvg = _svcTimeEwma.AverageValue;
            svcMin = _svcTimeEwma.MinValue;
            svcMax = _svcTimeEwma.MaxValue;

            if (percentileValues != null)
            {
                for (int i = 0; i < _svcTimePercentiles.Length; i++)
                {
                    percentileValues[i] = (_svcTimePercentiles[i].Percentile, _svcTimePercentiles[i].PercentileValue * MsRatio);
                }
            }

            if (reset)
            {
                _lastMessages = totalMesssages;

                _lastTicks = ticks;

                _svcTimeEwma.Reset();

                if (percentileValues != null)
                {
                    for (int i = 0; i < _svcTimePercentiles.Length; i++)
                    {
                        _svcTimePercentiles[i].Reset();
                    }
                }
            }
        }

        return new ServiceTimeStats(timePassed * MsRatio,
                                    intervalMessages,
                                    totalMesssages,
                                    svcAvg * MsRatio,
                                    svcMin * MsRatio,
                                    svcMax * MsRatio,
                                    percentileValues);
    }
}
