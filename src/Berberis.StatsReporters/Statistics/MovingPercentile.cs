using System;
using System.Runtime.CompilerServices;

namespace Berberis.StatsReporters;

/// <summary>
/// Tracks a percentile estimate over a stream of values using adaptive step size.
/// </summary>
public sealed class MovingPercentile
{
    private bool _initialised;

    private readonly float _alpha;
    private float _delta;
    private readonly float _deltaInit;

    /// <summary>
    /// Gets the percentile being tracked (e.g., 0.95 for P95).
    /// </summary>
    public float Percentile { get; private set; }

    /// <summary>
    /// Gets the current percentile estimate.
    /// </summary>
    public float PercentileValue { get; private set; }

    /// <summary>
    /// Creates a moving percentile tracker.
    /// </summary>
    public MovingPercentile(PercentileOptions percentileOptions)
    {
        Percentile = percentileOptions.Percentile;
        _alpha = percentileOptions.Alpha;
        _delta = _deltaInit = percentileOptions.Delta;
    }

    /// <summary>
    /// Records a new sample with fixed step size.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NewSample(float value)
    {
        if (_initialised)
        {
            if (value < PercentileValue)
            {
                PercentileValue -= _delta;
            }
            else if (value > PercentileValue)
            {
                PercentileValue += _delta;
            }
        }
        else
        {
            PercentileValue = value;
            _initialised = true;
        }
    }

    /// <summary>
    /// Records a new sample with adaptive step size based on EWMA.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void NewSample(float value, float ewma)
    {
        var sigma = (float)Math.Sqrt(Math.Abs(ewma - value));
        _delta = sigma * _alpha;
        NewSample(value);
    }

    /// <summary>
    /// Resets the percentile estimate.
    /// </summary>
    public void Reset()
    {
        PercentileValue = 0;
        _delta = _deltaInit;
        _initialised = false;
    }
}
