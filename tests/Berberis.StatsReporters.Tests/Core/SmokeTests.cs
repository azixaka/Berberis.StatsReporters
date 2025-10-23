using FluentAssertions;
using Xunit;

namespace Berberis.StatsReporters.Tests.Core;

/// <summary>
/// Smoke tests to verify test infrastructure is working.
/// </summary>
public class SmokeTests
{
    [Fact]
    public void TestInfrastructure_IsWorking()
    {
        // Arrange & Act
        var value = 42;

        // Assert
        value.Should().Be(42);
    }

    [Fact]
    public void FluentAssertions_IsWorking()
    {
        // Arrange
        var list = new List<int> { 1, 2, 3 };

        // Assert
        list.Should().HaveCount(3);
        list.Should().Contain(2);
        list.Should().BeInAscendingOrder();
    }
}
