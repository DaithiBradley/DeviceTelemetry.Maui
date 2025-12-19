using DeviceTelemetry.Maui.Dtos;
using DeviceTelemetry.Maui.Interfaces;
using DeviceTelemetry.Maui.Services;

namespace DeviceTelemetry.Maui;

/// <summary>
/// Utility class for capturing device telemetry data.
/// </summary>
public static partial class DeviceTelemetryUtil
{
    private static readonly IBatteryService DefaultBatteryService = new MauiBatteryService();
    private static readonly IPermissionService DefaultPermissionService = new MauiPermissionService();
    private static readonly IGeolocationService DefaultGeolocationService = new MauiGeolocationService();

    /// <summary>
    /// Captures device telemetry data asynchronously.
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A device telemetry DTO containing captured data.</returns>
    public static async Task<DeviceTelemetryDto> CaptureAsync(
        string deviceId,
        CancellationToken ct = default)
    {
        return await CaptureAsync(
            deviceId,
            DefaultBatteryService,
            DefaultPermissionService,
            DefaultGeolocationService,
            ct);
    }

    /// <summary>
    /// Captures device telemetry data asynchronously with custom services (for testing).
    /// </summary>
    /// <param name="deviceId">The device identifier.</param>
    /// <param name="batteryService">The battery service.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="geolocationService">The geolocation service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A device telemetry DTO containing captured data.</returns>
    internal static async Task<DeviceTelemetryDto> CaptureAsync(
        string deviceId,
        IBatteryService batteryService,
        IPermissionService permissionService,
        IGeolocationService geolocationService,
        CancellationToken ct = default)
    {
        var dto = new DeviceTelemetryDto
        {
            DeviceId = deviceId,
            CapturedAtUtc = DateTimeOffset.UtcNow,
            Battery = new BatteryDto
            {
                LevelPercent = ToPercent(batteryService.ChargeLevel),
                State = batteryService.State.ToString(),
                PowerSource = batteryService.PowerSource.ToString()
            }
        };

        Location? location = await TryGetLocationAsync(permissionService, geolocationService, ct);
        if (location != null)
        {
            dto.Location = new GeoFixDto
            {
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                AccuracyMeters = location.Accuracy,
                AltitudeMeters = location.Altitude,
                SpeedMetersPerSecond = location.Speed,
                CourseDegrees = location.Course,
                FixTimestampUtc = location.Timestamp
            };
        }

        dto.GpsQuality = await TryGetAndroidGpsQualityAsync(ct);
        dto.WindowsPower = await TryGetWindowsPowerAsync(ct);
        dto.Network = await TryGetNetworkTelemetryAsync(ct);

        return dto;
    }

    /// <summary>
    /// Converts a battery level (0.0 to 1.0) to a percentage (0 to 100).
    /// </summary>
    /// <param name="level">The battery level (0.0 to 1.0).</param>
    /// <returns>The battery level as a percentage (0 to 100).</returns>
    internal static int ToPercent(double level)
    {
        int pct = (int)Math.Round(level * 100.0);
        if (pct < 0) { pct = 0; }
        if (pct > 100) { pct = 100; }
        return pct;
    }

    /// <summary>
    /// Attempts to get the current location asynchronously.
    /// </summary>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="geolocationService">The geolocation service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The location if available, otherwise null.</returns>
    internal static async Task<Location?> TryGetLocationAsync(
        IPermissionService permissionService,
        IGeolocationService geolocationService,
        CancellationToken ct)
    {
        PermissionStatus status = await permissionService.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status != PermissionStatus.Granted)
        {
            status = await permissionService.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (status != PermissionStatus.Granted)
        {
            return null;
        }

        try
        {
            GeolocationRequest req =
                new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

            Location? loc = await geolocationService.GetLocationAsync(req, ct);
            return loc ?? await geolocationService.GetLastKnownLocationAsync();
        }
        catch
        {
            return null;
        }
    }

    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct);
    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct);
    private static partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(CancellationToken ct);
}
