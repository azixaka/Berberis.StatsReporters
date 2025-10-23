using System.Text;
using System.Text.Json;
using Berberis.StatsReporters;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Berberis.StatsReporters.Tests.AspNetCore;

/// <summary>
/// Tests for StatsRequestHandler HTTP endpoint handlers.
/// </summary>
public class StatsRequestHandlerTests
{
    private readonly Mock<IStatsReporterFactory> _mockFactory;
    private readonly StatsRequestHandler _handler;

    public StatsRequestHandlerTests()
    {
        _mockFactory = new Mock<IStatsReporterFactory>();
        _handler = new StatsRequestHandler(_mockFactory.Object);
    }

    #region System Endpoints Tests

    [Fact]
    public async Task GetSystemInfo_ReturnsSystemInformationAsJson()
    {
        // Arrange
        var systemInfo = new Dictionary<string, string>
        {
            { "OS", "Linux" },
            { "Runtime", ".NET 8.0" },
            { "MachineName", "test-machine" },
            { "ProcessorCount", "8" }
        };

        _mockFactory.Setup(f => f.GetSystemInfo()).Returns(systemInfo);

        var context = CreateHttpContext();

        // Act
        await _handler.GetSystemInfo(context);

        // Assert
        var json = GetResponseJson(context);
        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

        result.Should().NotBeNull();
        result.Should().HaveCount(4);
        result!["OS"].Should().Be("Linux");
        result["Runtime"].Should().Be(".NET 8.0");
        result["MachineName"].Should().Be("test-machine");
        result["ProcessorCount"].Should().Be("8");
    }

