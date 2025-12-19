namespace DeviceTelemetry.Maui.Interfaces;

/// <summary>
/// Interface for battery information service.
/// </summary>
public interface IBatteryService
{
    /// <summary>
    /// Gets the current battery charge level (0.0 to 1.0).
    /// </summary>
    double ChargeLevel { get; }

    /// <summary>
    /// Gets the current battery state.
    /// </summary>
    BatteryState State { get; }

    /// <summary>
    /// Gets the current power source.
    /// </summary>
    BatteryPowerSource PowerSource { get; }
}

