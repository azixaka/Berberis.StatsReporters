using Berberis.StatsReporters;
using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.Core;

/// <summary>
/// Tests for the Stats struct value storage and initialization.
/// </summary>
public class StatsTests
{
    [Fact]
    public void Stats_DefaultInitialization_AllFieldsZero()
    {
        // Arrange & Act
        var stats = new Stats();

        // Assert
        stats.IntervalMs.Should().Be(0);
        stats.MessagesPerSecond.Should().Be(0);
        stats.TotalMessages.Should().Be(0);
        stats.BytesPerSecond.Should().Be(0);
        stats.TotalBytes.Should().Be(0);
        stats.AvgServiceTime.Should().Be(0);
    }

    [Fact]
    public void Stats_Constructor_SetsAllFieldsCorrectly()
    {
        // Arrange
        const float intervalMs = 1000f;
        const float messagesPerSecond = 100f;
        const float totalMessages = 100f;
        const float bytesPerSecond = 50000f;
        const float totalBytes = 50000f;
        const float avgServiceTime = 10f;

        // Act
        var stats = new Stats(intervalMs, messagesPerSecond, totalMessages,
            bytesPerSecond, totalBytes, avgServiceTime);

        // Assert
        stats.IntervalMs.Should().Be(intervalMs);
        stats.MessagesPerSecond.Should().Be(messagesPerSecond);
        stats.TotalMessages.Should().Be(totalMessages);
        stats.BytesPerSecond.Should().Be(bytesPerSecond);
        stats.TotalBytes.Should().Be(totalBytes);
        stats.AvgServiceTime.Should().Be(avgServiceTime);
    }

    [Fact]
    public void Stats_WithZeroValues_StoresZeroCorrectly()
    {
        // Arrange & Act
        var stats = new Stats(0, 0, 0, 0, 0, 0);

        // Assert
        stats.IntervalMs.Should().Be(0);
        stats.MessagesPerSecond.Should().Be(0);
        stats.TotalMessages.Should().Be(0);
        stats.BytesPerSecond.Should().Be(0);
        stats.TotalBytes.Should().Be(0);
        stats.AvgServiceTime.Should().Be(0);
    }

    [Fact]
    public void Stats_WithLargeValues_StoresCorrectly()
    {
        // Arrange
        const float largeValue = float.MaxValue / 2;

        // Act
        var stats = new Stats(largeValue, largeValue, largeValue,
            largeValue, largeValue, largeValue);

        // Assert
        stats.IntervalMs.Should().Be(largeValue);
        stats.MessagesPerSecond.Should().Be(largeValue);
        stats.TotalMessages.Should().Be(largeValue);
        stats.BytesPerSecond.Should().Be(largeValue);
        stats.TotalBytes.Should().Be(largeValue);
        stats.AvgServiceTime.Should().Be(largeValue);
    }

    [Fact]
    public void Stats_WithNegativeValues_StoresNegativeCorrectly()
    {
        // Arrange - While negative values don't make semantic sense,
        // the struct should store them as-is
        const float negativeValue = -100f;

        // Act
        var stats = new Stats(negativeValue, negativeValue, negativeValue,
            negativeValue, negativeValue, negativeValue);

        // Assert
        stats.IntervalMs.Should().Be(negativeValue);
        stats.MessagesPerSecond.Should().Be(negativeValue);
        stats.TotalMessages.Should().Be(negativeValue);
        stats.BytesPerSecond.Should().Be(negativeValue);
        stats.TotalBytes.Should().Be(negativeValue);
        stats.AvgServiceTime.Should().Be(negativeValue);
    }

    [Theory]
    [InlineData(1000f, 100f, 100f, 50000f, 50000f, 10f)]
    [InlineData(500f, 200f, 100f, 100000f, 50000f, 5f)]
    [InlineData(2000f, 50f, 100f, 25000f, 50000f, 20f)]
    public void Stats_Constructor_MultipleScenarios_StoresCorrectly(
        float intervalMs,
        float messagesPerSecond,
        float totalMessages,
        float bytesPerSecond,
        float totalBytes,
        float avgServiceTime)
    {
        // Act
        var stats = new Stats(intervalMs, messagesPerSecond, totalMessages,
            bytesPerSecond, totalBytes, avgServiceTime);

        // Assert
        stats.IntervalMs.Should().Be(intervalMs);
        stats.MessagesPerSecond.Should().Be(messagesPerSecond);
        stats.TotalMessages.Should().Be(totalMessages);
        stats.BytesPerSecond.Should().Be(bytesPerSecond);
        stats.TotalBytes.Should().Be(totalBytes);
        stats.AvgServiceTime.Should().Be(avgServiceTime);
    }

    [Fact]
    public void Stats_WithFloatingPointPrecision_MaintainsPrecision()
    {
        // Arrange
        const float preciseValue = 123.456789f;

        // Act
        var stats = new Stats(preciseValue, preciseValue, preciseValue,
            preciseValue, preciseValue, preciseValue);

        // Assert
        stats.IntervalMs.Should().BeApproximately(preciseValue, 0.000001f);
        stats.MessagesPerSecond.Should().BeApproximately(preciseValue, 0.000001f);
        stats.TotalMessages.Should().BeApproximately(preciseValue, 0.000001f);
        stats.BytesPerSecond.Should().BeApproximately(preciseValue, 0.000001f);
        stats.TotalBytes.Should().BeApproximately(preciseValue, 0.000001f);
        stats.AvgServiceTime.Should().BeApproximately(preciseValue, 0.000001f);
    }
}
