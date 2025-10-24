using System.Collections.Generic;

namespace Berberis.StatsReporters;

/// <summary>
/// Factory for creating and managing stats reporters with system/network monitoring.
/// </summary>
public interface IStatsReporterFactory
{
    /// <summary>
    /// Gets existing reporter or creates a new one for the specified source.
    /// </summary>
    StatsReporter GetOrCreateReporter(string source);

    /// <summary>
    /// Lists all registered reporter source names.
    /// </summary>
    IEnumerable<string> ListReporters();

    /// <summary>
    /// Gets statistics for a specific reporter.
    /// </summary>
    Stats GetReporterStats(string source);

    /// <summary>
    /// Gets system information (OS, runtime, etc.).
    /// </summary>
    IReadOnlyDictionary<string, string> GetSystemInfo();

    /// <summary>
    /// Gets network interface information.
    /// </summary>
    IReadOnlyList<IReadOnlyDictionary<string, string>> GetNetworkInfo();

    /// <summary>
    /// Gets current system statistics (CPU, memory, GC).
    /// </summary>
    SystemStats GetSystemStats();

    /// <summary>
    /// Gets current network statistics (bytes sent/received).
    /// </summary>
    NetworkStats GetNetworkStats();
}
