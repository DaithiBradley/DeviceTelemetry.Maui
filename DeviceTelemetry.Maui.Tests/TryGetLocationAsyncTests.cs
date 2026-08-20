using DeviceTelemetry.Maui.Interfaces;
using Moq;
using Xunit;

namespace DeviceTelemetry.Maui.Tests;

/// <summary>
/// Tests for the TryGetLocationAsync method.
/// </summary>
public sealed class TryGetLocationAsyncTests
{
    [Fact]
    public async Task TryGetLocationAsync_WithPermissionGranted_ReturnsLocation()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Granted);

        var location = new Location(37.7749, -122.4194);
        var geolocationService = new Mock<IGeolocationService>();
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        // Act
        var result = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Latitude.Should().Be(37.7749);
        result.Longitude.Should().Be(-122.4194);
    }

    [Fact]
    public async Task TryGetLocationAsync_WithPermissionDenied_ReturnsNull()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Denied);

        var geolocationService = new Mock<IGeolocationService>();

        // Act
        var result = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
        geolocationService.Verify(
            x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryGetLocationAsync_WithPermissionUnknown_RequestsPermission()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Unknown);
        permissionService
            .Setup(x => x.RequestAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Denied);

        var geolocationService = new Mock<IGeolocationService>();

        // Act
        var result = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
        permissionService.Verify(x => x.RequestAsync<Permissions.LocationWhenInUse>(), Times.Once);
    }

    [Fact]
    public async Task TryGetLocationAsync_WithPermissionRequestGranted_ReturnsLocation()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Unknown);
        permissionService
            .Setup(x => x.RequestAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Granted);

        var location = new Location(37.7749, -122.4194);
        var geolocationService = new Mock<IGeolocationService>();
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(location);

        // Act
        var result = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        permissionService.Verify(x => x.RequestAsync<Permissions.LocationWhenInUse>(), Times.Once);
    }

    [Fact]
    public async Task TryGetLocationAsync_WithGeolocationException_ReturnsNull()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Granted);

        var geolocationService = new Mock<IGeolocationService>();
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("GPS error"));
        geolocationService
            .Setup(x => x.GetLastKnownLocationAsync())
            .ReturnsAsync((Location?)null);

        // Act
        var result = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task TryGetLocationAsync_WithGeolocationNull_FallsBackToLastKnown()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Granted);

        var lastKnownLocation = new Location(37.7749, -122.4194);
        var geolocationService = new Mock<IGeolocationService>();
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);
        geolocationService
            .Setup(x => x.GetLastKnownLocationAsync())
            .ReturnsAsync(lastKnownLocation);

        // Act
        var result = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Latitude.Should().Be(37.7749);
        result.Longitude.Should().Be(-122.4194);
    }

    [Fact]
    public async Task TryGetLocationAsync_WithCancellationToken_PassesToGeolocation()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Granted);

        var location = new Location(37.7749, -122.4194);
        var geolocationService = new Mock<IGeolocationService>();
        var cts = new CancellationTokenSource();

        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), cts.Token))
            .ReturnsAsync(location);

        // Act
        var result = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            cts.Token);

        // Assert
        result.Should().NotBeNull();
        geolocationService.Verify(
            x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task TryGetLocationAsync_CreatesGeolocationRequestWithCorrectParameters()
    {
        // Arrange
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Granted);

        var location = new Location(37.7749, -122.4194);
        var geolocationService = new Mock<IGeolocationService>();
        GeolocationRequest? capturedRequest = null;

        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GeolocationRequest, CancellationToken>((req, ct) => capturedRequest = req)
            .ReturnsAsync(location);

        // Act
        await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            CancellationToken.None);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.DesiredAccuracy.Should().Be(GeolocationAccuracy.Medium);
        capturedRequest.Timeout.Should().Be(TimeSpan.FromSeconds(30));
    }
}

