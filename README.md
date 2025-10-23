# Berberis.StatsReporters

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

## Tracking Custom Operations

### Simple Operation Tracking

```csharp
public class MyService
{
    private readonly StatsReporter _stats;

    public MyService(IStatsReporterFactory factory)
    {
        _stats = factory.GetOrCreate("MyService.ProcessData");
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

### Service Time Tracking with Percentiles

Track latencies and get percentile statistics:

```csharp
public class ApiHandler
{
    private readonly ServiceTimeTracker _tracker;

    public ApiHandler()
    {
        _tracker = new ServiceTimeTracker(
            new PercentileOptions(
                percentiles: new[] { 50, 95, 99, 99.9 },
                windowSize: 1000
            )
        );
    }

    public async Task<Response> HandleRequestAsync()
    {
        var start = Stopwatch.GetTimestamp();

        var response = await ProcessRequestAsync();

        var elapsed = StatsReporter.ElapsedSince(start);
        _tracker.Record(elapsed);

        return response;
    }

    public ServiceTimeStats GetStats()
    {
        return _tracker.GetStats();
        // Returns: p50, p95, p99, p99.9, average, count
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
        _latencyTracker = new ThreadPoolLatencyTracker();
    }

    public void CheckLatency()
    {
        var latencyMs = _latencyTracker.Measure();
        Console.WriteLine($"Thread pool latency: {latencyMs:F2}ms");
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
        var reporter = _factory.GetOrCreate(operationName);
        var start = reporter.Start();

        operation();

        reporter.Stop(start);
    }

    public void PrintAllStats()
    {
        foreach (var reporter in _factory.GetReporters())
        {
            if (!reporter.IsChanged) continue;

            var stats = reporter.GetStats();
            Console.WriteLine($"{reporter.Source}:");
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
public class ServiceTimeStats
{
    public double P50 { get; }      // Median latency
    public double P95 { get; }      // 95th percentile
    public double P99 { get; }      // 99th percentile
    public double P999 { get; }     // 99.9th percentile
    public double Average { get; }  // Mean latency
    public long Count { get; }      // Sample count
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

- .NET 7.0 or higher
- ASP.NET Core (for endpoint integration)

## License

See LICENSE file in the repository.

## Contributing

Contributions are welcome! Please submit issues and pull requests on [GitHub](https://github.com/azixaka/Berberis).

## Links

- [NuGet Package](https://www.nuget.org/packages/Berberis.StatsReporters)
- [GitHub Repository](https://github.com/azixaka/Berberis)
