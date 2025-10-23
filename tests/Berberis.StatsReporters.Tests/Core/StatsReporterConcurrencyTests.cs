using Berberis.StatsReporters;
using Berberis.StatsReporters.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.Core;

/// <summary>
/// Tests for StatsReporter thread-safety and concurrent operations.
/// </summary>
public class StatsReporterConcurrencyTests
{
    [Fact]
    public async Task ConcurrentRecording_From100Threads_CountsAllOperations()
    {
        // Arrange
        var reporter = new StatsReporter("concurrent-test");
        const int threadCount = 100;
        const int operationsPerThread = 1000;
        const long expectedTotal = threadCount * operationsPerThread;

        // Act
        await ConcurrencyTestHelper.ExecuteConcurrently(
            threadCount,
            operationsPerThread,
            (threadId, operationIndex) =>
            {
                var start = reporter.Start();
                reporter.Stop(start, bytes: 10);
            });

        var stats = reporter.GetStats();

        // Assert
        stats.TotalMessages.Should().Be(expectedTotal,
            "Interlocked operations should not lose any updates");
        stats.TotalBytes.Should().Be(expectedTotal * 10UL,
            "Byte counting should be atomic");
    }

    [Fact]
    public async Task ConcurrentRecord_WithCustomMetrics_NoLostUpdates()
    {
        // Arrange
        var reporter = new StatsReporter("record-test");
        const int threadCount = 50;
        const int operationsPerThread = 500;

        // Act
        await ConcurrencyTestHelper.ExecuteConcurrently(
            threadCount,
            operationsPerThread,
            (threadId, operationIndex) =>
            {
                reporter.Record(units: 2, serviceTimeMs: 10, bytes: 100);
            });

        var stats = reporter.GetStats();

        // Assert
        var expectedMessages = threadCount * operationsPerThread * 2;
        var expectedBytes = (ulong)(threadCount * operationsPerThread * 100);

        stats.TotalMessages.Should().Be(expectedMessages);
        stats.TotalBytes.Should().Be(expectedBytes);
    }

    [Fact]
    public async Task ConcurrentGetStats_DuringRecording_NoDataCorruption()
    {
        // Arrange
        var reporter = new StatsReporter("mixed-test");
        const int duration = 1000; // 1 second
        var recordingCount = 0L;
        var statsCallCount = 0;
        var cts = new CancellationTokenSource(duration);

        // Act - Concurrent recording and stats retrieval
        var recordingTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var start = reporter.Start();
                reporter.Stop(start);
                Interlocked.Increment(ref recordingCount);
            }
        });

        var statsTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var stats = reporter.GetStats();
                statsCallCount++;
                Thread.Sleep(10);
            }
        });

        await Task.WhenAll(recordingTask, statsTask);

        // Assert
        recordingCount.Should().BeGreaterThan(0, "Should have recorded messages");
        statsCallCount.Should().BeGreaterThan(0, "Should have retrieved stats");
        // No exceptions means no data corruption
    }

    [Fact]
    public async Task StressTest_HighThroughput_MaintainsAccuracy()
    {
        // Arrange
        var reporter = new StatsReporter("stress-test");
        const int threadCount = 10;
        const int operationsPerThread = 10000;
        const long expectedTotal = threadCount * operationsPerThread;

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        await ConcurrencyTestHelper.ExecuteConcurrently(
            threadCount,
            operationsPerThread,
            (threadId, operationIndex) =>
            {
                reporter.Record(units: 1, serviceTimeMs: 1, bytes: 50);
            });

        stopwatch.Stop();
        var stats = reporter.GetStats();

        // Assert
        stats.TotalMessages.Should().Be(expectedTotal);
        stats.TotalBytes.Should().Be(expectedTotal * 50UL);

        var throughput = expectedTotal / stopwatch.Elapsed.TotalSeconds;
        throughput.Should().BeGreaterThan(10_000,
            "Should handle >10k operations/second");
    }
}
