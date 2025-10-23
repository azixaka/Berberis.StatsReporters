using Berberis.StatsReporters;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.Tracking;

/// <summary>
/// Tests for ThreadPoolLatencyTracker measurement functionality.
/// </summary>
public class ThreadPoolLatencyTrackerTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Constructor_WithInvalidNumberOfMeasurements_ThrowsArgumentOutOfRangeException(int invalidCount)
    {
        // Arrange & Act
        var act = () => new ThreadPoolLatencyTracker(invalidCount);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("numberOfMeasurements");
    }

    [Fact]
    public void Constructor_WithValidNumberOfMeasurements_DoesNotThrow()
    {
        // Arrange & Act
        var act = () => new ThreadPoolLatencyTracker(100);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Measure_ReturnsValidLatencyStatistics()
    {
        // Arrange
        var tracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 1000);

        // Act
        var stats = tracker.Measure();

        // Assert
        stats.MedianMs.Should().BeGreaterThanOrEqualTo(0, "Median latency should be non-negative");
        stats.P90Ms.Should().BeGreaterThanOrEqualTo(0, "P90 latency should be non-negative");
        stats.P99Ms.Should().BeGreaterThanOrEqualTo(0, "P99 latency should be non-negative");
        stats.P99_99Ms.Should().BeGreaterThanOrEqualTo(0, "P99.99 latency should be non-negative");
    }

    [Fact]
    public void Measure_MultipleTimes_ReturnsConsistentResults()
    {
        // Arrange
        var tracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 1000);

        // Act - Run multiple measurements
        var stats1 = tracker.Measure();
        Thread.Sleep(10); // Small delay between measurements
        var stats2 = tracker.Measure();
        Thread.Sleep(10);
        var stats3 = tracker.Measure();

        // Assert - All measurements should return valid, non-zero results
        stats1.MedianMs.Should().BeGreaterThanOrEqualTo(0);
        stats2.MedianMs.Should().BeGreaterThanOrEqualTo(0);
        stats3.MedianMs.Should().BeGreaterThanOrEqualTo(0);

        // Latencies should be in reasonable range (less than 100ms for thread pool scheduling)
        stats1.P99Ms.Should().BeLessThan(100, "P99 should be less than 100ms in normal conditions");
        stats2.P99Ms.Should().BeLessThan(100, "P99 should be less than 100ms in normal conditions");
        stats3.P99Ms.Should().BeLessThan(100, "P99 should be less than 100ms in normal conditions");
    }

    [Fact]
    public void Measure_PercentilesAreOrderedCorrectly()
    {
        // Arrange
        var tracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 5000);

        // Act
        var stats = tracker.Measure();

        // Assert - Percentiles should be ordered: Median <= P90 <= P99 <= P99.99
        stats.P90Ms.Should().BeGreaterThanOrEqualTo(stats.MedianMs,
            "P90 should be >= median");
        stats.P99Ms.Should().BeGreaterThanOrEqualTo(stats.P90Ms,
            "P99 should be >= P90");
        stats.P99_99Ms.Should().BeGreaterThanOrEqualTo(stats.P99Ms,
            "P99.99 should be >= P99");
    }

    [Fact]
    public void Measure_WithSmallNumberOfMeasurements_CompletesSuccessfully()
    {
        // Arrange
        var tracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 10);

        // Act
        var stats = tracker.Measure();

        // Assert
        stats.MedianMs.Should().BeGreaterThanOrEqualTo(0);
        stats.P90Ms.Should().BeGreaterThanOrEqualTo(0);
        stats.P99Ms.Should().BeGreaterThanOrEqualTo(0);
        stats.P99_99Ms.Should().BeGreaterThanOrEqualTo(0);
    }
}
