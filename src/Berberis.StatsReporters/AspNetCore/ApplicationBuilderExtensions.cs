using System;
using System.Diagnostics;
using System.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Berberis.StatsReporters;

/// <summary>
/// ASP.NET Core integration extensions for stats reporting and diagnostics.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Maps stats endpoints to the route builder.
    /// </summary>
    /// <param name="endpoints">Route builder.</param>
    /// <param name="statsReporterFactory">Factory for accessing stats reporters.</param>
    /// <param name="prefix">Optional URL prefix (e.g., "api/").</param>
    public static void MapStats(this IEndpointRouteBuilder endpoints, IStatsReporterFactory statsReporterFactory, string prefix = "")
    {
        var handler = new StatsRequestHandler(statsReporterFactory);
        MapStats(endpoints, handler, prefix);
    }

    /// <summary>
    /// Adds stats endpoints to the application pipeline.
    /// </summary>
    /// <param name="appBuilder">Application builder.</param>
    /// <param name="statsReporterFactory">Factory for accessing stats reporters.</param>
    /// <param name="prefix">Optional URL prefix (e.g., "api/").</param>
    public static IApplicationBuilder UseStats(this IApplicationBuilder appBuilder, IStatsReporterFactory statsReporterFactory, string prefix = "")
    {
        var handler = new StatsRequestHandler(statsReporterFactory);

        appBuilder.UseEndpoints(endpoints =>
        {
            MapStats(endpoints, handler, prefix);
        });

        return appBuilder;
    }

    private static void MapStats(IEndpointRouteBuilder endpoints, StatsRequestHandler handler, string prefix)
    {
        endpoints.MapGet($"{prefix}stats/systeminfo", handler.GetSystemInfo);
        endpoints.MapGet($"{prefix}stats/networkinfo", handler.GetNetworkInfo);
        endpoints.MapGet($"{prefix}stats/systemstats", handler.GetSystemStats);
        endpoints.MapGet($"{prefix}stats/networkstats", handler.GetNetworkStats);
        endpoints.MapGet($"{prefix}stats/reporters", handler.ListReporters);
        endpoints.MapGet($"{prefix}stats/reporters/" + "{source}", handler.GetReporterStats);
    }

    /// <summary>
    /// Adds GC diagnostic endpoints to the application pipeline.
    /// </summary>
    /// <param name="appBuilder">Application builder.</param>
    /// <param name="prefix">Optional URL prefix (e.g., "api/").</param>
    public static IApplicationBuilder UseGCOperations(this IApplicationBuilder appBuilder, string prefix = "")
    {
        appBuilder.UseEndpoints(endpoints =>
        {
            MapGCOperations(endpoints, prefix);
        });

        return appBuilder;
    }

    /// <summary>
    /// Maps GC diagnostic endpoints (e.g., manual collection trigger).
    /// </summary>
    /// <param name="endpoints">Route builder.</param>
    /// <param name="prefix">Optional URL prefix (e.g., "api/").</param>
    public static void MapGCOperations(this IEndpointRouteBuilder endpoints, string prefix)
    {
        endpoints.MapGet($"{prefix}dbg/gccollect", async context =>
        {
            var sw = Stopwatch.StartNew();
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

            sw.Stop();
            await context.Response.WriteAsync($"GC took {sw.Elapsed.TotalMilliseconds:N1}ms");
        });
    }
}
