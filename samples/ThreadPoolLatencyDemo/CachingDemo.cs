using Berberis.StatsReporters;
using System.Diagnostics;

public static class CachingDemo
{
    public static void Run()
    {
        Console.WriteLine("\n=== Caching & Deduplication Demo ===\n");

        // Test 1: Cache hit within time window
        Console.WriteLine("Test 1: Cache hit within 2-second window");
        Console.WriteLine("------------------------------------------");
        var tracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 10_000, cacheDuration: TimeSpan.FromSeconds(2));

        var sw = Stopwatch.StartNew();
        var stats1 = tracker.Measure();
        var time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Call 1: Median={stats1.MedianMs:F4}ms (took {time1}ms) - MEASURED");

        sw.Restart();
        var stats2 = tracker.Measure();
        var time2 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Call 2: Median={stats2.MedianMs:F4}ms (took {time2}ms) - {(time2 < 1 ? "CACHED" : "MEASURED")}");

        Console.WriteLine($"Same result: {stats1 == stats2}\n");

        // Test 2: Cache miss after expiry
        Console.WriteLine("Test 2: Cache miss after expiry");
        Console.WriteLine("------------------------------------------");
        Console.WriteLine("Waiting 2.1 seconds for cache to expire...");
        Thread.Sleep(2100);

        sw.Restart();
        var stats3 = tracker.Measure();
        var time3 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Call 3: Median={stats3.MedianMs:F4}ms (took {time3}ms) - {(time3 > 1 ? "MEASURED" : "CACHED")}");
        Console.WriteLine($"Different from Call 1: {stats1 != stats3}\n");

        // Test 3: Concurrent calls - deduplication
        Console.WriteLine("Test 3: Concurrent calls during measurement");
        Console.WriteLine("------------------------------------------");
        tracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 20_000, cacheDuration: TimeSpan.FromSeconds(1));

        int concurrentCalls = 10;
        var barrier = new CountdownEvent(concurrentCalls);
        var results = new LatencyStats[concurrentCalls];
        var timings = new long[concurrentCalls];

        Console.WriteLine($"Launching {concurrentCalls} concurrent Measure() calls...");
        var overallSw = Stopwatch.StartNew();

        for (int i = 0; i < concurrentCalls; i++)
        {
            int index = i;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                var callSw = Stopwatch.StartNew();
                results[index] = tracker.Measure();
                timings[index] = callSw.ElapsedMilliseconds;
                barrier.Signal();
            });
        }

        barrier.Wait();
        overallSw.Stop();

        Console.WriteLine($"\nAll {concurrentCalls} calls completed in {overallSw.ElapsedMilliseconds}ms total");
        Console.WriteLine($"Individual timings: [{string.Join(", ", timings)}]ms");

        bool allSame = results.All(r => r == results[0]);
        Console.WriteLine($"All results identical: {allSame}");
        Console.WriteLine($"Result: Median={results[0].MedianMs:F4}ms");

        int measurementCount = timings.Count(t => t > 5);
        int cachedCount = timings.Count(t => t <= 5);
        Console.WriteLine($"Estimated: {measurementCount} measured, {cachedCount} cached/waited");
        Console.WriteLine($"Expected: Only 1 actual measurement should have occurred!\n");

        // Test 4: Rapid sequential calls
        Console.WriteLine("Test 4: Rapid sequential calls (all should be cached)");
        Console.WriteLine("------------------------------------------");
        tracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 10_000, cacheDuration: TimeSpan.FromSeconds(5));

        tracker.Measure(); // Prime the cache

        for (int i = 0; i < 5; i++)
        {
            sw.Restart();
            var stats = tracker.Measure();
            var elapsed = sw.ElapsedMilliseconds;
            Console.WriteLine($"Call {i + 1}: {elapsed}ms - {(elapsed < 1 ? "CACHED ✓" : "MEASURED ✗")}");
        }

        Console.WriteLine("\nDemo complete!");
    }
}
