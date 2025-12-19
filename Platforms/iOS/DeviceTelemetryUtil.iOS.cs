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
}
#endif

