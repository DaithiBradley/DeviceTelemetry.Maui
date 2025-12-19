using DeviceTelemetry.Maui.Dtos;

namespace DeviceTelemetry.Maui;

// All the code in this file is only included on iOS.
#if IOS
public static partial class DeviceTelemetryUtil
{
    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct)
        => Task.FromResult<GpsQualityDto?>(null);

    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct)
        => Task.FromResult<WindowsPowerTelemetryDto?>(null);

    /// <summary>
    /// Collects network and SIM card telemetry data for iOS devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Network telemetry data, or null if unavailable.</returns>
    private static partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(CancellationToken ct)
    {
        try
        {
            // TODO: Implement iOS network/SIM telemetry collection
            // This would use CoreTelephony APIs to get:
            // - Carrier name
            // - Mobile country code (MCC)
            // - Mobile network code (MNC)
            // - Network type
            // - Signal strength (limited on iOS)
            // Note: IMEI and IMSI are restricted on iOS and may not be accessible
            return Task.FromResult<NetworkTelemetryDto?>(null);
        }
        catch
        {
            return Task.FromResult<NetworkTelemetryDto?>(null);
        }
    }
}
#endif

