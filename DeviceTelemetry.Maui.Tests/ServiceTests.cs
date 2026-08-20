using DeviceTelemetry.Maui.Services;
using Xunit;

namespace DeviceTelemetry.Maui.Tests;

/// <summary>
/// Tests for service implementations.
/// </summary>
public sealed class ServiceTests
{
    [Fact]
    public void MauiBatteryService_ImplementsIBatteryService()
    {
        // Act
        var service = new MauiBatteryService();

        // Assert
        service.Should().BeAssignableTo<Interfaces.IBatteryService>();
    }

    [Fact]
    public void MauiPermissionService_ImplementsIPermissionService()
    {
        // Act
        var service = new MauiPermissionService();

        // Assert
        service.Should().BeAssignableTo<Interfaces.IPermissionService>();
    }

    [Fact]
    public void MauiGeolocationService_ImplementsIGeolocationService()
    {
        // Act
        var service = new MauiGeolocationService();

        // Assert
        service.Should().BeAssignableTo<Interfaces.IGeolocationService>();
    }

    [Fact]
    public void MauiDeviceInfoService_ImplementsIDeviceInfoService()
    {
        var service = new MauiDeviceInfoService();
        service.Should().BeAssignableTo<Interfaces.IDeviceInfoService>();
    }

    [Fact]
    public void MauiConnectivityService_ImplementsIConnectivityService()
    {
        var service = new MauiConnectivityService();
        service.Should().BeAssignableTo<Interfaces.IConnectivityService>();
    }
}

