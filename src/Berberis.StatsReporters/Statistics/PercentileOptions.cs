namespace Berberis.StatsReporters;

public readonly struct PercentileOptions
{
    public readonly float Percentile;
    public readonly float Alpha;
    public readonly float Delta;

    public PercentileOptions(float percentile, float alpha = 0.05f, float delta = 0.05f)
    {
        Percentile = percentile;
        Alpha = alpha;
        Delta = delta;
    }
}