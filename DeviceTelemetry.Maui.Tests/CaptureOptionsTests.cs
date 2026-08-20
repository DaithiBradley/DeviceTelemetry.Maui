using DeviceTelemetry.Maui.Interfaces;
using Moq;

namespace DeviceTelemetry.Maui.Tests;

/// <summary>
/// Tests for <see cref="CaptureOptions"/> defaults and option-driven location requests.
/// </summary>
public sealed class CaptureOptionsTests
{
    [Fact]
    public void Default_ExcludesIdentifiersAndIncludesSafeSurfaces()
    {
        var options = CaptureOptions.Default;

        options.IncludeLocation.Should().BeTrue();
        options.IncludeNetwork.Should().BeTrue();
        options.IncludeDeviceInfo.Should().BeTrue();
        options.IncludeConnectivity.Should().BeTrue();
        options.IncludeWindowsPower.Should().BeTrue();
        options.IncludeIdentifiers.Should().BeFalse();
        options.LocationAccuracy.Should().Be(GeolocationAccuracy.Medium);
        options.LocationTimeout.Should().Be(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task TryGetLocationAsync_WithOptions_UsesRequestedAccuracy()
    {
        var permissionService = new Mock<IPermissionService>();
        permissionService
            .Setup(x => x.CheckStatusAsync<Permissions.LocationWhenInUse>())
            .ReturnsAsync(PermissionStatus.Granted);

        GeolocationRequest? captured = null;
        var geolocationService = new Mock<IGeolocationService>();
        geolocationService
            .Setup(x => x.GetLocationAsync(It.IsAny<GeolocationRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GeolocationRequest, CancellationToken>((req, _) => captured = req)
            .ReturnsAsync(new Location(1, 2));

        var options = new CaptureOptions
        {
            LocationAccuracy = GeolocationAccuracy.Best,
            LocationTimeout = TimeSpan.FromSeconds(8)
        };

        var (location, status) = await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            options,
            CancellationToken.None);

        location.Should().NotBeNull();
        status.Should().Be(LocationCaptureStatus.Ok);
        captured!.DesiredAccuracy.Should().Be(GeolocationAccuracy.Best);
        captured.Timeout.Should().Be(TimeSpan.FromSeconds(8));
    }

    [Fact]
    public async Task TryGetLocationAsync_WhenCancelled_Throws()
    {
        var permissionService = new Mock<IPermissionService>();
        var geolocationService = new Mock<IGeolocationService>();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = async () => await DeviceTelemetryUtil.TryGetLocationAsync(
            permissionService.Object,
            geolocationService.Object,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
