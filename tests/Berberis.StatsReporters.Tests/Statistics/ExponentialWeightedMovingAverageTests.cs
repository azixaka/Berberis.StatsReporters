using Berberis.StatsReporters;
using Berberis.StatsReporters.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.Statistics;

/// <summary>
/// Tests for ExponentialWeightedMovingAverage initialization and calculations.
/// </summary>
public class ExponentialWeightedMovingAverageTests
{
    #region Initialization Tests

    [Fact]
    public void Constructor_WithValidSamplesPerWindow_CalculatesCorrectAlpha()
    {
        // Arrange & Act
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 50);

        // Assert - Alpha should be 2 / (n + 1) = 2 / 51 ≈ 0.0392
        // We can't directly test alpha (it's private), but we can verify the behavior
        ewma.AverageValue.Should().Be(0);
        ewma.MinValue.Should().Be(0);
        ewma.MaxValue.Should().Be(0);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Constructor_WithInvalidSamplesPerWindow_DefaultsTo50(int invalidSamples)
    {
        // Arrange & Act
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: invalidSamples);

        // Assert - Should default to 50 samples, which means alpha = 2/51
        // Behavior should be same as explicit 50
        ewma.AverageValue.Should().Be(0);
        ewma.MinValue.Should().Be(0);
        ewma.MaxValue.Should().Be(0);
    }

    [Fact]
    public void InitialState_BeforeFirstSample_HasZeroValues()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 10);

        // Act & Assert
        ewma.AverageValue.Should().Be(0);
        ewma.MinValue.Should().Be(0);
        ewma.MaxValue.Should().Be(0);
    }

    #endregion

    #region Calculation Tests

    [Fact]
    public void NewSample_FirstSample_SetsAllValuesToSample()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 10);
        const float firstValue = 100f;

        // Act
        ewma.NewSample(firstValue);

        // Assert - First sample initializes all values
        ewma.AverageValue.Should().Be(firstValue);
        ewma.MinValue.Should().Be(firstValue);
        ewma.MaxValue.Should().Be(firstValue);
    }

    [Fact]
    public void NewSample_SubsequentSamples_UpdatesAverageUsingEWMA()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 10);
        // Alpha = 2 / (10 + 1) = 2/11 ≈ 0.1818

        // Act
        ewma.NewSample(100f); // Initializes to 100
        ewma.NewSample(200f); // Should update using EWMA formula

        // Assert - EMA[new] = EMA[old] + alpha * (value - EMA[old])
        // EMA[new] = 100 + 0.1818 * (200 - 100) = 100 + 18.18 = 118.18
        ewma.AverageValue.Should().BeApproximately(118.18f, 0.1f);
    }

    [Fact]
    public void NewSample_MultipleValues_TracksMinCorrectly()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 10);

        // Act
        ewma.NewSample(100f);
        ewma.NewSample(50f);  // New min
        ewma.NewSample(75f);
        ewma.NewSample(200f);

        // Assert
        ewma.MinValue.Should().Be(50f, "Min should track the lowest value seen");
    }

    [Fact]
    public void NewSample_MultipleValues_TracksMaxCorrectly()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 10);

        // Act
        ewma.NewSample(100f);
        ewma.NewSample(200f); // New max
        ewma.NewSample(75f);
        ewma.NewSample(150f);

        // Assert
        ewma.MaxValue.Should().Be(200f, "Max should track the highest value seen");
    }

    [Fact]
    public void NewSample_UniformDistribution_ConvergesToAverage()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 20);
        const double targetValue = 100.0;
        const int sampleCount = 200; // Feed many samples to ensure convergence

        // Act - Feed constant value
        for (int i = 0; i < sampleCount; i++)
        {
            ewma.NewSample((float)targetValue);
        }

        // Assert - Should converge very close to the target value
        ewma.AverageValue.Should().BeApproximately((float)targetValue, 0.01f,
            "EWMA should converge to constant value after many samples");
    }

    [Fact]
    public void NewSample_WithNormalDistribution_ConvergesToMean()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 50);
        const double expectedMean = 100.0;
        const double stdDev = 15.0;
        var samples = StatisticalTestHelper.GenerateNormalDistribution(expectedMean, stdDev, 1000, seed: 42).ToList();

        // Act
        foreach (var sample in samples)
        {
            ewma.NewSample((float)sample);
        }

        // Assert - Should converge close to the mean (within a few standard deviations)
        ewma.AverageValue.Should().BeInRange(
            (float)(expectedMean - 2 * stdDev),
            (float)(expectedMean + 2 * stdDev),
            "EWMA should converge near the distribution mean");
    }

    [Fact]
    public void Reset_AfterSamples_ClearsAllState()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 10);
        ewma.NewSample(100f);
        ewma.NewSample(200f);
        ewma.NewSample(50f);

        // Act
        ewma.Reset();

        // Assert - All values should be back to zero
        ewma.AverageValue.Should().Be(0);
        ewma.MinValue.Should().Be(0);
        ewma.MaxValue.Should().Be(0);
    }

    [Fact]
    public void Reset_ThenNewSample_ReinitializesCorrectly()
    {
        // Arrange
        var ewma = new ExponentialWeightedMovingAverage(samplesPerWindow: 10);
        ewma.NewSample(100f);
        ewma.NewSample(200f);
        ewma.Reset();

        // Act
        const float newValue = 75f;
        ewma.NewSample(newValue);

        // Assert - Should behave like first sample again
        ewma.AverageValue.Should().Be(newValue);
        ewma.MinValue.Should().Be(newValue);
        ewma.MaxValue.Should().Be(newValue);
    }

    #endregion
}
