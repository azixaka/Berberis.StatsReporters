using Berberis.StatsReporters;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.NetworkModule;

/// <summary>
/// Tests for NetworkStatsReporter.
/// Note: Limited platform-independent tests due to dependency on actual network adapters.
/// </summary>
public class NetworkStatsReporterTests
{
    [Fact]
    public void Constructor_InitializesNetworkInfo()
    {
        // Arrange & Act
        var reporter = new NetworkStatsReporter();

        // Assert
        reporter.NetworkInfo.Should().NotBeNull();
        // Network adapters count is platform-dependent, so just verify structure
    }

    [Fact]
    public void GetStats_ReturnsValidNetworkStats()
    {
        // Arrange
        var reporter = new NetworkStatsReporter();

        // Act
        var stats = reporter.GetStats();

        // Assert
        stats.IntervalMs.Should().BeGreaterThan(0, "Interval should be positive");
        // Platform-dependent metrics - just verify structure is valid
    }

    [Fact]
    public void GetStats_MultipleCalls_TracksIntervalCorrectly()
    {
        // Arrange
        var reporter = new NetworkStatsReporter();

        // Act
        var stats1 = reporter.GetStats();
        Thread.Sleep(50);
        var stats2 = reporter.GetStats();

        // Assert
        stats1.IntervalMs.Should().BeGreaterThan(0);
        stats2.IntervalMs.Should().BeGreaterThan(0);
        stats2.IntervalMs.Should().BeGreaterThan(stats1.IntervalMs / 2,
            "Second interval should account for sleep time");
    }
}
