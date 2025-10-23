using Berberis.StatsReporters;
using Berberis.StatsReporters.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Berberis.StatsReporters.Tests.Core;

/// <summary>
/// Tests for StatsReporterFactory basic operations.
/// </summary>
public class StatsReporterFactoryTests
{
    private static StatsReporterFactory CreateFactory()
    {
        var loggerFactory = TestLoggerFactory.CreateNullLoggerFactory();
        return new StatsReporterFactory(loggerFactory.CreateLogger<StatsReporterFactory>());
    }

    [Fact]
    public void GetOrCreateReporter_WithSameSource_ReturnsSameInstance()
    {
        // Arrange
        var factory = CreateFactory();
        const string source = "test-source";

        // Act
        var reporter1 = factory.GetOrCreateReporter(source);
        var reporter2 = factory.GetOrCreateReporter(source);

        // Assert
        reporter1.Should().BeSameAs(reporter2, "Factory should return the same instance for the same source");
        reporter1.Source.Should().Be(source);
    }

    [Fact]
    public void GetOrCreateReporter_WithDifferentSources_ReturnsDifferentInstances()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var reporter1 = factory.GetOrCreateReporter("source1");
        var reporter2 = factory.GetOrCreateReporter("source2");

        // Assert
        reporter1.Should().NotBeSameAs(reporter2);
        reporter1.Source.Should().Be("source1");
        reporter2.Source.Should().Be("source2");
    }

    [Fact]
    public void ListReporters_WithMultipleReporters_ReturnsAllSources()
    {
        // Arrange
        var factory = CreateFactory();
        var expectedSources = new[] { "source1", "source2", "source3" };

        // Act
        foreach (var source in expectedSources)
        {
            factory.GetOrCreateReporter(source);
        }
        var actualSources = factory.ListReporters().ToList();

        // Assert
        actualSources.Should().HaveCount(3);
        actualSources.Should().Contain(expectedSources);
    }

    [Fact]
    public void GetReporterStats_ForExistingReporter_ReturnsCorrectStats()
    {
        // Arrange
        var factory = CreateFactory();
        const string source = "test-reporter";
        var reporter = factory.GetOrCreateReporter(source);

        // Record some data
        reporter.Record(units: 10, serviceTimeMs: 100, bytes: 1000);

        // Act
        var stats = factory.GetReporterStats(source);

        // Assert
        stats.TotalMessages.Should().Be(10);
        stats.TotalBytes.Should().Be(1000);
    }

    [Fact]
    public void GetReporterStats_ForNonExistentReporter_ThrowsKeyNotFoundException()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var action = () => factory.GetReporterStats("non-existent");

        // Assert
        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("*non-existent*");
    }

    [Fact]
    public void Dispose_RemovesReporterFromFactory()
    {
        // Arrange
        var factory = CreateFactory();
        const string source = "disposable-reporter";
        var reporter = factory.GetOrCreateReporter(source);

        // Act
        reporter.Dispose();
        var reporters = factory.ListReporters().ToList();

        // Assert
        reporters.Should().NotContain(source, "Disposed reporter should be removed from factory");
    }

    [Fact]
    public void GetSystemInfo_ReturnsSystemInformation()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var systemInfo = factory.GetSystemInfo();

        // Assert
        systemInfo.Should().NotBeNull();
        systemInfo.Should().NotBeEmpty("System info should contain at least some information");
    }

    [Fact]
    public void GetNetworkInfo_ReturnsNetworkInformation()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var networkInfo = factory.GetNetworkInfo();

        // Assert
        networkInfo.Should().NotBeNull();
        // Network info may be empty if no network adapters are available
    }

    [Fact]
    public void GetSystemStats_ReturnsSystemStatistics()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var systemStats = factory.GetSystemStats();

        // Assert
        systemStats.IntervalMs.Should().BeGreaterThanOrEqualTo(0);
        systemStats.CpuLoad.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetNetworkStats_ReturnsNetworkStatistics()
    {
        // Arrange
        var factory = CreateFactory();

        // Act
        var networkStats = factory.GetNetworkStats();

        // Assert
        networkStats.Should().NotBeNull();
        networkStats.BytesReceived.Should().BeGreaterThanOrEqualTo(0);
        networkStats.BytesSent.Should().BeGreaterThanOrEqualTo(0);
    }
}
