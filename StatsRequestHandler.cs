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

        WriteNumber(writer, "IMs", systemStats.IntervalMs);
        WriteNumber(writer, "CpuMs", systemStats.CpuTimeTotalMs);
        WriteNumber(writer, "CpuIMs", systemStats.CpuTimeIntMs);
        WriteNumber(writer, "CpuL", systemStats.CpuLoad);
        WriteNumber(writer, "G0", systemStats.Gc0Collections);
        WriteNumber(writer, "G1", systemStats.Gc1Collections);
        WriteNumber(writer, "G2", systemStats.Gc2Collections);
        WriteNumber(writer, "GCIndex", systemStats.GcIterationIndex);
        WriteNumber(writer, "GCPausePct", systemStats.PauseTimePercentage);
        WriteNumber(writer, "GCTimeBlockingMs", systemStats.GcTimeBlockingMs);
        WriteNumber(writer, "GCTimeBackgroundMs", systemStats.GcTimeBackgroundMs);
        WriteNumber(writer, "G0s", systemStats.TotalGc0s);
        WriteNumber(writer, "G1s", systemStats.TotalGc1s);
        WriteNumber(writer, "G2s", systemStats.TotalGc2s);
        WriteNumber(writer, "WsB", systemStats.WorkSetBytes);
        WriteNumber(writer, "GcB", systemStats.GcMemory);
        WriteNumber(writer, "Threads", systemStats.NumberOfThreads);
        WriteNumber(writer, "TPThreads", systemStats.ThreadPoolThreads);
        WriteNumber(writer, "WorkItems", systemStats.PendingThreadPoolWorkItems);
        WriteNumber(writer, "CompletedWorkItems", systemStats.CompletedThreadPoolWorkItems);
        WriteNumber(writer, "CompletedWorkItemsPerSecond", systemStats.CompletedThreadPoolWorkItemsPerSecond);
        writer.WriteBoolean("GcCompacted", systemStats.GcCompacted);
        writer.WriteBoolean("GcBackground", systemStats.GcBackground);
        WriteNumber(writer, "PromotedBytesInterval", systemStats.PromotedBytesInterval);
        WriteNumber(writer, "LockContentions", systemStats.LockContentions);

        writer.WriteEndObject();
    }

    public Task GetNetworkStats(HttpContext context)
    {
        var networkStats = _statsReporterFactory.GetNetworkStats();

        using var writer = new Utf8JsonWriter(context.Response.Body);

        writer.WriteStartObject();

        WriteNumber(writer, "IMs", networkStats.IntervalMs);
        writer.WriteNumber("RcvB", networkStats.BytesReceived);
        writer.WriteNumber("SntB", networkStats.BytesSent);
        WriteNumber(writer, "RcvS", networkStats.ReceivedBytesPerSecond);
        WriteNumber(writer, "SndS", networkStats.SentBytesPerSecond);

        writer.WritePropertyName("Interfaces");
        writer.WriteStartArray();

        foreach (var (name, stats) in networkStats.InterfaceStats)
        {
            writer.WriteStartObject();
            writer.WriteString("Name", name);
            writer.WriteNumber("RcvB", stats.BytesReceived);
            writer.WriteNumber("SntB", stats.BytesSent);
            WriteNumber(writer, "RcvS", stats.ReceivedBytesPerSecond);
            WriteNumber(writer, "SndS", stats.SentBytesPerSecond);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();

        return Task.CompletedTask;
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

        WriteNumber(writer, "IMs", systemStats.IntervalMs);
        WriteNumber(writer, "Mps", systemStats.MessagesPerSecond);
        WriteNumber(writer, "AST", systemStats.AvgServiceTime);
        WriteNumber(writer, "Msgs", systemStats.TotalMessages);
        WriteNumber(writer, "Bps", systemStats.BytesPerSecond);
        WriteNumber(writer, "TB", systemStats.TotalBytes);

        writer.WriteEndObject();
    }

    private static void WriteNumber(Utf8JsonWriter writer, string name, float value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            writer.WriteNull(name);
        else
            writer.WriteNumber(name, value);
    }
}
