namespace Berberis.StatsReporters.Tests.TestHelpers;

/// <summary>
/// Helper utilities for statistical testing and validation.
/// </summary>
public static class StatisticalTestHelper
{
    /// <summary>
    /// Generates a sequence of uniformly distributed values.
    /// </summary>
    public static IEnumerable<double> GenerateUniformDistribution(
        double min,
        double max,
        int count)
    {
        var step = (max - min) / (count - 1);
        for (int i = 0; i < count; i++)
        {
            yield return min + (i * step);
        }
    }

    /// <summary>
    /// Generates a sequence with normal (Gaussian) distribution.
    /// Uses Box-Muller transform.
    /// </summary>
    public static IEnumerable<double> GenerateNormalDistribution(
        double mean,
        double stdDev,
        int count,
        int? seed = null)
    {
        var random = seed.HasValue ? new Random(seed.Value) : new Random();

        for (int i = 0; i < count; i += 2)
        {
            // Box-Muller transform
            var u1 = random.NextDouble();
            var u2 = random.NextDouble();

            var z0 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2);
            var z1 = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            yield return mean + (z0 * stdDev);

            if (i + 1 < count)
                yield return mean + (z1 * stdDev);
        }
    }

    /// <summary>
    /// Calculates the exact percentile from a sorted list of values.
    /// Uses linear interpolation between ranks.
    /// </summary>
    public static double CalculateExactPercentile(List<double> sortedValues, double percentile)
    {
        if (sortedValues.Count == 0)
            throw new ArgumentException("Cannot calculate percentile of empty list");

        if (sortedValues.Count == 1)
            return sortedValues[0];

        // Percentile rank formula: P = (n - 1) * percentile
        var rank = (sortedValues.Count - 1) * percentile;
        var lowerIndex = (int)Math.Floor(rank);
        var upperIndex = (int)Math.Ceiling(rank);

        if (lowerIndex == upperIndex)
            return sortedValues[lowerIndex];

        // Linear interpolation
        var lowerValue = sortedValues[lowerIndex];
        var upperValue = sortedValues[upperIndex];
        var fraction = rank - lowerIndex;

        return lowerValue + ((upperValue - lowerValue) * fraction);
    }

    /// <summary>
    /// Calculates the mean (average) of a sequence.
    /// </summary>
    public static double CalculateMean(IEnumerable<double> values)
    {
        var count = 0;
        var sum = 0.0;

        foreach (var value in values)
        {
            sum += value;
            count++;
        }

        return count > 0 ? sum / count : 0;
    }

    /// <summary>
    /// Calculates the standard deviation of a sequence.
    /// </summary>
    public static double CalculateStandardDeviation(IEnumerable<double> values)
    {
        var valuesList = values.ToList();
        if (valuesList.Count == 0)
            return 0;

        var mean = CalculateMean(valuesList);
        var sumOfSquaredDifferences = valuesList.Sum(v => Math.Pow(v - mean, 2));

        return Math.Sqrt(sumOfSquaredDifferences / valuesList.Count);
    }

    /// <summary>
    /// Validates that a moving average has converged within acceptable error bounds.
    /// </summary>
    public static bool HasConverged(
        double actual,
        double expected,
        double tolerancePercentage = 5.0)
    {
        var tolerance = Math.Abs(expected * (tolerancePercentage / 100.0));
        return Math.Abs(actual - expected) <= tolerance;
    }
}
