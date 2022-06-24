using System.Collections.Generic;

namespace Berberis.StatsReporters;

public interface IStatsReporterFactory
{
    StatsReporter GetOrCreateReporter(string source);
    IEnumerable<string> ListReporters();
    Stats GetReporterStats(string source);
    IReadOnlyDictionary<string, string> GetSystemInfo();
    IReadOnlyList<IReadOnlyDictionary<string, string>> GetNetworkInfo();
    SystemStats GetSystemStats();
    NetworkStats GetNetworkStats();
}
