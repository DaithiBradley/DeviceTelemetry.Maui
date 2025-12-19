namespace DeviceTelemetry.Maui.Services;

/// <summary>
/// MAUI implementation of the battery service interface.
/// </summary>
public sealed class MauiBatteryService : Interfaces.IBatteryService
{
    /// <inheritdoc />
    public double ChargeLevel => Battery.ChargeLevel;

    /// <inheritdoc />
    public BatteryState State => Battery.State;

    /// <inheritdoc />
    public BatteryPowerSource PowerSource => Battery.PowerSource;
}

