using System.Collections.Concurrent;

namespace Berberis.StatsReporters.Tests.TestHelpers;

/// <summary>
/// Helper utilities for concurrent testing scenarios.
/// </summary>
public static class ConcurrencyTestHelper
{
    /// <summary>
    /// Executes an action concurrently from multiple threads with barrier synchronization.
    /// </summary>
    /// <param name="threadCount">Number of concurrent threads to spawn</param>
    /// <param name="operationsPerThread">Number of operations each thread performs</param>
    /// <param name="action">The action to execute (receives thread ID and operation index)</param>
    /// <returns>Task that completes when all threads finish</returns>
    public static async Task ExecuteConcurrently(
        int threadCount,
        int operationsPerThread,
        Action<int, int> action)
    {
        var barrier = new Barrier(threadCount);
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadId => Task.Run(() =>
            {
                try
                {
                    // Wait for all threads to be ready
                    barrier.SignalAndWait();

                    // Execute operations
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        action(threadId, i);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        if (!exceptions.IsEmpty)
        {
            throw new AggregateException(
                "One or more threads threw exceptions during concurrent execution",
                exceptions);
        }
    }

    /// <summary>
    /// Executes an async action concurrently from multiple threads.
    /// </summary>
    public static async Task ExecuteConcurrentlyAsync(
        int threadCount,
        int operationsPerThread,
        Func<int, int, Task> action)
    {
        var barrier = new Barrier(threadCount);
        var exceptions = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, threadCount)
            .Select(threadId => Task.Run(async () =>
            {
                try
                {
                    barrier.SignalAndWait();

                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        await action(threadId, i);
                    }
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        if (!exceptions.IsEmpty)
        {
            throw new AggregateException(
                "One or more threads threw exceptions during concurrent execution",
                exceptions);
        }
    }

    /// <summary>
    /// Runs a test action multiple times to detect race conditions.
    /// </summary>
    public static async Task RunMultipleTimes(int iterations, Func<Task> testAction)
    {
        for (int i = 0; i < iterations; i++)
        {
            await testAction();
        }
    }
}
