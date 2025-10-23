using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Berberis.StatsReporters;

public sealed class StatsReporterFactory : IStatsReporterFactory
{
    private readonly ILogger<StatsReporterFactory> _logger;

    private readonly SystemStatsReporter _systemStatsReporter = new();
    private readonly NetworkStatsReporter _networkStatsReporter = new();

    private readonly ConcurrentDictionary<string, Lazy<StatsReporter>> _reporters =
        new();

    public StatsReporterFactory(ILogger<StatsReporterFactory> logger)
    {
        _logger = logger;
    }

    public IReadOnlyDictionary<string, string> GetSystemInfo() => _systemStatsReporter.SystemInfo;

    public IReadOnlyList<IReadOnlyDictionary<string, string>> GetNetworkInfo() =>
        _networkStatsReporter.NetworkInfo;

    public SystemStats GetSystemStats() => _systemStatsReporter.GetStats();

    public NetworkStats GetNetworkStats() => _networkStatsReporter.GetStats();

    public StatsReporter GetOrCreateReporter(string source) =>
        _reporters.GetOrAdd(source, n => new Lazy<StatsReporter>(() => new StatsReporter(source, RemoveReporter)))
            .Value;

    public IEnumerable<string> ListReporters() => _reporters.Keys.ToArray();

    public Stats GetReporterStats(string source)
    {
        if (_reporters.TryGetValue(source, out var reporter))
        {
            return reporter.Value.GetStats();
        }

        throw new KeyNotFoundException($"Couldn't find reporter with name {source}");
    }

    private void RemoveReporter(StatsReporter reporter)
    {
        if (!_reporters.TryRemove(reporter.Source, out var _))
        {
            _logger.LogWarning("Couldn't find reporter with name {source}", reporter.Source);
        }
    }
}
