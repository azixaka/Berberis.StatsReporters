using System.Diagnostics;
using System.Threading;

namespace Berberis.StatsReporters;

public sealed class ServiceTimeTracker
{
    public static long GetTicks() => Stopwatch.GetTimestamp();
    internal static float MsRatio => 1000f / Stopwatch.Frequency;

    private long _totalMessages;
    private long _lastMessages;

    private long _lastTicks;
    private object _syncObj = new();

    private readonly ExponentialWeightedMovingAverage _svcTimeEwma;
    private readonly MovingPercentile[] _svcTimePercentiles;

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

    public PercentileOptions[] PercentileOptions { get; }

    public long RecordServiceTime(long startTicks)
    {
        var svcTime = GetTicks() - startTicks;

        Interlocked.Increment(ref _totalMessages);

        _svcTimeEwma.NewSample(svcTime);

        if (_svcTimePercentiles != null)
        {
            for (int i = 0; i < _svcTimePercentiles.Length; i++)
            {
                _svcTimePercentiles[i].NewSample(svcTime, _svcTimeEwma.AverageValue);
            }
        }

        return svcTime;
    }

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

            timePassed = (float)(ticks - _lastTicks) / Stopwatch.Frequency;

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

        return new ServiceTimeStats(timePassed * 1000,
            intervalMessages / timePassed,
            totalMesssages,
            svcAvg * MsRatio,
            svcMin * MsRatio,
            svcMax * MsRatio,
            percentileValues);
    }
}
