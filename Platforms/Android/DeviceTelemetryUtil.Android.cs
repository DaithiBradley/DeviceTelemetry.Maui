using DeviceTelemetry.Maui.Dtos;

namespace DeviceTelemetry.Maui;

// All the code in this file is only included on Android.
#if ANDROID
public static partial class DeviceTelemetryUtil
{
    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct)
    {
        // TODO: Implement Android GPS quality collection
        // This would use Android LocationManager APIs to get GPS satellite information
        return Task.FromResult<GpsQualityDto?>(null);
    }

    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct)
        => Task.FromResult<WindowsPowerTelemetryDto?>(null);
}
#endif

