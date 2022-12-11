using System;

namespace Berberis.StatsReporters;

public sealed class MovingPercentile
{
    private bool _initialised;

    private readonly float _alpha;
    private float _delta;
    private readonly float _deltaInit;

    public float Percentile { get; private set; }

    public float PercentileValue { get; private set; }

    public MovingPercentile(PercentileOptions percentileOptions)
    {
        Percentile = percentileOptions.Percentile;
        _alpha = percentileOptions.Alpha;
        _delta = _deltaInit = percentileOptions.Delta;
    }

    public void NewSample(float value)
    {
        if (_initialised)
        {
            if (value < PercentileValue)
            {
                PercentileValue -= _delta / Percentile;
            }
            else if (value > PercentileValue)
            {
                PercentileValue += _delta / (1 - Percentile);
            }
        }
        else
        {
            PercentileValue = value;
            _initialised = true;
        }
    }

    public void NewSample(float value, float ewma)
    {
        var sigma = (float)Math.Sqrt(Math.Abs(ewma - value));
        _delta = sigma * _alpha;
        NewSample(value);
    }

    public void Reset()
    {
        PercentileValue = 0;
        _delta = _deltaInit;
        _initialised = false;
    }
}
