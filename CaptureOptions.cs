namespace DeviceTelemetry.Maui;

/// <summary>
/// Options that control which telemetry is collected by <see cref="DeviceTelemetryUtil.CaptureAsync(string, CaptureOptions, CancellationToken)"/>.
/// </summary>
public sealed class CaptureOptions
{
    /// <summary>
    /// Gets a shared instance with default values. Location, network, device info, connectivity, and Windows power are included; device identifiers are not.
    /// </summary>
    public static CaptureOptions Default { get; } = new();

    /// <summary>
    /// Gets a value indicating whether location and Android GPS quality should be collected. The default is <see langword="true"/>.
    /// </summary>
    public bool IncludeLocation { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether network and SIM telemetry should be collected. The default is <see langword="true"/>.
    /// </summary>
    public bool IncludeNetwork { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether IMEI, IMSI, and phone number should be collected when the platform and permissions allow it. The default is <see langword="false"/>.
    /// </summary>
    public bool IncludeIdentifiers { get; init; }

    /// <summary>
    /// Gets a value indicating whether device model, manufacturer, OS, and idiom should be collected. The default is <see langword="true"/>.
    /// </summary>
    public bool IncludeDeviceInfo { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether network access and connection profiles should be collected. The default is <see langword="true"/>.
    /// </summary>
    public bool IncludeConnectivity { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether Windows power telemetry should be collected on Windows. The default is <see langword="true"/>.
    /// </summary>
    public bool IncludeWindowsPower { get; init; } = true;

    /// <summary>
    /// Gets the timeout used when requesting the current location. The default is 30 seconds.
    /// </summary>
    public TimeSpan LocationTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets the desired location accuracy. The default is <see cref="GeolocationAccuracy.Medium"/>.
    /// </summary>
    public GeolocationAccuracy LocationAccuracy { get; init; } = GeolocationAccuracy.Medium;
}
