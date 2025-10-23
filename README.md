# Berberis.StatsReporters

[![NuGet Version](https://img.shields.io/nuget/v/Berberis.StatsReporters?style=flat-square&logo=nuget&label=NuGet)](https://www.nuget.org/packages/Berberis.StatsReporters)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Berberis.StatsReporters?style=flat-square&logo=nuget&label=Downloads)](https://www.nuget.org/packages/Berberis.StatsReporters)
[![Build Status](https://img.shields.io/github/actions/workflow/status/azixaka/Berberis/build-and-publish.yml?branch=master&style=flat-square&logo=github&label=Build)](https://github.com/azixaka/Berberis/actions)
[![License](https://img.shields.io/badge/License-MIT-blue?style=flat-square&logo=opensourceinitiative&logoColor=white)](https://github.com/azixaka/Berberis/blob/master/LICENSE)
[![.NET Version](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square&logo=dotnet)](https://dotnet.microsoft.com/download/dotnet/8.0)

A high-performance .NET library for real-time monitoring and metrics collection in ASP.NET Core applications. Track system resources, network activity, and custom operation metrics with minimal overhead.

## Installation

```bash
dotnet add package Berberis.StatsReporters
```

Or via Package Manager:

```powershell
Install-Package Berberis.StatsReporters
```

## Features

- **System Monitoring**: CPU usage, memory consumption, GC statistics, thread pool metrics
- **Network Monitoring**: Network interface statistics, bandwidth tracking per adapter
- **Custom Metrics**: Track throughput, latency, and service times for any operation
- **Thread Pool Latency**: Measure thread pool scheduling latency
- **Service Time Tracking**: Track operation latencies with percentile calculations (p50, p95, p99)
- **ASP.NET Core Integration**: Built-in HTTP endpoints to expose metrics
- **Thread-Safe**: All reporters are designed for concurrent access

## Quick Start

### Basic Usage with ASP.NET Core

```csharp
using Berberis.StatsReporters;

var builder = WebApplication.CreateBuilder(args);

// Register the stats reporter factory
builder.Services.AddSingleton<IStatsReporterFactory, StatsReporterFactory>();

var app = builder.Build();

// Get the factory and map stats endpoints
var statsFactory = app.Services.GetRequiredService<IStatsReporterFactory>();
app.MapStats(statsFactory, prefix: "/api/"); // Optional prefix

app.Run();
```

This exposes the following endpoints:

- `GET /api/stats/systeminfo` - Static system information
- `GET /api/stats/systemstats` - Real-time system metrics
- `GET /api/stats/networkinfo` - Network adapter information
- `GET /api/stats/networkstats` - Network bandwidth metrics
- `GET /api/stats/reporters` - List all custom reporters
- `GET /api/stats/reporters/{source}` - Get stats for a specific reporter

**Alternative:** You can also use `app.UseStats(statsFactory, prefix: "/api/")` if you prefer the UseEndpoints pattern.

## Tracking Custom Operations

### Simple Operation Tracking

```csharp
public class MyService
{
    private readonly StatsReporter _stats;

    public MyService(IStatsReporterFactory factory)
    {
        _stats = factory.GetOrCreateReporter("MyService.ProcessData");
    }

    public async Task ProcessDataAsync(byte[] data)
    {
        var start = _stats.Start();

        // Your operation here
        await DoWorkAsync(data);

        // Track the operation with byte count
        _stats.Stop(start, data.Length);
    }
}
```

### Recording Pre-Calculated Metrics

If you have pre-calculated metrics, you can record them directly:

```csharp
public class BatchProcessor
{
    private readonly StatsReporter _stats;

    public BatchProcessor(IStatsReporterFactory factory)
    {
        _stats = factory.GetOrCreateReporter("BatchProcessor");
    }

    public void RecordBatchMetrics(long itemsProcessed, float totalServiceTimeMs, long totalBytes)
    {
        // Record pre-calculated metrics
        _stats.Record(
            units: itemsProcessed,
            serviceTimeMs: totalServiceTimeMs,
            bytes: totalBytes
        );
    }
}
```

### Service Time Tracking with Percentiles

Track latencies and get percentile statistics:

```csharp
public class ApiHandler
{
    private readonly ServiceTimeTracker _tracker;

    public ApiHandler()
    {
        _tracker = new ServiceTimeTracker(
            ewmaWindowSize: 100,
            percentileOptions: new[]
            {
                new PercentileOptions(50),    // p50 (median)
                new PercentileOptions(95),    // p95
                new PercentileOptions(99),    // p99
                new PercentileOptions(99.9f)  // p99.9
            }
        );
    }

    public async Task<Response> HandleRequestAsync()
    {
        var start = ServiceTimeTracker.GetTicks();

        var response = await ProcessRequestAsync();

        _tracker.RecordServiceTime(start);

        return response;
    }

    public ServiceTimeStats GetStats()
    {
        return _tracker.GetStats(reset: false);
        // Returns: IntervalMs, ProcessRate, IntervalMessages, TotalProcessedMessages,
        // AvgServiceTimeMs, MinServiceTimeMs, MaxServiceTimeMs, PercentileValues[]
    }
}
```

### Thread Pool Latency Monitoring

Measure how long items wait in the thread pool queue:

```csharp
public class ThreadPoolMonitor
{
    private readonly ThreadPoolLatencyTracker _latencyTracker;

    public ThreadPoolMonitor()
    {
        _latencyTracker = new ThreadPoolLatencyTracker(numberOfMeasurements: 10_000);
    }

    public void CheckLatency()
    {
        var latencyStats = _latencyTracker.Measure();
        Console.WriteLine($"Thread pool latency:");
        Console.WriteLine($"  Median: {latencyStats.MedianMs:F2}ms");
        Console.WriteLine($"  P90: {latencyStats.P90Ms:F2}ms");
        Console.WriteLine($"  P99: {latencyStats.P99Ms:F2}ms");
        Console.WriteLine($"  P99.99: {latencyStats.P99_99Ms:F2}ms");
    }
}
```

## System Monitoring

### System Stats

```csharp
public class SystemMonitor
{
    private readonly SystemStatsReporter _systemStats;

    public SystemMonitor()
    {
        // Pass true to enable thread pool latency tracking
        _systemStats = new SystemStatsReporter(measureThreadPoolLatency: true);
    }

    public void PrintSystemInfo()
    {
        var info = _systemStats.SystemInfo;
        Console.WriteLine($"CPU Cores: {info["CPU Cores"]}");
        Console.WriteLine($"GC Server Mode: {info["GC Server Mode"]}");
        Console.WriteLine($"Process ID: {info["Process Id"]}");
    }

    public void MonitorResources()
    {
        var stats = _systemStats.GetStats();

        Console.WriteLine($"CPU Usage: {stats.CpuUsagePercent:F1}%");
        Console.WriteLine($"Working Set: {stats.WorkingSetBytes / 1024 / 1024} MB");
        Console.WriteLine($"GC Memory: {stats.GcTotalMemory / 1024 / 1024} MB");
        Console.WriteLine($"Thread Count: {stats.ThreadCount}");
        Console.WriteLine($"Thread Pool: {stats.ThreadPoolThreadCount}");
        Console.WriteLine($"GC Gen0: {stats.Gc0} collections");
    }
}
```

### Network Stats

```csharp
public class NetworkMonitor
{
    private readonly NetworkStatsReporter _networkStats;

    public NetworkMonitor()
    {
        _networkStats = new NetworkStatsReporter();
    }

    public void PrintNetworkInfo()
    {
        foreach (var adapter in _networkStats.NetworkInfo)
        {
            Console.WriteLine($"Adapter: {adapter["Name"]}");
            Console.WriteLine($"  Type: {adapter["Type"]}");
            Console.WriteLine($"  Status: {adapter["Status"]}");
            Console.WriteLine($"  Speed: {adapter["Speed"]} bps");
        }
    }

    public void MonitorBandwidth()
    {
        var stats = _networkStats.GetStats();
        var primary = stats.PrimaryAdapter;

        Console.WriteLine($"Download: {primary.RcvBytesPerSecond / 1024 / 1024:F2} MB/s");
        Console.WriteLine($"Upload: {primary.SndBytesPerSecond / 1024 / 1024:F2} MB/s");

        // Per-adapter stats
        foreach (var (name, adapterStats) in stats.AdapterStats)
        {
            Console.WriteLine($"{name}: ↓{adapterStats.RcvBytesPerSecond / 1024:F0} KB/s " +
                            $"↑{adapterStats.SndBytesPerSecond / 1024:F0} KB/s");
        }
    }
}
```

## Advanced Features

### Using the Stats Reporter Factory

The factory manages multiple reporters and provides lifecycle management:

```csharp
public class ApplicationMetrics
{
    private readonly IStatsReporterFactory _factory;

    public ApplicationMetrics(IStatsReporterFactory factory)
    {
        _factory = factory;
    }

    public void TrackOperation(string operationName, Action operation)
    {
        var reporter = _factory.GetOrCreateReporter(operationName);
        var start = reporter.Start();

        operation();

        reporter.Stop(start);
    }

    public void PrintAllStats()
    {
        foreach (var reporterName in _factory.ListReporters())
        {
            var stats = _factory.GetReporterStats(reporterName);
            Console.WriteLine($"{reporterName}:");
            Console.WriteLine($"  Rate: {stats.MessagesPerSecond:F1} ops/s");
            Console.WriteLine($"  Avg Time: {stats.AvgServiceTime:F2} ms");
            Console.WriteLine($"  Total: {stats.TotalMessages}");
        }
    }
}
```

### GC Operations Endpoint

Enable manual GC collection for debugging (use with caution in production):

```csharp
app.MapGCOperations(prefix: "/api/");
// Exposes: GET /api/dbg/gccollect

// Alternative using UseEndpoints pattern:
app.UseGCOperations(prefix: "/api/");
```

## Stats Output

### Stats Structure

```csharp
public readonly struct Stats
{
    public readonly float IntervalMs;           // Measurement window in ms
    public readonly float MessagesPerSecond;    // Operations per second
    public readonly float TotalMessages;        // Total operations counted
    public readonly float BytesPerSecond;       // Throughput in bytes/s
    public readonly float TotalBytes;           // Total bytes processed
    public readonly float AvgServiceTime;       // Average latency in ms
}
```

### System Stats Structure

Includes CPU usage, memory, GC stats, thread pool metrics, lock contention, and more.

### Service Time Stats

```csharp
public readonly struct ServiceTimeStats
{
    public readonly float IntervalMs;                  // Measurement window in ms
    public readonly float ProcessRate;                 // Operations per second
    public readonly long IntervalMessages;             // Messages in this interval
    public readonly long TotalProcessedMessages;       // Total messages processed
    public readonly float AvgServiceTimeMs;            // Average latency
    public readonly float MinServiceTimeMs;            // Minimum latency
    public readonly float MaxServiceTimeMs;            // Maximum latency
    public readonly (float percentile, float value)[] PercentileValues; // Configured percentiles
}
```

## Performance Considerations

- All reporters use lock-free operations where possible (Interlocked operations)
- Minimal allocation during tracking (struct-based stats)
- Thread-safe by design
- Negligible overhead for operation tracking (<100ns per operation)
- Stats calculation is separate from tracking (pull-based model)

## Use Cases

- **Production Monitoring**: Track application health and performance
- **Load Testing**: Measure throughput and latency under load
- **Debugging**: Identify performance bottlenecks
- **SLA Monitoring**: Track percentile latencies
- **Capacity Planning**: Monitor resource utilization trends

## Requirements

- .NET 8.0 or higher
- ASP.NET Core (for endpoint integration)

## License

See LICENSE file in the repository.

## Contributing

Contributions are welcome! Please submit issues and pull requests on [GitHub](https://github.com/azixaka/Berberis).

## Links

- [NuGet Package](https://www.nuget.org/packages/Berberis.StatsReporters)
- [GitHub Repository](https://github.com/azixaka/Berberis)
