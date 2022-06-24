using System;
using System.Diagnostics;
using System.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Berberis.StatsReporters;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseStats(this IApplicationBuilder appBuilder, IStatsReporterFactory statsReporterFactory)
    {
        var handler = new StatsRequestHandler(statsReporterFactory);

        appBuilder.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("stats/systeminfo", handler.GetSystemInfo);
            endpoints.MapGet("stats/networkinfo", handler.GetNetworkInfo);
            endpoints.MapGet("stats/systemstats", handler.GetSystemStats);
            endpoints.MapGet("stats/networkstats", handler.GetNetworkStats);
            endpoints.MapGet("stats/reporters", handler.ListReporters);
            endpoints.MapGet("stats/reporters/{source}", handler.GetReporterStats);
        });

        return appBuilder;
    }

    public static IApplicationBuilder UseGCOperations(this IApplicationBuilder appBuilder)
    {
        appBuilder.UseEndpoints(endpoints =>
        {
            endpoints.MapGet("dbg/gccollect", async context =>
            {
                var sw = Stopwatch.StartNew();
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);

                sw.Stop();
                await context.Response.WriteAsync($"GC took {sw.Elapsed.TotalMilliseconds:N1}ms");
            });
        });

        return appBuilder;
    }
}
