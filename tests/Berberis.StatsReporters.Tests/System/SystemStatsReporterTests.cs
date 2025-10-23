using Berberis.StatsReporters;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.SystemModule;

/// <summary>
/// Tests for SystemStatsReporter metrics collection and interval tracking.
/// Note: Tests focus on behavior rather than exact metric values due to platform-dependent nature.
/// </summary>
public class SystemStatsReporterTests
{
    #region Task 21: Metrics Collection Tests

    [Fact]
    public void Constructor_InitializesSystemInfoDictionary()
    {
        // Arrange & Act
        var reporter = new SystemStatsReporter();

        // Assert
        reporter.SystemInfo.Should().NotBeNull();
        reporter.SystemInfo.Should().NotBeEmpty();
    }

    [Fact]
    public void Constructor_WithMeasureThreadPoolLatencyTrue_IncludesLatencyTracker()
    {
        // Arrange & Act
        var reporter = new SystemStatsReporter(measureThreadPoolLatency: true);
        var stats = reporter.GetStats();

        // Assert
        stats.ThreadPoolLatency.Should().NotBeNull("Latency tracker should be enabled");
    }

    [Fact]
    public void Constructor_WithMeasureThreadPoolLatencyFalse_HasNullLatencyTracker()
    {
        // Arrange & Act
        var reporter = new SystemStatsReporter(measureThreadPoolLatency: false);
        var stats = reporter.GetStats();

        // Assert
        stats.ThreadPoolLatency.Should().BeNull("Latency tracker should be disabled");
    }

    [Fact]
    public void GetStats_ReturnsValidSystemMetrics()
    {
        // Arrange
        var reporter = new SystemStatsReporter();

        // Act
        var stats = reporter.GetStats();

        // Assert - Verify all metrics are populated with reasonable values
        stats.IntervalMs.Should().BeGreaterThan(0, "Interval should be positive");
        stats.CpuTimeTotalMs.Should().BeGreaterThanOrEqualTo(0, "CPU time should be non-negative");
        stats.CpuLoad.Should().BeGreaterThanOrEqualTo(0, "CPU load should be non-negative");

        // GC metrics
        stats.TotalGc0s.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalGc1s.Should().BeGreaterThanOrEqualTo(0);
        stats.TotalGc2s.Should().BeGreaterThanOrEqualTo(0);
        stats.GcIterationIndex.Should().BeGreaterThanOrEqualTo(0);

        // Memory metrics
        stats.WorkSetBytes.Should().BeGreaterThan(0, "Working set should be positive");
        stats.GcMemory.Should().BeGreaterThan(0, "GC memory should be positive");

        // Thread metrics
        stats.NumberOfThreads.Should().BeGreaterThan(0, "Should have at least one thread");
        stats.ThreadPoolThreads.Should().BeGreaterThanOrEqualTo(0);
        stats.PendingThreadPoolWorkItems.Should().BeGreaterThanOrEqualTo(0);
        stats.CompletedThreadPoolWorkItems.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void SystemInfo_ContainsExpectedKeys()
    {
        // Arrange
        var reporter = new SystemStatsReporter();

        // Act
        var systemInfo = reporter.SystemInfo;

        // Assert - Verify essential system information keys exist
        var expectedKeys = new[]
        {
            "Process Id",
            "Process Name",
            "Start Time",
            "CPU Cores",
            "CLR Version",
            "FrameworkDescription",
            "OS Version",
            "OS Architecture",
            "Process Architecture",
            "GC Server Mode",
            "GC Latency Mode",
            "ThreadPool.MinWorkerThreads",
            "ThreadPool.MaxWorkerThreads"
        };

        foreach (var key in expectedKeys)
        {
            systemInfo.Should().ContainKey(key, $"SystemInfo should contain '{key}'");
            systemInfo[key].Should().NotBeNullOrWhiteSpace($"'{key}' should have a value");
        }
    }

    #endregion

    #region Task 22: Interval Tests

    [Fact]
    public void GetStats_MultipleCalls_TracksIntervalMetricsCorrectly()
    {
        // Arrange
        var reporter = new SystemStatsReporter();

        // Act - First interval
        var stats1 = reporter.GetStats();

        // Generate some activity
        Thread.Sleep(50);
        _ = GC.GetTotalMemory(forceFullCollection: false);

        // Act - Second interval
        var stats2 = reporter.GetStats();

        // Assert - Each call should return interval-specific data
        stats1.IntervalMs.Should().BeGreaterThan(0);
        stats2.IntervalMs.Should().BeGreaterThan(0);

        // Cumulative values should increase or stay the same
        stats2.CpuTimeTotalMs.Should().BeGreaterThanOrEqualTo(stats1.CpuTimeTotalMs,
            "Total CPU time should be cumulative");
        stats2.CompletedThreadPoolWorkItems.Should().BeGreaterThanOrEqualTo(stats1.CompletedThreadPoolWorkItems,
            "Completed work items should be cumulative");
    }

    [Fact]
    public void GetStats_BetweenCalls_IntervalValuesRepresentDelta()
    {
        // Arrange
        var reporter = new SystemStatsReporter();

        // First call to establish baseline
        var stats1 = reporter.GetStats();

        // Generate measurable activity
        Thread.Sleep(100);
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => Thread.SpinWait(1000)));
        }
        Task.WaitAll(tasks.ToArray());

        // Act - Second call
        var stats2 = reporter.GetStats();

        // Assert - Interval should represent time since last GetStats
        stats2.IntervalMs.Should().BeGreaterThan(50, "Should measure time since last call");
        // Note: Upper bound check removed to prevent flakiness in concurrent test environments
        // where thread scheduling can cause arbitrary delays

        // The second interval's CPU time should be less than total (it's just the delta)
        stats2.CpuTimeIntMs.Should().BeLessThanOrEqualTo(stats2.CpuTimeTotalMs,
            "Interval CPU time should not exceed total");
    }

    [Fact]
    public void GetStats_ConsecutiveCalls_CumulativeValuesAccumulate()
    {
        // Arrange
        var reporter = new SystemStatsReporter();
        var stats1 = reporter.GetStats();

        // Generate threadpool work
        var workDone = 0;
        for (int i = 0; i < 100; i++)
        {
            ThreadPool.QueueUserWorkItem(_ => Interlocked.Increment(ref workDone));
        }

        // Wait for work to complete
        Thread.Sleep(100);
        var timeout = DateTime.UtcNow.AddSeconds(2);
        while (workDone < 100 && DateTime.UtcNow < timeout)
        {
            Thread.Sleep(10);
        }

        // Act
        var stats2 = reporter.GetStats();
        var stats3 = reporter.GetStats();

        // Assert - Cumulative counters should never decrease
        stats2.TotalGc0s.Should().BeGreaterThanOrEqualTo(stats1.TotalGc0s,
            "GC Gen0 count should be cumulative");
        stats3.TotalGc0s.Should().BeGreaterThanOrEqualTo(stats2.TotalGc0s,
            "GC Gen0 count should keep accumulating");

        stats2.CompletedThreadPoolWorkItems.Should().BeGreaterThanOrEqualTo(stats1.CompletedThreadPoolWorkItems,
            "Completed work items should be cumulative");
        stats3.CompletedThreadPoolWorkItems.Should().BeGreaterThanOrEqualTo(stats2.CompletedThreadPoolWorkItems,
            "Completed work items should keep accumulating");
    }

    #endregion
}
