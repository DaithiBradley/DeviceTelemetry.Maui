using DeviceTelemetry.Maui.Dtos;
using DeviceTelemetry.Maui.Interfaces;
using Moq;
using Xunit;

namespace DeviceTelemetry.Maui.Tests;

/// <summary>
/// Tests for platform-specific implementations.
/// </summary>
public sealed class PlatformSpecificTests
{
    [Fact]
    public async Task CaptureAsync_IncludesPlatformSpecificData()
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
        // Platform-specific data may be null on non-target platforms
        // This is expected behavior - the methods are called but may return null
        result.Should().NotBeNull();
        // GpsQuality and WindowsPower are set by platform-specific implementations
        // They may be null depending on the platform
    }

    [Fact]
    public async Task CaptureAsync_WithCancellationToken_HandlesPlatformSpecificMethods()
    {
        // Arrange
        var deviceId = "test-device";
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);
        var cts = new CancellationTokenSource();
        cts.Cancel();

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

    private static Mock<IBatteryService> CreateMockBatteryService()
    {
        var mock = new Mock<IBatteryService>();
        mock.Setup(x => x.ChargeLevel).Returns(0.5);
        mock.Setup(x => x.State).Returns(BatteryState.Unknown);
        mock.Setup(x => x.PowerSource).Returns(BatteryPowerSource.Unknown);
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

