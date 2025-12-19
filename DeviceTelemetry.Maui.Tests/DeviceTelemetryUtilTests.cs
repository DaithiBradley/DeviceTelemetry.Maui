using DeviceTelemetry.Maui.Dtos;
using DeviceTelemetry.Maui.Interfaces;
using Moq;
using Xunit;

namespace DeviceTelemetry.Maui.Tests;

/// <summary>
/// Tests for DeviceTelemetryUtil class.
/// </summary>
public sealed class DeviceTelemetryUtilTests
{
    [Fact]
    public async Task CaptureAsync_WithValidDeviceId_ReturnsDtoWithDeviceId()
    {
        // Arrange
        var deviceId = "test-device-123";
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        result.Should().NotBeNull();
        result.DeviceId.Should().Be(deviceId);
        result.CapturedAtUtc.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CaptureAsync_WithBatteryService_PopulatesBatteryDto()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService(0.75, BatteryState.Charging, BatteryPowerSource.Usb);
        var permissionService = CreateMockPermissionService(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        result.Battery.Should().NotBeNull();
        result.Battery.LevelPercent.Should().Be(75);
        result.Battery.State.Should().Be("Charging");
        result.Battery.PowerSource.Should().Be("Usb");
    }

    [Fact]
    public async Task CaptureAsync_WithLocationPermissionGranted_IncludesLocation()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Granted);
        var location = new Location(37.7749, -122.4194)
        {
            Accuracy = 10.5,
            Altitude = 100.0,
            Speed = 5.2,
            Course = 90.0,
            Timestamp = DateTimeOffset.UtcNow
        };
        var geolocationService = CreateMockGeolocationService(location);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        result.Location.Should().NotBeNull();
        result.Location!.Latitude.Should().Be(37.7749);
        result.Location.Longitude.Should().Be(-122.4194);
        result.Location.AccuracyMeters.Should().Be(10.5);
        result.Location.AltitudeMeters.Should().Be(100.0);
        result.Location.SpeedMetersPerSecond.Should().Be(5.2);
        result.Location.CourseDegrees.Should().Be(90.0);
        result.Location.FixTimestampUtc.Should().Be(location.Timestamp);
    }

    [Fact]
    public async Task CaptureAsync_WithLocationPermissionDenied_ExcludesLocation()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        result.Location.Should().BeNull();
    }

    [Fact]
    public async Task CaptureAsync_WithLocationPermissionNotGranted_RequestsPermission()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService();
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Unknown);
        permissionService
            .Setup(x => x.RequestAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        permissionService.Verify(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>(), Times.Once);
        permissionService.Verify(x => x.RequestAsync<Permissions.LocationWhenInUse>(), Times.Once);
        result.Location.Should().BeNull();
    }

    [Fact]
    public async Task CaptureAsync_WithGeolocationException_ReturnsNullLocation()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Granted);
        var geolocationService = new Mock<IGeolocationService>();
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("GPS unavailable"));
        geolocationService
            .Setup(x => x.GetLastKnownLocationAsync())
            .ReturnsAsync((Location?)null);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        result.Location.Should().BeNull();
    }

    [Fact]
    public async Task CaptureAsync_WithGeolocationFailure_FallsBackToLastKnownLocation()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Granted);
        var lastKnownLocation = new Location(37.7749, -122.4194)
        {
            Timestamp = DateTimeOffset.UtcNow.AddMinutes(-5)
        };
        var geolocationService = new Mock<IGeolocationService>();
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);
        geolocationService
            .Setup(x => x.GetLastKnownLocationAsync())
            .ReturnsAsync(lastKnownLocation);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        result.Location.Should().NotBeNull();
        result.Location!.Latitude.Should().Be(37.7749);
        result.Location.Longitude.Should().Be(-122.4194);
    }

    [Fact]
    public async Task CaptureAsync_WithCancellationToken_CancelsOperation()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Granted);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var geolocationService = CreateMockGeolocationService(null);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object,
            cts.Token);

        // Assert
        result.Should().NotBeNull();
        result.DeviceId.Should().Be(deviceId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("device-123")]
    [InlineData("very-long-device-id-with-special-chars-!@#$%")]
    public async Task CaptureAsync_WithVariousDeviceIds_HandlesCorrectly(string deviceId)
    {
        // Arrange
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);

        // Act
        var result = await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        // Assert
        result.DeviceId.Should().Be(deviceId);
    }

    private static Mock<IBatteryService> CreateMockBatteryService(
        double chargeLevel = 0.5,
        BatteryState state = BatteryState.Unknown,
        BatteryPowerSource powerSource = BatteryPowerSource.Unknown)
    {
        var mock = new Mock<IBatteryService>();
        mock.Setup(x => x.ChargeLevel).Returns(chargeLevel);
        mock.Setup(x => x.State).Returns(state);
        mock.Setup(x => x.PowerSource).Returns(powerSource);
        return mock;
    }

    private static Mock<IPermissionService> CreateMockPermissionService(PermissionStatus status)
    {
        var mock = new Mock<IPermissionService>();
        mock.Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(status);
        mock.Setup(x => x.RequestAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(status);
        return mock;
    }

    private static Mock<IGeolocationService> CreateMockGeolocationService(Location? location)
    {
        var mock = new Mock<IGeolocationService>();
        mock.Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);
        mock.Setup(x => x.GetLastKnownLocationAsync())
            .ReturnsAsync(location);
        return mock;
    }
}

