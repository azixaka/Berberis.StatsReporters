using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Berberis.StatsReporters.Tests.TestHelpers;

/// <summary>
/// Helper for creating test loggers.
/// </summary>
public static class TestLoggerFactory
{
    /// <summary>
    /// Creates a null logger factory (logs nothing).
    /// </summary>
    public static ILoggerFactory CreateNullLoggerFactory()
    {
        return NullLoggerFactory.Instance;
    }

    /// <summary>
    /// Creates a logger factory that writes to console (useful for debugging tests).
    /// </summary>
    public static ILoggerFactory CreateConsoleLoggerFactory()
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Debug);
        });
    }
}
