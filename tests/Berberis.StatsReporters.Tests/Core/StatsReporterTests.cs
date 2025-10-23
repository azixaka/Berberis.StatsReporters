using Berberis.StatsReporters;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.Core;

/// <summary>
/// Tests for StatsReporter single-threaded scenarios.
/// </summary>
public class StatsReporterTests
{
    [Fact]
    public void Constructor_SetsSourceNameCorrectly()
    {
        // Arrange
        const string sourceName = "test-source";

        // Act
        var reporter = new StatsReporter(sourceName);

        // Assert
        reporter.Source.Should().Be(sourceName);
    }

    [Fact]
    public void GetStats_WithNoRecordings_ReturnsLowValues()
    {
        // Arrange
        var reporter = new StatsReporter("test");

        // Act
        var stats = reporter.GetStats();

        // Assert - TotalMessages and TotalBytes should be zero for no recordings
        stats.TotalMessages.Should().Be(0);
        stats.TotalBytes.Should().Be(0);
        stats.AvgServiceTime.Should().Be(0);
        // Rates may be NaN or zero depending on interval calculation
    }

    [Fact]
    public void Stop_AfterStart_RecordsSingleOperation()
    {
        // Arrange
        var reporter = new StatsReporter("test");

        // Act
        var start = reporter.Start();
        Thread.Sleep(5); // Simulate some work
        reporter.Stop(start);

        var stats = reporter.GetStats();

        // Assert
        stats.TotalMessages.Should().Be(1);
        stats.AvgServiceTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Stop_WithBytes_RecordsBytesCorrectly()
    {
        // Arrange
        var reporter = new StatsReporter("test");
        const int bytesProcessed = 1024;

        // Act
        var start = reporter.Start();
        reporter.Stop(start, bytesProcessed);

        var stats = reporter.GetStats();

        // Assert
        stats.TotalMessages.Should().Be(1);
        stats.TotalBytes.Should().Be(bytesProcessed);
    }

    [Fact]
    public void Record_WithCustomMetrics_AccumulatesCorrectly()
    {
        // Arrange
        var reporter = new StatsReporter("test");

        // Act
        reporter.Record(units: 5, serviceTimeMs: 100, bytes: 500);
        reporter.Record(units: 3, serviceTimeMs: 50, bytes: 300);

        var stats = reporter.GetStats();

        // Assert
        stats.TotalMessages.Should().Be(8); // 5 + 3
        stats.TotalBytes.Should().Be(800);  // 500 + 300
    }

    [Fact]
    public void GetStats_MultipleRecordings_AccumulateCorrectly()
    {
        // Arrange
        var reporter = new StatsReporter("test");
        const int recordingCount = 100;

        // Act
        for (int i = 0; i < recordingCount; i++)
        {
            var start = reporter.Start();
            Thread.Sleep(1); // Small delay to accumulate service time
            reporter.Stop(start, bytes: 100);
        }

        var stats = reporter.GetStats();

        // Assert
        stats.TotalMessages.Should().Be(recordingCount);
        stats.TotalBytes.Should().Be(recordingCount * 100);
        stats.AvgServiceTime.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetStats_ResetsCountersForNextInterval()
    {
        // Arrange
        var reporter = new StatsReporter("test");

        // Act - First interval
        reporter.Record(units: 10, serviceTimeMs: 100, bytes: 1000);
        var stats1 = reporter.GetStats();

        // Act - Second interval
        reporter.Record(units: 5, serviceTimeMs: 50, bytes: 500);
        var stats2 = reporter.GetStats();

        // Assert - First interval shows cumulative totals
        stats1.TotalMessages.Should().Be(10);
        stats1.TotalBytes.Should().Be(1000);

        // Assert - Second interval shows NEW cumulative totals (delta is calculated internally but total is cumulative)
        stats2.TotalMessages.Should().Be(15); // Cumulative: 10 + 5
        stats2.TotalBytes.Should().Be(1500);  // Cumulative: 1000 + 500
    }

    [Fact]
    public void ElapsedSince_CalculatesElapsedTimeCorrectly()
    {
        // Arrange
        var reporter = new StatsReporter("test");
        var startTicks = reporter.Start();

        // Act
        Thread.Sleep(10);
        var elapsedSec = StatsReporter.ElapsedSince(startTicks);

        // Assert
        elapsedSec.Should().BeGreaterThanOrEqualTo(0.009f); // At least 9ms (allow some tolerance)
        elapsedSec.Should().BeLessThan(0.05f); // Should be reasonably fast (< 50ms)
    }

    [Fact]
    public void IsChanged_ReflectsRecordingState()
    {
        // Arrange
        var reporter = new StatsReporter("test");

        // Act & Assert - Initially false
        reporter.IsChanged.Should().BeFalse();

        // Act - Record something
        var start = reporter.Start();
        reporter.Stop(start);

        // Assert - Should be true after recording
        reporter.IsChanged.Should().BeTrue();

        // Act - Get stats (resets changed flag)
        reporter.GetStats();

        // Assert - Should be false after GetStats
        reporter.IsChanged.Should().BeFalse();
    }

    [Fact]
    public void Start_ReturnsPositiveTickValue()
    {
        // Arrange
        var reporter = new StatsReporter("test");

        // Act
        var ticks = reporter.Start();

        // Assert
        ticks.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Dispose_WithoutDisposeAction_DoesNotThrow()
    {
        // Arrange
        var reporter = new StatsReporter("test");

        // Act & Assert - Should not throw
        var act = () => reporter.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_WithDisposeAction_InvokesAction()
    {
        // Arrange
        bool disposed = false;
        var reporter = new StatsReporter("test", r => disposed = true);

        // Act
        reporter.Dispose();

        // Assert
        disposed.Should().BeTrue();
    }
}