    [Fact]
    public async Task GetSystemStats_ReturnsSystemStatisticsAsJson()
    {
        // Arrange
        var latencyStats = new LatencyStats(
            MedianMs: 5.0,
            P90Ms: 10.0,
            P99Ms: 20.0,
            P99_99Ms: 50.0
        );

        var systemStats = new SystemStats(
            intervalMs: 1000,
            cpuTimeTotalMs: 5000,
            cpuTimeIntMs: 100,
            cpuLoad: 0.25f,
            gc0Collections: 10,
            gc1Collections: 5,
            gc2Collections: 2,
            gcIterationIndex: 123,
            pauseTimePercentage: 1.5f,
            gcTimeBlockingMs: 50,
            gcTimeBackgroundMs: 25,
            totalGc0s: 100,
            totalGc1s: 50,
            totalGc2s: 20,
            workSetBytes: 100_000_000f,
            gcMemory: 50_000_000f,
            numberOfThreads: 25,
            threadPoolThreads: 10,
            pendingThreadPoolWorkItems: 5,
            completedThreadPoolWorkItems: 1000,
            completedThreadPoolWorkItemsPerSecond: 100,
            gcCompacted: true,
            gcBackground: false,
            promotedBytesInterval: 1024,
            lockContentions: 3,
            threadPoolLatency: latencyStats
        );

        _mockFactory.Setup(f => f.GetSystemStats()).Returns(systemStats);

        var context = CreateHttpContext();

        // Act
        await _handler.GetSystemStats(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("IMs").GetSingle().Should().BeApproximately(1000f, 0.01f);
        root.GetProperty("CpuMs").GetSingle().Should().BeApproximately(5000f, 0.01f);
        root.GetProperty("CpuIMs").GetSingle().Should().BeApproximately(100f, 0.01f);
        root.GetProperty("CpuL").GetSingle().Should().BeApproximately(0.25f, 0.01f);
        root.GetProperty("G0").GetSingle().Should().BeApproximately(10f, 0.01f);
        root.GetProperty("G1").GetSingle().Should().BeApproximately(5f, 0.01f);
        root.GetProperty("G2").GetSingle().Should().BeApproximately(2f, 0.01f);
        root.GetProperty("GCIndex").GetInt64().Should().Be(123);
        root.GetProperty("GCPausePct").GetSingle().Should().BeApproximately(1.5f, 0.01f);
        root.GetProperty("GcCompacted").GetBoolean().Should().BeTrue();
        root.GetProperty("GcBackground").GetBoolean().Should().BeFalse();
        root.GetProperty("TPMedianMs").GetDouble().Should().BeApproximately(5.0, 0.01);
        root.GetProperty("TPP90Ms").GetDouble().Should().BeApproximately(10.0, 0.01);
        root.GetProperty("TPP99Ms").GetDouble().Should().BeApproximately(20.0, 0.01);
        root.GetProperty("TPP99_99Ms").GetDouble().Should().BeApproximately(50.0, 0.01);
    }

    [Fact]
    public async Task GetSystemStats_WithoutThreadPoolLatency_OmitsLatencyFields()
    {
        // Arrange
        var systemStats = new SystemStats(
            intervalMs: 1000,
            cpuTimeTotalMs: 5000,
            cpuTimeIntMs: 100,
            cpuLoad: 0.25f,
            gc0Collections: 10,
            gc1Collections: 5,
            gc2Collections: 2,
            gcIterationIndex: 123,
            pauseTimePercentage: 1.5f,
            gcTimeBlockingMs: 50,
            gcTimeBackgroundMs: 25,
            totalGc0s: 100,
            totalGc1s: 50,
            totalGc2s: 20,
            workSetBytes: 100_000_000f,
            gcMemory: 50_000_000f,
            numberOfThreads: 25,
            threadPoolThreads: 10,
            pendingThreadPoolWorkItems: 5,
            completedThreadPoolWorkItems: 1000,
            completedThreadPoolWorkItemsPerSecond: 100,
            gcCompacted: true,
            gcBackground: false,
            promotedBytesInterval: 1024,
            lockContentions: 3,
            threadPoolLatency: null
        );

        _mockFactory.Setup(f => f.GetSystemStats()).Returns(systemStats);

        var context = CreateHttpContext();

        // Act
        await _handler.GetSystemStats(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("IMs").GetSingle().Should().BeApproximately(1000f, 0.01f);
        root.TryGetProperty("TPMedianMs", out _).Should().BeFalse("ThreadPoolLatency fields should not be present");
        root.TryGetProperty("TPP90Ms", out _).Should().BeFalse();
        root.TryGetProperty("TPP99Ms", out _).Should().BeFalse();
        root.TryGetProperty("TPP99_99Ms", out _).Should().BeFalse();
    }

    [Fact]
    public async Task GetSystemStats_WithNaNValues_WritesNullToJson()
    {
        // Arrange
        var latencyStats = new LatencyStats(
            MedianMs: double.NaN,
            P90Ms: 10.0,
            P99Ms: double.PositiveInfinity,
            P99_99Ms: 50.0
        );

        var systemStats = new SystemStats(
            intervalMs: 1000,
            cpuTimeTotalMs: 5000,
            cpuTimeIntMs: 100,
            cpuLoad: float.NaN,
            gc0Collections: 10,
            gc1Collections: 5,
            gc2Collections: 2,
            gcIterationIndex: 123,
            pauseTimePercentage: float.PositiveInfinity,
            gcTimeBlockingMs: 50,
            gcTimeBackgroundMs: 25,
            totalGc0s: 100,
            totalGc1s: 50,
            totalGc2s: 20,
            workSetBytes: 100_000_000f,
            gcMemory: 50_000_000f,
            numberOfThreads: 25,
            threadPoolThreads: 10,
            pendingThreadPoolWorkItems: 5,
            completedThreadPoolWorkItems: 1000,
            completedThreadPoolWorkItemsPerSecond: 100,
            gcCompacted: true,
            gcBackground: false,
            promotedBytesInterval: 1024,
            lockContentions: 3,
            threadPoolLatency: latencyStats
        );

        _mockFactory.Setup(f => f.GetSystemStats()).Returns(systemStats);

        var context = CreateHttpContext();

        // Act
        await _handler.GetSystemStats(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("CpuL").ValueKind.Should().Be(JsonValueKind.Null, "NaN should serialize as null");
        root.GetProperty("GCPausePct").ValueKind.Should().Be(JsonValueKind.Null, "Infinity should serialize as null");
        root.GetProperty("TPMedianMs").ValueKind.Should().Be(JsonValueKind.Null, "NaN should serialize as null");
        root.GetProperty("TPP99Ms").ValueKind.Should().Be(JsonValueKind.Null, "Infinity should serialize as null");
        root.GetProperty("TPP90Ms").GetDouble().Should().BeApproximately(10.0, 0.01, "Valid values should serialize normally");
    }

    #endregion

    #region Network Endpoints Tests

    [Fact]
    public async Task GetNetworkInfo_ReturnsNetworkInterfacesAsJson()
    {
        // Arrange
        var networkInfo = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                { "Name", "eth0" },
                { "Description", "Ethernet Adapter" },
                { "Speed", "1000000000" },
                { "Status", "Up" }
            },
            new Dictionary<string, string>
            {
                { "Name", "lo" },
                { "Description", "Loopback" },
                { "Speed", "0" },
                { "Status", "Up" }
            }
        };

        _mockFactory.Setup(f => f.GetNetworkInfo()).Returns(networkInfo);

        var context = CreateHttpContext();

        // Act
        await _handler.GetNetworkInfo(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("Network Interfaces", out var interfaces).Should().BeTrue();
        interfaces.GetArrayLength().Should().Be(2);

        var eth0 = interfaces[0];
        eth0.GetProperty("Name").GetString().Should().Be("eth0");
        eth0.GetProperty("Description").GetString().Should().Be("Ethernet Adapter");
        eth0.GetProperty("Speed").GetString().Should().Be("1000000000");

        var lo = interfaces[1];
        lo.GetProperty("Name").GetString().Should().Be("lo");
        lo.GetProperty("Description").GetString().Should().Be("Loopback");
    }

    [Fact]
    public async Task GetNetworkStats_ReturnsNetworkStatisticsAsJson()
    {
        // Arrange
        var interfaceStats = new[]
        {
            new KeyValuePair<string, InterfaceStats>(
                "eth0",
                new InterfaceStats(
                    bytesReceived: 40_000,
                    bytesSent: 20_000,
                    receivedBytesPerSecond: 40_000,
                    sentBytesPerSecond: 20_000
                )
            ),
            new KeyValuePair<string, InterfaceStats>(
                "lo",
                new InterfaceStats(
                    bytesReceived: 10_000,
                    bytesSent: 5_000,
                    receivedBytesPerSecond: 10_000,
                    sentBytesPerSecond: 5_000
                )
            )
        };

        var primaryInterface = new InterfaceStats(
            bytesReceived: 50_000,
            bytesSent: 25_000,
            receivedBytesPerSecond: 50_000,
            sentBytesPerSecond: 25_000
        );

        var networkStats = new NetworkStats(
            intervalMs: 1000,
            interfaceStats: interfaceStats,
            primaryInterface: primaryInterface
        );

        _mockFactory.Setup(f => f.GetNetworkStats()).Returns(networkStats);

        var context = CreateHttpContext();

        // Act
        await _handler.GetNetworkStats(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("IMs").GetSingle().Should().BeApproximately(1000f, 0.01f);
        root.GetProperty("RcvB").GetInt64().Should().Be(50_000);
        root.GetProperty("SntB").GetInt64().Should().Be(25_000);
        root.GetProperty("RcvS").GetSingle().Should().BeApproximately(50_000f, 0.01f);
        root.GetProperty("SndS").GetSingle().Should().BeApproximately(25_000f, 0.01f);

        var interfaces = root.GetProperty("Interfaces");
        interfaces.GetArrayLength().Should().Be(2);

        var eth0 = interfaces[0];
        eth0.GetProperty("Name").GetString().Should().Be("eth0");
        eth0.GetProperty("RcvB").GetInt64().Should().Be(40_000);
        eth0.GetProperty("SntB").GetInt64().Should().Be(20_000);
        eth0.GetProperty("RcvS").GetSingle().Should().BeApproximately(40_000f, 0.01f);
        eth0.GetProperty("SndS").GetSingle().Should().BeApproximately(20_000f, 0.01f);

        var lo = interfaces[1];
        lo.GetProperty("Name").GetString().Should().Be("lo");
        lo.GetProperty("RcvB").GetInt64().Should().Be(10_000);
        lo.GetProperty("SntB").GetInt64().Should().Be(5_000);
    }

    [Fact]
    public async Task GetNetworkStats_WithNaNValues_WritesNullToJson()
    {
        // Arrange
        var primaryInterface = new InterfaceStats(
            bytesReceived: 50_000,
            bytesSent: 25_000,
            receivedBytesPerSecond: float.NaN,
            sentBytesPerSecond: float.PositiveInfinity
        );

        var networkStats = new NetworkStats(
            intervalMs: 1000,
            interfaceStats: Array.Empty<KeyValuePair<string, InterfaceStats>>(),
            primaryInterface: primaryInterface
        );

        _mockFactory.Setup(f => f.GetNetworkStats()).Returns(networkStats);

        var context = CreateHttpContext();

        // Act
        await _handler.GetNetworkStats(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("RcvS").ValueKind.Should().Be(JsonValueKind.Null, "NaN should serialize as null");
        root.GetProperty("SndS").ValueKind.Should().Be(JsonValueKind.Null, "Infinity should serialize as null");
    }

    #endregion

    #region Reporter Endpoints Tests

    [Fact]
    public async Task ListReporters_ReturnsReporterNamesAsJsonArray()
    {
        // Arrange
        var reporters = new List<string> { "http-requests", "database-queries", "cache-operations" };

        _mockFactory.Setup(f => f.ListReporters()).Returns(reporters);

        var context = CreateHttpContext();

        // Act
        await _handler.ListReporters(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("Reporters", out var reportersArray).Should().BeTrue();
        reportersArray.GetArrayLength().Should().Be(3);
        reportersArray[0].GetString().Should().Be("http-requests");
        reportersArray[1].GetString().Should().Be("database-queries");
        reportersArray[2].GetString().Should().Be("cache-operations");
    }

    [Fact]
    public async Task ListReporters_WithNoReporters_ReturnsEmptyArray()
    {
        // Arrange
        _mockFactory.Setup(f => f.ListReporters()).Returns(new List<string>());

        var context = CreateHttpContext();

        // Act
        await _handler.ListReporters(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.TryGetProperty("Reporters", out var reportersArray).Should().BeTrue();
        reportersArray.GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GetReporterStats_ReturnsReporterStatisticsAsJson()
    {
        // Arrange
        var reporterStats = new Stats(
            intervalMs: 1000,
            messagesPerSecond: 1500,
            totalMessages: 100_000,
            bytesPerSecond: 75_000,
            totalBytes: 5_000_000,
            avgServiceTime: 25.5f
        );

        _mockFactory.Setup(f => f.GetReporterStats("http-requests")).Returns(reporterStats);

        var context = CreateHttpContext();
        context.Request.RouteValues["source"] = "http-requests";

        // Act
        await _handler.GetReporterStats(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("IMs").GetSingle().Should().BeApproximately(1000f, 0.01f);
        root.GetProperty("Mps").GetSingle().Should().BeApproximately(1500f, 0.01f);
        root.GetProperty("AST").GetSingle().Should().BeApproximately(25.5f, 0.01f);
        root.GetProperty("Msgs").GetSingle().Should().BeApproximately(100_000f, 0.01f);
        root.GetProperty("Bps").GetSingle().Should().BeApproximately(75_000f, 0.01f);
        root.GetProperty("TB").GetSingle().Should().BeApproximately(5_000_000f, 0.01f);

        _mockFactory.Verify(f => f.GetReporterStats("http-requests"), Times.Once);
    }

    [Fact]
    public async Task GetReporterStats_WithUrlEncodedSource_DecodesCorrectly()
    {
        // Arrange
        var reporterStats = new Stats(
            intervalMs: 1000,
            messagesPerSecond: 500,
            totalMessages: 1000,
            bytesPerSecond: 5000,
            totalBytes: 50000,
            avgServiceTime: 10.0f
        );

        _mockFactory.Setup(f => f.GetReporterStats("test source with spaces")).Returns(reporterStats);

        var context = CreateHttpContext();
        context.Request.RouteValues["source"] = "test+source+with+spaces"; // URL encoded

        // Act
        await _handler.GetReporterStats(context);

        // Assert
        var json = GetResponseJson(context);
        json.Should().NotBeEmpty();

        _mockFactory.Verify(f => f.GetReporterStats("test source with spaces"), Times.Once);
    }

    [Fact]
    public async Task GetReporterStats_WithNaNValues_WritesNullToJson()
    {
        // Arrange
        var reporterStats = new Stats(
            intervalMs: 1000,
            messagesPerSecond: float.NaN,
            totalMessages: 100,
            bytesPerSecond: 1000,
            totalBytes: 10000,
            avgServiceTime: float.PositiveInfinity
        );

        _mockFactory.Setup(f => f.GetReporterStats("test")).Returns(reporterStats);

        var context = CreateHttpContext();
        context.Request.RouteValues["source"] = "test";

        // Act
        await _handler.GetReporterStats(context);

        // Assert
        var json = GetResponseJson(context);
        var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        root.GetProperty("Mps").ValueKind.Should().Be(JsonValueKind.Null, "NaN should serialize as null");
        root.GetProperty("AST").ValueKind.Should().Be(JsonValueKind.Null, "Infinity should serialize as null");
        root.GetProperty("Msgs").GetSingle().Should().BeApproximately(100f, 0.01f, "Valid values should serialize normally");
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static string GetResponseJson(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return reader.ReadToEnd();
    }

    #endregion
}
