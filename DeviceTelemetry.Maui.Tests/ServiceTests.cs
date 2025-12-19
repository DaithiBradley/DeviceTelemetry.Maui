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
    public void MauiBatteryService_Properties_AccessMAUIBattery()
    {
        // Arrange
        var service = new MauiBatteryService();

        // Act & Assert
        // These will access the actual MAUI Battery class
        // In a real test environment, we'd verify the values are within expected ranges
        var chargeLevel = service.ChargeLevel;
        chargeLevel.Should().BeInRange(0.0, 1.0);

        var state = service.State;
        state.Should().BeDefined();

        var powerSource = service.PowerSource;
        powerSource.Should().BeDefined();
    }
}

