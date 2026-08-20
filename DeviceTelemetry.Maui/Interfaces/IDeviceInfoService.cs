namespace DeviceTelemetry.Maui.Interfaces;

/// <summary>
/// Abstraction over MAUI <c>DeviceInfo</c> so telemetry capture can be tested without a live device.
/// </summary>
public interface IDeviceInfoService
{
    /// <summary>
    /// Gets the device model name.
    /// </summary>
    string Model { get; }

    /// <summary>
    /// Gets the device manufacturer.
    /// </summary>
    string Manufacturer { get; }

    /// <summary>
    /// Gets the user-facing device name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the operating system version string.
    /// </summary>
    string VersionString { get; }

    /// <summary>
    /// Gets the platform name (for example Android, iOS, WinUI).
    /// </summary>
    string Platform { get; }

    /// <summary>
    /// Gets the device idiom (for example Phone, Desktop, Tablet).
    /// </summary>
    string Idiom { get; }

    /// <summary>
    /// Gets the device type (Physical or Virtual).
    /// </summary>
    string DeviceType { get; }
}
