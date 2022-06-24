using System.Threading.Tasks;
using System.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Berberis.StatsReporters;

public sealed class StatsRequestHandler
{
    private readonly IStatsReporterFactory _statsReporterFactory;

    public StatsRequestHandler(IStatsReporterFactory statsReporterFactory)
    {
        _statsReporterFactory = statsReporterFactory;
    }

    public async Task GetSystemInfo(HttpContext context)
    {
        await using var writer = new Utf8JsonWriter(context.Response.Body);
        writer.WriteStartObject();

        foreach (var kvp in _statsReporterFactory.GetSystemInfo())
        {
            writer.WriteString(kvp.Key, kvp.Value);
        }

        writer.WriteEndObject();
    }

    public async Task GetNetworkInfo(HttpContext context)
    {
        await using var writer = new Utf8JsonWriter(context.Response.Body);

        writer.WriteStartObject();
        writer.WritePropertyName("Network Interfaces");
        writer.WriteStartArray();

        foreach (var ni in _statsReporterFactory.GetNetworkInfo())
        {
            writer.WriteStartObject();

            foreach (var kvp in ni)
            {
                writer.WriteString(kvp.Key, kvp.Value);
            }

            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public async Task GetSystemStats(HttpContext context)
    {
        var systemStats = _statsReporterFactory.GetSystemStats();

        await using var writer = new Utf8JsonWriter(context.Response.Body);

        writer.WriteStartObject();

        writer.WriteNumber("IMs", systemStats.IntervalMs);
        writer.WriteNumber("CpuMs", systemStats.CpuTimeTotalMs);
        writer.WriteNumber("CpuIMs", systemStats.CpuTimeIntMs);
        writer.WriteNumber("CpuL", systemStats.CpuLoad);
        writer.WriteNumber("G0", systemStats.Gc0Collections);
        writer.WriteNumber("G1", systemStats.Gc1Collections);
        writer.WriteNumber("G2", systemStats.Gc2Collections);
        writer.WriteNumber("GCIndex", systemStats.GcIterationIndex);
        writer.WriteNumber("GCPausePct", systemStats.PauseTimePercentage);
        writer.WriteNumber("GCTimeBlockingMs", systemStats.GcTimeBlockingMs);
        writer.WriteNumber("GCTimeBackgroundMs", systemStats.GcTimeBackgroundMs);
        writer.WriteNumber("G0s", systemStats.TotalGc0s);
        writer.WriteNumber("G1s", systemStats.TotalGc1s);
        writer.WriteNumber("G2s", systemStats.TotalGc2s);
        writer.WriteNumber("WsB", systemStats.WorkSetBytes);
        writer.WriteNumber("GcB", systemStats.GcMemory);
        writer.WriteNumber("Threads", systemStats.NumberOfThreads);
        writer.WriteNumber("TPThreads", systemStats.ThreadPoolThreads);
        writer.WriteNumber("WorkItems", systemStats.PendingThreadPoolWorkItems);

        writer.WriteEndObject();
    }

    public async Task GetNetworkStats(HttpContext context)
    {
        var stats = _statsReporterFactory.GetNetworkStats();

        await using var writer = new Utf8JsonWriter(context.Response.Body);

        writer.WriteStartObject();

        writer.WriteNumber("IMs", stats.IntervalMs);
        writer.WriteNumber("RcvB", stats.BytesReceived);
        writer.WriteNumber("SntB", stats.BytesSent);
        writer.WriteNumber("RcvS", stats.ReceivedBytesPerSecond);
        writer.WriteNumber("SndS", stats.SentBytesPerSecond);

        writer.WriteEndObject();
    }

    public async Task ListReporters(HttpContext context)
    {
        await using var writer = new Utf8JsonWriter(context.Response.Body);

        writer.WriteStartObject();
        writer.WritePropertyName("Reporters");
        writer.WriteStartArray();

        foreach (var reporter in _statsReporterFactory.ListReporters())
        {
            writer.WriteStringValue(reporter);
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    public async Task GetReporterStats(HttpContext context)
    {
        var source = HttpUtility.UrlDecode(context.Request.RouteValues["source"].ToString());

        var systemStats = _statsReporterFactory.GetReporterStats(source);

        await using var writer = new Utf8JsonWriter(context.Response.Body);

        writer.WriteStartObject();

        writer.WriteNumber("IMs", systemStats.IntervalMs);
        writer.WriteNumber("Mps", systemStats.MessagesPerSecond);
        writer.WriteNumber("AST", systemStats.AvgServiceTime);
        writer.WriteNumber("Msgs", systemStats.TotalMessages);
        writer.WriteNumber("Bps", systemStats.BytesPerSecond);
        writer.WriteNumber("TB", systemStats.TotalBytes);

        writer.WriteEndObject();
    }
}
