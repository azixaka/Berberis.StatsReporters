namespace Berberis.StatsReporters;

/// <summary>
/// Configuration for tracking a moving percentile.
/// </summary>
public readonly struct PercentileOptions
{
    /// <summary>
    /// The percentile to track (e.g., 0.95 for P95, 0.99 for P99).
    /// </summary>
    public readonly float Percentile;

    /// <summary>
    /// Sensitivity multiplier for adaptive step size. Higher values adjust faster.
    /// </summary>
    public readonly float Alpha;

    /// <summary>
    /// Initial step size for percentile adjustments.
    /// </summary>
    public readonly float Delta;

    /// <summary>
    /// Creates percentile tracking configuration.
    /// </summary>
    /// <param name="percentile">Percentile to track (0.0 to 1.0).</param>
    /// <param name="alpha">Sensitivity multiplier. Default is 0.05.</param>
    /// <param name="delta">Initial step size. Default is 0.05.</param>
    public PercentileOptions(float percentile, float alpha = 0.05f, float delta = 0.05f)
    {
        Percentile = percentile;
        Alpha = alpha;
        Delta = delta;
    }
}