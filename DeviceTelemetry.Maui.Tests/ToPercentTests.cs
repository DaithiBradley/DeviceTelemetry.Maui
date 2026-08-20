using Xunit;

namespace DeviceTelemetry.Maui.Tests;

/// <summary>
/// Tests for the ToPercent method.
/// </summary>
public sealed class ToPercentTests
{
    [Theory]
    [InlineData(0.0, 0)]
    [InlineData(0.25, 25)]
    [InlineData(0.5, 50)]
    [InlineData(0.75, 75)]
    [InlineData(1.0, 100)]
    public void ToPercent_WithValidInputs_ReturnsCorrectPercentage(double input, int expected)
    {
        // Act
        var result = DeviceTelemetryUtil.ToPercent(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(0.001, 0)]
    [InlineData(0.004, 0)]
    [InlineData(0.005, 0)] // 0.005 * 100 = 0.5, rounds to 0 (banker's rounding)
    [InlineData(0.245, 24)] // 0.245 * 100 = 24.5, rounds to 24 (banker's rounding)
    [InlineData(0.255, 26)] // 0.255 * 100 = 25.5, rounds to 26 (banker's rounding)
    [InlineData(0.995, 100)]
    [InlineData(0.999, 100)]
    public void ToPercent_WithDecimalInputs_RoundsCorrectly(double input, int expected)
    {
        // Act
        var result = DeviceTelemetryUtil.ToPercent(input);

        // Assert
        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(-1.0)]
    [InlineData(-0.5)]
    [InlineData(-0.1)]
    [InlineData(-100.0)]
    [InlineData(double.NaN)]
    public void ToPercent_WithUnknownInputs_ReturnsNull(double input)
    {
        DeviceTelemetryUtil.ToPercent(input).Should().BeNull();
    }

    [Theory]
    [InlineData(1.1, 100)]
    [InlineData(1.5, 100)]
    [InlineData(2.0, 100)]
    [InlineData(100.0, 100)]
    public void ToPercent_WithInputsAboveOne_ClampsToHundred(double input, int expected)
    {
        // Act
        var result = DeviceTelemetryUtil.ToPercent(input);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void ToPercent_WithZero_ReturnsZero()
    {
        // Act
        var result = DeviceTelemetryUtil.ToPercent(0.0);

        // Assert
        result.Should().Be(0);
    }

    [Fact]
    public void ToPercent_WithOne_ReturnsHundred()
    {
        // Act
        var result = DeviceTelemetryUtil.ToPercent(1.0);

        // Assert
        result.Should().Be(100);
    }

    [Fact]
    public void ToPercent_WithPreciseHalf_RoundsToFifty()
    {
        // Act
        var result = DeviceTelemetryUtil.ToPercent(0.5);

        // Assert
        result.Should().Be(50);
    }

    [Theory]
    [InlineData(0.123456789, 12)]
    [InlineData(0.987654321, 99)]
    [InlineData(0.333333333, 33)]
    [InlineData(0.666666666, 67)]
    public void ToPercent_WithPreciseDecimals_RoundsCorrectly(double input, int expected)
    {
        // Act
        var result = DeviceTelemetryUtil.ToPercent(input);

        // Assert
        result.Should().Be(expected);
    }
}

