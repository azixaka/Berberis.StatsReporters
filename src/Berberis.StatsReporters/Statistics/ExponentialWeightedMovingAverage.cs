using System;

namespace Berberis.StatsReporters;

/// <summary>
/// Computes exponentially weighted moving average with min/max tracking.
/// </summary>
public sealed class ExponentialWeightedMovingAverage
{
    private bool _initialised = false;

    // Smoothing/damping coefficient
    private readonly float _alpha;

    /// <summary>
    /// Gets the current EWMA value.
    /// </summary>
    public float AverageValue { get; private set; }

    /// <summary>
    /// Gets the minimum value observed since last reset.
    /// </summary>
    public float MinValue { get; private set; }

    /// <summary>
    /// Gets the maximum value observed since last reset.
    /// </summary>
    public float MaxValue { get; private set; }

    /// <summary>
    /// Creates an EWMA calculator with the specified window size.
    /// </summary>
    /// <param name="samplesPerWindow">Number of samples per window. Default is 50 if less than 1.</param>
    public ExponentialWeightedMovingAverage(int samplesPerWindow)
    {
        samplesPerWindow = samplesPerWindow < 1 ? 50 : samplesPerWindow;

        // `2 / (n + 1)` is a standard ways of choosing an alpha value
        _alpha = 2f / (samplesPerWindow + 1);
    }

    /// <summary>
    /// Records a new sample and updates the moving average.
    /// </summary>
    public void NewSample(float value)
    {
        if (_initialised)
        {
            // Recursive weighting function: EMA[current] = EMA[previous] + alpha * (current_value - EMA[previous])
            AverageValue += _alpha * (value - AverageValue);

            MinValue = Math.Min(MinValue, value);
            MaxValue = Math.Max(MaxValue, value);
        }
        else
        {
            AverageValue = value;
            MinValue = value;
            MaxValue = value;
            _initialised = true;
        }
    }

    /// <summary>
    /// Resets all values to zero.
    /// </summary>
    public void Reset()
    {
        AverageValue = 0;
        MinValue = 0;
        MaxValue = 0;
        _initialised = false;
    }
}