using System;
using System.Diagnostics;
using System.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Berberis.StatsReporters;

public static class ApplicationBuilderExtensions
{
    public static void MapStats(this IEndpointRouteBuilder endpoints, IStatsReporterFactory statsReporterFactory, string prefix = "")
    {
        var handler = new StatsRequestHandler(statsReporterFactory);
        MapStats(endpoints, handler, prefix);
    }

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

    public static IApplicationBuilder UseGCOperations(this IApplicationBuilder appBuilder, string prefix = "")
    {
        appBuilder.UseEndpoints(endpoints =>
        {
            MapGCOperations(endpoints, prefix);
        });

        return appBuilder;
    }

    public static void MapGCOperations(IEndpointRouteBuilder endpoints, string prefix)
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
