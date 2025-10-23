using Berberis.StatsReporters;
using Berberis.StatsReporters.Tests.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Berberis.StatsReporters.Tests.Core;

/// <summary>
/// Tests for StatsReporterFactory thread-safety and concurrent operations.
/// </summary>
public class StatsReporterFactoryConcurrencyTests
{
    private static StatsReporterFactory CreateFactory()
    {
        var loggerFactory = TestLoggerFactory.CreateNullLoggerFactory();
        return new StatsReporterFactory(loggerFactory.CreateLogger<StatsReporterFactory>());
    }

    [Fact]
    public async Task ConcurrentGetOrCreateReporter_SameSource_ReturnsSameInstance()
    {
        // Arrange
        var factory = CreateFactory();
        const string source = "concurrent-source";
        const int threadCount = 100;
        var reporters = new System.Collections.Concurrent.ConcurrentBag<StatsReporter>();

        // Act - Multiple threads try to get/create the same reporter simultaneously
        await ConcurrencyTestHelper.ExecuteConcurrently(
            threadCount,
            1, // One operation per thread
            (threadId, operationIndex) =>
            {
                var reporter = factory.GetOrCreateReporter(source);
                reporters.Add(reporter);
            });

        // Assert - All threads should have received the same instance
        var distinctReporters = reporters.Distinct().ToList();
        distinctReporters.Should().HaveCount(1, "All threads should receive the same reporter instance");
        distinctReporters.Single().Source.Should().Be(source);
    }

    [Fact]
    public async Task ConcurrentGetOrCreateReporter_DifferentSources_CreatesDistinctInstances()
    {
        // Arrange
        var factory = CreateFactory();
        const int threadCount = 50;
        const int operationsPerThread = 10;
        var reporters = new System.Collections.Concurrent.ConcurrentDictionary<string, StatsReporter>();

        // Act - Each thread creates reporters with unique source names
        await ConcurrencyTestHelper.ExecuteConcurrently(
            threadCount,
            operationsPerThread,
            (threadId, operationIndex) =>
            {
                var source = $"source-{threadId}-{operationIndex}";
                var reporter = factory.GetOrCreateReporter(source);
                reporters.TryAdd(source, reporter);
            });

        // Assert - Should have created unique reporters for each unique source
        var expectedCount = threadCount * operationsPerThread;
        reporters.Should().HaveCount(expectedCount);

        var sources = factory.ListReporters().ToList();
        sources.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task ConcurrentGetReporterStats_DuringRecording_NoExceptions()
    {
        // Arrange
        var factory = CreateFactory();
        const string source = "stats-source";
        var reporter = factory.GetOrCreateReporter(source);
        var statsRetrieved = 0;
        var recordingsMade = 0;
        const int duration = 1000; // 1 second
        var cts = new CancellationTokenSource(duration);

        // Act - Concurrent stats retrieval while recording
        var recordingTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                reporter.Record(units: 1, serviceTimeMs: 10, bytes: 100);
                Interlocked.Increment(ref recordingsMade);
            }
        });

        var statsTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var stats = factory.GetReporterStats(source);
                Interlocked.Increment(ref statsRetrieved);
                Thread.Sleep(5);
            }
        });

        await Task.WhenAll(recordingTask, statsTask);

        // Assert
        recordingsMade.Should().BeGreaterThan(0, "Should have made recordings");
        statsRetrieved.Should().BeGreaterThan(0, "Should have retrieved stats");
        // No exceptions thrown means thread-safety is maintained
    }

    [Fact]
    public async Task ConcurrentMixedOperations_CreateListAndRetrieveStats_NoDataCorruption()
    {
        // Arrange
        var factory = CreateFactory();
        const int threadCount = 20;
        const int operationsPerThread = 50;
        var createdSources = new System.Collections.Concurrent.ConcurrentBag<string>();

        // Act - Mix of create, list, and stats operations
        await ConcurrencyTestHelper.ExecuteConcurrently(
            threadCount,
            operationsPerThread,
            (threadId, operationIndex) =>
            {
                var operation = operationIndex % 3;

                switch (operation)
                {
                    case 0: // Create reporter
                        var source = $"mixed-{threadId}-{operationIndex}";
                        factory.GetOrCreateReporter(source);
                        createdSources.Add(source);
                        break;

                    case 1: // List reporters
                        var reporters = factory.ListReporters().ToList();
                        reporters.Should().NotBeNull();
                        break;

                    case 2: // Try to get stats (may throw if reporter doesn't exist yet)
                        var allSources = createdSources.ToList();
                        if (allSources.Any())
                        {
                            var randomSource = allSources[new Random().Next(allSources.Count)];
                            try
                            {
                                factory.GetReporterStats(randomSource);
                            }
                            catch (KeyNotFoundException)
                            {
                                // Expected if reporter was removed
                            }
                        }
                        break;
                }
            });

        // Assert - Should have created many reporters
        var finalList = factory.ListReporters().ToList();
        finalList.Should().NotBeEmpty();
        // No exceptions (other than expected KeyNotFoundException) means no data corruption
    }
}
