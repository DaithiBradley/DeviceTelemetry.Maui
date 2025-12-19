using DeviceTelemetry.Maui.Dtos;
using Xunit;

namespace DeviceTelemetry.Maui.Tests;

/// <summary>
/// Tests for DTO classes.
/// </summary>
public sealed class DtoTests
{
    [Fact]
    public void DeviceTelemetryDto_DefaultConstructor_InitializesCorrectly()
    {
        // Act
        var dto = new DeviceTelemetryDto();

        // Assert
        dto.DeviceId.Should().BeEmpty();
        dto.CapturedAtUtc.Should().Be(default(DateTimeOffset));
        dto.Location.Should().BeNull();
        dto.Battery.Should().NotBeNull();
        dto.GpsQuality.Should().BeNull();
        dto.WindowsPower.Should().BeNull();
    }

    [Fact]
    public void DeviceTelemetryDto_Battery_IsInitialized()
    {
        // Act
        var dto = new DeviceTelemetryDto();

        // Assert
        dto.Battery.Should().NotBeNull();
        dto.Battery.LevelPercent.Should().Be(0);
        dto.Battery.State.Should().Be("Unknown");
        dto.Battery.PowerSource.Should().Be("Unknown");
    }

    [Fact]
    public void GeoFixDto_CanBeCreatedAndSet()
    {
        // Act
        var dto = new GeoFixDto
        {
            Latitude = 37.7749,
            Longitude = -122.4194,
            AccuracyMeters = 10.5,
            AltitudeMeters = 100.0,
            SpeedMetersPerSecond = 5.2,
            CourseDegrees = 90.0,
            FixTimestampUtc = DateTimeOffset.UtcNow
        };

        // Assert
        dto.Latitude.Should().Be(37.7749);
        dto.Longitude.Should().Be(-122.4194);
        dto.AccuracyMeters.Should().Be(10.5);
        dto.AltitudeMeters.Should().Be(100.0);
        dto.SpeedMetersPerSecond.Should().Be(5.2);
        dto.CourseDegrees.Should().Be(90.0);
        dto.FixTimestampUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GeoFixDto_NullableProperties_CanBeNull()
    {
        // Act
        var dto = new GeoFixDto
        {
            Latitude = 37.7749,
            Longitude = -122.4194
        };

        // Assert
        dto.AccuracyMeters.Should().BeNull();
        dto.AltitudeMeters.Should().BeNull();
        dto.SpeedMetersPerSecond.Should().BeNull();
        dto.CourseDegrees.Should().BeNull();
        dto.FixTimestampUtc.Should().BeNull();
    }

    [Fact]
    public void BatteryDto_CanBeCreatedAndSet()
    {
        // Act
        var dto = new BatteryDto
        {
            LevelPercent = 75,
            State = "Charging",
            PowerSource = "Usb"
        };

        // Assert
        dto.LevelPercent.Should().Be(75);
        dto.State.Should().Be("Charging");
        dto.PowerSource.Should().Be("Usb");
    }

    [Fact]
    public void BatteryDto_DefaultValues_AreSet()
    {
        // Act
        var dto = new BatteryDto();

        // Assert
        dto.LevelPercent.Should().Be(0);
        dto.State.Should().Be("Unknown");
        dto.PowerSource.Should().Be("Unknown");
    }

    [Fact]
    public void GpsQualityDto_CanBeCreatedAndSet()
    {
        // Act
        var dto = new GpsQualityDto
        {
            SatellitesInView = 12,
            SatellitesUsedInFix = 8,
            AverageCn0DbHz = 35.5,
            QualityBand = "Good"
        };

        // Assert
        dto.SatellitesInView.Should().Be(12);
        dto.SatellitesUsedInFix.Should().Be(8);
        dto.AverageCn0DbHz.Should().Be(35.5);
        dto.QualityBand.Should().Be("Good");
    }

    [Fact]
    public void GpsQualityDto_DefaultValues_AreSet()
    {
        // Act
        var dto = new GpsQualityDto();

        // Assert
        dto.SatellitesInView.Should().Be(0);
        dto.SatellitesUsedInFix.Should().Be(0);
        dto.AverageCn0DbHz.Should().BeNull();
        dto.QualityBand.Should().Be("Unknown");
    }

    [Fact]
    public void WindowsPowerTelemetryDto_CanBeCreatedAndSet()
    {
        // Act
        var dto = new WindowsPowerTelemetryDto
        {
            ScreenBrightnessPercent = 80,
            EnergySaverStatus = "Off",
            PowerSupplyStatus = "AC",
            BatteryStatus = "Charging",
            ActivePowerPlanName = "Balanced",
            ActivePowerPlanGuid = Guid.NewGuid()
        };

        // Assert
        dto.ScreenBrightnessPercent.Should().Be(80);
        dto.EnergySaverStatus.Should().Be("Off");
        dto.PowerSupplyStatus.Should().Be("AC");
        dto.BatteryStatus.Should().Be("Charging");
        dto.ActivePowerPlanName.Should().Be("Balanced");
        dto.ActivePowerPlanGuid.Should().NotBeEmpty();
    }

    [Fact]
    public void WindowsPowerTelemetryDto_DefaultValues_AreSet()
    {
        // Act
        var dto = new WindowsPowerTelemetryDto();

        // Assert
        dto.ScreenBrightnessPercent.Should().BeNull();
        dto.EnergySaverStatus.Should().Be("Unknown");
        dto.PowerSupplyStatus.Should().Be("Unknown");
        dto.BatteryStatus.Should().Be("Unknown");
        dto.ActivePowerPlanName.Should().BeNull();
        dto.ActivePowerPlanGuid.Should().BeNull();
    }

    [Theory]
    [InlineData(0, 0, null, "Unknown")]
    [InlineData(12, 8, 35.5, "Good")]
    [InlineData(0, 0, null, "Poor")]
    public void GpsQualityDto_WithVariousValues_StoresCorrectly(
        int satellitesInView,
        int satellitesUsed,
        double? averageCn0,
        string qualityBand)
    {
        // Act
        var dto = new GpsQualityDto
        {
            SatellitesInView = satellitesInView,
            SatellitesUsedInFix = satellitesUsed,
            AverageCn0DbHz = averageCn0,
            QualityBand = qualityBand
        };

        // Assert
        dto.SatellitesInView.Should().Be(satellitesInView);
        dto.SatellitesUsedInFix.Should().Be(satellitesUsed);
        dto.AverageCn0DbHz.Should().Be(averageCn0);
        dto.QualityBand.Should().Be(qualityBand);
    }
}

