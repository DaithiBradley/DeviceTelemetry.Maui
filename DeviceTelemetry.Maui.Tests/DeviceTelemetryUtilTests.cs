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
        result.LocationStatus.Should().Be(LocationCaptureStatus.Ok);
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
        result.LocationStatus.Should().Be(LocationCaptureStatus.Denied);
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
        result.LocationStatus.Should().Be(LocationCaptureStatus.Denied);
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
        result.LocationStatus.Should().Be(LocationCaptureStatus.Unavailable);
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
        result.LocationStatus.Should().Be(LocationCaptureStatus.Ok);
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
        var act = async () => await DeviceTelemetryUtil.CaptureAsync(
            deviceId,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object,
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
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

    [Fact]
    public async Task CaptureAsync_WhenIncludeLocationIsFalse_SkipsPermissionAndLocation()
    {
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Granted);
        var geolocationService = CreateMockGeolocationService(new Location(1, 2));
        var options = new CaptureOptions { IncludeLocation = false };

        var result = await DeviceTelemetryUtil.CaptureAsync(
            "device-1",
            options,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object,
            CreateMockDeviceInfoService().Object,
            CreateMockConnectivityService().Object);

        result.Location.Should().BeNull();
        result.LocationStatus.Should().Be(LocationCaptureStatus.NotRequested);
        result.GpsQuality.Should().BeNull();
        permissionService.Verify(
            x => x.CheckStatusAsync<Permissions.LocationWhenInUse>(),
            Times.Never);
        geolocationService.Verify(
            x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CaptureAsync_WithCustomLocationAccuracy_PassesOptionsToGeolocation()
    {
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Granted);
        var geolocationService = new Mock<IGeolocationService>();
        GeolocationRequest? captured = null;
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GeolocationRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new Location(1, 2));
        geolocationService
            .Setup(x => x.GetLastKnownLocationAsync())
            .ReturnsAsync((Location?)null);

        var options = new CaptureOptions
        {
            LocationAccuracy = GeolocationAccuracy.Low,
            LocationTimeout = TimeSpan.FromSeconds(5)
        };

        await DeviceTelemetryUtil.CaptureAsync(
            "device-1",
            options,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object,
            CreateMockDeviceInfoService().Object,
            CreateMockConnectivityService().Object);

        captured.Should().NotBeNull();
        captured!.DesiredAccuracy.Should().Be(GeolocationAccuracy.Low);
        captured.Timeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task CaptureAsync_DefaultOptions_DoesNotPopulateIdentifiers()
    {
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);

        var result = await DeviceTelemetryUtil.CaptureAsync(
            "device-1",
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        result.Network?.Imei.Should().BeNull();
        result.Network?.Imsi.Should().BeNull();
        result.Network?.PhoneNumber.Should().BeNull();
    }

    [Fact]
    public async Task CaptureAsync_WithDeviceInfoAndConnectivityServices_PopulatesDtos()
    {
        var batteryService = CreateMockBatteryService();
        var permissionService = CreateMockPermissionService(PermissionStatus.Denied);
        var geolocationService = CreateMockGeolocationService(null);
        var deviceInfo = CreateMockDeviceInfoService();
        var connectivity = CreateMockConnectivityService();
        var options = new CaptureOptions { IncludeLocation = false, IncludeNetwork = false, IncludeWindowsPower = false };

        var result = await DeviceTelemetryUtil.CaptureAsync(
            "device-1",
            options,
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object,
            deviceInfo.Object,
            connectivity.Object);

        result.DeviceInfo.Should().NotBeNull();
        result.DeviceInfo!.Model.Should().Be("Pixel");
        result.DeviceInfo.Platform.Should().Be("Android");
        result.Connectivity.Should().NotBeNull();
        result.Connectivity!.NetworkAccess.Should().Be("Internet");
        result.Connectivity.ConnectionProfiles.Should().Equal("WiFi");
    }

    [Fact]
    public async Task CaptureAsync_WhenPermissionCheckThrows_ReturnsDeniedWithoutThrowing()
    {
        var batteryService = CreateMockBatteryService();
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ThrowsAsync(new InvalidOperationException("permission APIs unavailable"));
        var geolocationService = CreateMockGeolocationService(null);

        var result = await DeviceTelemetryUtil.CaptureAsync(
            "device-1",
            batteryService.Object,
            permissionService.Object,
            geolocationService.Object);

        result.Location.Should().BeNull();
        result.LocationStatus.Should().Be(LocationCaptureStatus.Denied);
        result.Battery.LevelPercent.Should().Be(50);
        geolocationService.Verify(
            x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void HasCellularTelemetry_IgnoresWifiAndUnknownPlaceholders()
    {
        DeviceTelemetryUtil.HasCellularTelemetry(new NetworkTelemetryDto { NetworkType = "WiFi" })
            .Should().BeFalse();
        DeviceTelemetryUtil.HasCellularTelemetry(new NetworkTelemetryDto { NetworkType = "Type_6" })
            .Should().BeFalse();
        DeviceTelemetryUtil.HasCellularTelemetry(new NetworkTelemetryDto { NetworkType = "Unknown" })
            .Should().BeFalse();
        DeviceTelemetryUtil.HasCellularTelemetry(new NetworkTelemetryDto { CarrierName = " " })
            .Should().BeFalse();
        DeviceTelemetryUtil.HasCellularTelemetry(new NetworkTelemetryDto { NetworkType = "LTE" })
            .Should().BeTrue();
        DeviceTelemetryUtil.HasCellularTelemetry(new NetworkTelemetryDto { CarrierName = "Vodafone" })
            .Should().BeTrue();
    }

    [Fact]
    public void IsLikelyImei_RequiresFifteenDigits()
    {
        DeviceTelemetryUtil.IsLikelyImei("123456789012345").Should().BeTrue();
        DeviceTelemetryUtil.IsLikelyImei("COM3").Should().BeFalse();
        DeviceTelemetryUtil.IsLikelyImei("PCI\\VEN_8086").Should().BeFalse();
        DeviceTelemetryUtil.IsLikelyImei("12345678901234").Should().BeFalse();
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

    private static Mock<IDeviceInfoService> CreateMockDeviceInfoService()
    {
        var mock = new Mock<IDeviceInfoService>();
        mock.Setup(x => x.Model).Returns("Pixel");
        mock.Setup(x => x.Manufacturer).Returns("Google");
        mock.Setup(x => x.Name).Returns("Test Phone");
        mock.Setup(x => x.VersionString).Returns("16");
        mock.Setup(x => x.Platform).Returns("Android");
        mock.Setup(x => x.Idiom).Returns("Phone");
        mock.Setup(x => x.DeviceType).Returns("Physical");
        return mock;
    }

    private static Mock<IConnectivityService> CreateMockConnectivityService()
    {
        var mock = new Mock<IConnectivityService>();
        mock.Setup(x => x.NetworkAccess).Returns(NetworkAccess.Internet);
        mock.Setup(x => x.ConnectionProfiles).Returns([ConnectionProfile.WiFi]);
        return mock;
    }
}

