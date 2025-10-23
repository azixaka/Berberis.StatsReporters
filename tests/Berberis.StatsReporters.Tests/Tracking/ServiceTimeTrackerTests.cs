using Berberis.StatsReporters;
using Berberis.StatsReporters.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.Tracking;

/// <summary>
/// Tests for ServiceTimeTracker recording, statistics, and thread safety.
/// </summary>
public class ServiceTimeTrackerTests
{
    #region Task 16: Recording Tests

    [Fact]
    public void RecordServiceTime_SingleRecording_IncrementsMessageCounter()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();
        var startTicks = ServiceTimeTracker.GetTicks();
        Thread.Sleep(5);

        // Act
        tracker.RecordServiceTime(startTicks);
        var stats = tracker.GetStats(reset: false);

        // Assert
        stats.TotalProcessedMessages.Should().Be(1);
        stats.IntervalMessages.Should().Be(1);
    }

    [Fact]
    public void RecordServiceTime_WithCustomTicks_RecordsCorrectServiceTime()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();
        var startTicks = ServiceTimeTracker.GetTicks();
        Thread.Sleep(10);
        var endTicks = ServiceTimeTracker.GetTicks();

        // Act
        var serviceTime = tracker.RecordServiceTime(startTicks, endTicks);
        var stats = tracker.GetStats(reset: false);

        // Assert
        serviceTime.Should().Be(endTicks - startTicks);
        stats.AvgServiceTimeMs.Should().BeGreaterThan(0);
        stats.AvgServiceTimeMs.Should().BeLessThan(50); // Should be around 10ms
    }

    [Fact]
    public void RecordServiceTime_MultipleRecordings_AccumulateCorrectly()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();
        const int recordingCount = 10;

        // Act
        for (int i = 0; i < recordingCount; i++)
        {
            var start = ServiceTimeTracker.GetTicks();
            Thread.Sleep(1);
            tracker.RecordServiceTime(start);
        }

        var stats = tracker.GetStats(reset: false);

        // Assert
        stats.TotalProcessedMessages.Should().Be(recordingCount);
        stats.IntervalMessages.Should().Be(recordingCount);
        stats.AvgServiceTimeMs.Should().BeGreaterThan(0);
    }

    [Fact]
    public void RecordServiceTime_ReturnsCalculatedServiceTime()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();
        var startTicks = ServiceTimeTracker.GetTicks();
        var endTicks = startTicks + 1000;

        // Act
        var returned = tracker.RecordServiceTime(startTicks, endTicks);

        // Assert
        returned.Should().Be(1000);
    }

    #endregion

    #region Task 17: Statistics Tests

    [Fact]
    public void GetStats_WithNoRecordings_ReturnsZeroValues()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();

        // Act
        var stats = tracker.GetStats(reset: false);

        // Assert
        stats.TotalProcessedMessages.Should().Be(0);
        stats.IntervalMessages.Should().Be(0);
        stats.AvgServiceTimeMs.Should().Be(0);
        stats.MinServiceTimeMs.Should().Be(0);
        stats.MaxServiceTimeMs.Should().Be(0);
    }

    [Fact]
    public void GetStats_AfterRecordings_ReturnsCorrectStatistics()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();
        var start1 = ServiceTimeTracker.GetTicks();
        Thread.Sleep(5);
        var end1 = ServiceTimeTracker.GetTicks();

        var start2 = ServiceTimeTracker.GetTicks();
        Thread.Sleep(10);
        var end2 = ServiceTimeTracker.GetTicks();

        // Act
        tracker.RecordServiceTime(start1, end1);
        tracker.RecordServiceTime(start2, end2);
        var stats = tracker.GetStats(reset: false);

        // Assert
        stats.TotalProcessedMessages.Should().Be(2);
        stats.IntervalMessages.Should().Be(2);
        stats.AvgServiceTimeMs.Should().BeGreaterThan(0);
        stats.MinServiceTimeMs.Should().BeGreaterThan(0);
        stats.MaxServiceTimeMs.Should().BeGreaterThan(stats.MinServiceTimeMs);
        stats.IntervalMs.Should().BeGreaterThan(0);
        stats.ProcessRate.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetStats_WithReset_ResetsIntervalCounters()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();

        // First interval
        for (int i = 0; i < 10; i++)
        {
            var start = ServiceTimeTracker.GetTicks();
            tracker.RecordServiceTime(start);
        }
        var stats1 = tracker.GetStats(reset: true);

        // Second interval
        for (int i = 0; i < 5; i++)
        {
            var start = ServiceTimeTracker.GetTicks();
            tracker.RecordServiceTime(start);
        }
        var stats2 = tracker.GetStats(reset: true);

        // Assert
        stats1.IntervalMessages.Should().Be(10);
        stats1.TotalProcessedMessages.Should().Be(10);

        stats2.IntervalMessages.Should().Be(5, "Reset should track only new messages in interval");
        stats2.TotalProcessedMessages.Should().Be(15, "Total should keep accumulating");
    }

    [Fact]
    public void GetStats_WithoutReset_KeepsAccumulating()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();

        for (int i = 0; i < 10; i++)
        {
            var start = ServiceTimeTracker.GetTicks();
            tracker.RecordServiceTime(start);
        }
        var stats1 = tracker.GetStats(reset: false);

        for (int i = 0; i < 5; i++)
        {
            var start = ServiceTimeTracker.GetTicks();
            tracker.RecordServiceTime(start);
        }
        var stats2 = tracker.GetStats(reset: false);

        // Assert
        stats1.IntervalMessages.Should().Be(10);
        stats2.IntervalMessages.Should().Be(15, "Without reset, interval should keep accumulating");
    }

    [Fact]
    public void GetStats_WithPercentileOptions_ReturnsPercentileStatistics()
    {
        // Arrange
        var percentileOptions = new[]
        {
            new PercentileOptions(0.5f),  // P50 (median)
            new PercentileOptions(0.95f), // P95
            new PercentileOptions(0.99f)  // P99
        };
        var tracker = new ServiceTimeTracker(percentileOptions: percentileOptions);

        // Act - Record multiple samples
        for (int i = 0; i < 100; i++)
        {
            var start = ServiceTimeTracker.GetTicks();
            Thread.Sleep(1);
            tracker.RecordServiceTime(start);
        }

        var stats = tracker.GetStats(reset: false);

        // Assert
        stats.PercentileValues.Should().NotBeNull();
        stats.PercentileValues.Should().HaveCount(3);
        stats.PercentileValues[0].percentile.Should().Be(0.5f);
        stats.PercentileValues[1].percentile.Should().Be(0.95f);
        stats.PercentileValues[2].percentile.Should().Be(0.99f);

        // All percentile values should be positive
        stats.PercentileValues[0].value.Should().BeGreaterThan(0);
        stats.PercentileValues[1].value.Should().BeGreaterThan(0);
        stats.PercentileValues[2].value.Should().BeGreaterThan(0);
    }

    #endregion

    #region Task 18: Thread Safety Tests

    [Fact]
    public async Task ConcurrentRecording_From100Threads_NoLostMessages()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();
        const int threadCount = 100;
        const int operationsPerThread = 100;
        const long expectedTotal = threadCount * operationsPerThread;

        // Act
        await ConcurrencyTestHelper.ExecuteConcurrently(
            threadCount,
            operationsPerThread,
            (threadId, operationIndex) =>
            {
                var start = ServiceTimeTracker.GetTicks();
                tracker.RecordServiceTime(start);
            });

        var stats = tracker.GetStats(reset: false);

        // Assert
        stats.TotalProcessedMessages.Should().Be(expectedTotal,
            "All concurrent recordings should be counted");
    }

    [Fact]
    public async Task ConcurrentGetStats_DuringRecording_NoDataCorruption()
    {
        // Arrange
        var tracker = new ServiceTimeTracker();
        const int duration = 500; // 500ms
        var recordingCount = 0L;
        var statsCallCount = 0;
        var cts = new CancellationTokenSource(duration);

        // Act - Concurrent recording and stats retrieval
        var recordingTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var start = ServiceTimeTracker.GetTicks();
                tracker.RecordServiceTime(start);
                Interlocked.Increment(ref recordingCount);
            }
        });

        var statsTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var stats = tracker.GetStats(reset: false);
                statsCallCount++;
                Thread.Sleep(5);
            }
        });

        await Task.WhenAll(recordingTask, statsTask);

        var finalStats = tracker.GetStats(reset: false);

        // Assert
        recordingCount.Should().BeGreaterThan(0, "Should have recorded messages");
        statsCallCount.Should().BeGreaterThan(0, "Should have retrieved stats");
        finalStats.TotalProcessedMessages.Should().Be(recordingCount,
            "Final count should match recorded messages (no corruption)");
    }

    #endregion
}
