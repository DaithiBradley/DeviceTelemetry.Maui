using DeviceTelemetry.Maui.Dtos;
using DeviceTelemetry.Maui.Interfaces;
using DeviceTelemetry.Maui.Services;

namespace DeviceTelemetry.Maui;

/// <summary>
/// Captures battery, location, network, and related device telemetry for .NET MAUI applications.
/// </summary>
public static partial class DeviceTelemetryUtil
{
    private static readonly IBatteryService DefaultBatteryService = new MauiBatteryService();
    private static readonly IPermissionService DefaultPermissionService = new MauiPermissionService();
    private static readonly IGeolocationService DefaultGeolocationService = new MauiGeolocationService();
    private static readonly IDeviceInfoService DefaultDeviceInfoService = new MauiDeviceInfoService();
    private static readonly IConnectivityService DefaultConnectivityService = new MauiConnectivityService();

    /// <summary>
    /// Captures device telemetry using <see cref="CaptureOptions.Default"/> (identifiers excluded).
    /// </summary>
    /// <param name="deviceId">The caller-supplied device identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A device telemetry DTO containing captured data.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
    public static Task<DeviceTelemetryDto> CaptureAsync(
        string deviceId,
        CancellationToken ct = default)
        => CaptureAsync(deviceId, CaptureOptions.Default, ct);

    /// <summary>
    /// Captures device telemetry using the supplied options.
    /// </summary>
    /// <param name="deviceId">The caller-supplied device identifier.</param>
    /// <param name="options">Capture flags and location request settings. When <see langword="null"/>, <see cref="CaptureOptions.Default"/> is used.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A device telemetry DTO containing captured data.</returns>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="ct"/> is cancelled.</exception>
    public static Task<DeviceTelemetryDto> CaptureAsync(
        string deviceId,
        CaptureOptions? options,
        CancellationToken ct = default)
        => CaptureAsync(
            deviceId,
            options ?? CaptureOptions.Default,
            DefaultBatteryService,
            DefaultPermissionService,
            DefaultGeolocationService,
            DefaultDeviceInfoService,
            DefaultConnectivityService,
            ct);

    /// <summary>
    /// Captures device telemetry with injected services (for tests).
    /// </summary>
    /// <param name="deviceId">The caller-supplied device identifier.</param>
    /// <param name="batteryService">The battery service.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="geolocationService">The geolocation service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A device telemetry DTO containing captured data.</returns>
    internal static Task<DeviceTelemetryDto> CaptureAsync(
        string deviceId,
        IBatteryService batteryService,
        IPermissionService permissionService,
        IGeolocationService geolocationService,
        CancellationToken ct = default)
        => CaptureAsync(
            deviceId,
            CaptureOptions.Default,
            batteryService,
            permissionService,
            geolocationService,
            DefaultDeviceInfoService,
            DefaultConnectivityService,
            ct);

    /// <summary>
    /// Captures device telemetry with injected services and options (for tests).
    /// </summary>
    /// <param name="deviceId">The caller-supplied device identifier.</param>
    /// <param name="options">Capture flags and location request settings.</param>
    /// <param name="batteryService">The battery service.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="geolocationService">The geolocation service.</param>
    /// <param name="deviceInfoService">The device info service.</param>
    /// <param name="connectivityService">The connectivity service.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A device telemetry DTO containing captured data.</returns>
    internal static async Task<DeviceTelemetryDto> CaptureAsync(
        string deviceId,
        CaptureOptions options,
        IBatteryService batteryService,
        IPermissionService permissionService,
        IGeolocationService geolocationService,
        IDeviceInfoService deviceInfoService,
        IConnectivityService connectivityService,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(options);

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

        if (options.IncludeLocation)
        {
            var (location, locationStatus) = await TryGetLocationAsync(
                permissionService,
                geolocationService,
                options,
                ct);
            dto.LocationStatus = locationStatus;
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

            if (locationStatus is not LocationCaptureStatus.Denied)
            {
                dto.GpsQuality = await TryGetAndroidGpsQualityAsync(ct);
            }
        }
        else
        {
            dto.LocationStatus = LocationCaptureStatus.NotRequested;
        }

        if (options.IncludeWindowsPower)
        {
            dto.WindowsPower = await TryGetWindowsPowerAsync(ct);
        }

        if (options.IncludeNetwork)
        {
            dto.Network = await TryGetNetworkTelemetryAsync(options.IncludeIdentifiers, ct);
            if (dto.Network != null && !options.IncludeIdentifiers)
            {
                dto.Network.Imei = null;
                dto.Network.Imsi = null;
                dto.Network.PhoneNumber = null;
            }
        }

        if (options.IncludeDeviceInfo)
        {
            dto.DeviceInfo = TryCreateDeviceInfo(deviceInfoService);
        }

        if (options.IncludeConnectivity)
        {
            dto.Connectivity = TryCreateConnectivity(connectivityService);
        }

        return dto;
    }

    /// <summary>
    /// Converts a battery level (0.0 to 1.0) to a percentage (0 to 100).
    /// Negative and non-finite values are treated as unknown.
    /// </summary>
    /// <param name="level">The battery level (0.0 to 1.0), or a negative value when unknown.</param>
    /// <returns>The battery level as a percentage (0 to 100), or <see langword="null"/> when unknown.</returns>
    internal static int? ToPercent(double level)
    {
        if (double.IsNaN(level) || double.IsInfinity(level) || level < 0)
        {
            return null;
        }

        int pct = (int)Math.Round(level * 100.0);
        if (pct > 100)
        {
            pct = 100;
        }

        return pct;
    }

    /// <summary>
    /// Attempts to get the current location, falling back to last known when the current fix is missing.
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
        var (location, _) = await TryGetLocationAsync(
            permissionService,
            geolocationService,
            CaptureOptions.Default,
            ct);
        return location;
    }

    /// <summary>
    /// Attempts to get the current location and a status explaining the result.
    /// </summary>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="geolocationService">The geolocation service.</param>
    /// <param name="options">Capture options that supply accuracy and timeout.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The location (when available) and a capture status.</returns>
    internal static async Task<(Location? Location, LocationCaptureStatus Status)> TryGetLocationAsync(
        IPermissionService permissionService,
        IGeolocationService geolocationService,
        CaptureOptions options,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        PermissionStatus status;
        try
        {
            status = await permissionService.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (status != PermissionStatus.Granted)
            {
                status = await RequestLocationWhenInUseAsync(permissionService);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return (null, LocationCaptureStatus.Denied);
        }

        if (status != PermissionStatus.Granted)
        {
            return (null, LocationCaptureStatus.Denied);
        }

        Location? lastKnown = null;
        var timedOut = false;

        try
        {
            var req = new GeolocationRequest(options.LocationAccuracy, options.LocationTimeout);
            Location? current = await geolocationService.GetLocationAsync(req, ct);
            if (current != null)
            {
                return (current, LocationCaptureStatus.Ok);
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
        }
        catch (TimeoutException)
        {
            timedOut = true;
        }
        catch
        {
            // Current fix failed; try last known below.
        }

        try
        {
            lastKnown = await geolocationService.GetLastKnownLocationAsync();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            lastKnown = null;
        }

        if (lastKnown != null)
        {
            return (lastKnown, timedOut ? LocationCaptureStatus.TimedOut : LocationCaptureStatus.Ok);
        }

        return (null, timedOut ? LocationCaptureStatus.TimedOut : LocationCaptureStatus.Unavailable);
    }

    /// <summary>
    /// Maps device info, returning <see langword="null"/> if the platform APIs throw.
    /// </summary>
    /// <param name="deviceInfoService">The device info service.</param>
    /// <returns>A device info DTO, or <see langword="null"/>.</returns>
    private static DeviceInfoDto? TryCreateDeviceInfo(IDeviceInfoService deviceInfoService)
    {
        try
        {
            return new DeviceInfoDto
            {
                Model = deviceInfoService.Model,
                Manufacturer = deviceInfoService.Manufacturer,
                Name = deviceInfoService.Name,
                VersionString = deviceInfoService.VersionString,
                Platform = deviceInfoService.Platform,
                Idiom = deviceInfoService.Idiom,
                DeviceType = deviceInfoService.DeviceType
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Maps connectivity, returning <see langword="null"/> if the platform APIs throw.
    /// </summary>
    /// <param name="connectivityService">The connectivity service.</param>
    /// <returns>A connectivity DTO, or <see langword="null"/>.</returns>
    private static ConnectivityDto? TryCreateConnectivity(IConnectivityService connectivityService)
    {
        try
        {
            return new ConnectivityDto
            {
                NetworkAccess = connectivityService.NetworkAccess.ToString(),
                ConnectionProfiles = [.. connectivityService.ConnectionProfiles.Select(static p => p.ToString())]
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Requests location permission, marshaling onto the main thread for the real MAUI implementation.
    /// </summary>
    /// <param name="permissionService">The permission service.</param>
    /// <returns>The permission status after the request.</returns>
    private static Task<PermissionStatus> RequestLocationWhenInUseAsync(IPermissionService permissionService)
    {
        if (permissionService is MauiPermissionService && !MainThread.IsMainThread)
        {
            return MainThread.InvokeOnMainThreadAsync(
                () => permissionService.RequestAsync<Permissions.LocationWhenInUse>());
        }

        return permissionService.RequestAsync<Permissions.LocationWhenInUse>();
    }

    /// <summary>
    /// Returns <see langword="true"/> when the DTO contains real cellular data rather than placeholders.
    /// </summary>
    /// <param name="dto">The network DTO.</param>
    /// <returns><see langword="true"/> when the DTO is worth returning to callers.</returns>
    internal static bool HasCellularTelemetry(NetworkTelemetryDto dto)
    {
        if (!string.IsNullOrWhiteSpace(dto.CarrierName)
            || !string.IsNullOrWhiteSpace(dto.MobileCountryCode)
            || !string.IsNullOrWhiteSpace(dto.MobileNetworkCode)
            || !string.IsNullOrWhiteSpace(dto.Imei)
            || !string.IsNullOrWhiteSpace(dto.Imsi))
        {
            return true;
        }

        if (IsCellularNetworkType(dto.NetworkType))
        {
            return true;
        }

        return dto.SimState is "Ready" or "PinRequired" or "PukRequired" or "NetworkLocked";
    }

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="networkType"/> is a cellular radio type.
    /// </summary>
    /// <param name="networkType">The network type string.</param>
    /// <returns><see langword="true"/> for LTE, 5G, and similar values.</returns>
    internal static bool IsCellularNetworkType(string? networkType)
        => networkType is "LTE" or "5G" or "HSPA" or "HSPA+" or "EDGE" or "GPRS" or "CDMA"
            or "EVDO 0" or "EVDO A" or "EVDO B" or "Cellular" or "3G" or "NR";

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="value"/> looks like a 15-digit IMEI.
    /// </summary>
    /// <param name="value">The candidate identifier.</param>
    /// <returns><see langword="true"/> when the value is 15 decimal digits.</returns>
    internal static bool IsLikelyImei(string? value)
        => value is { Length: 15 } && value.All(char.IsDigit);

    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct);

    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct);

    private static partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(bool includeIdentifiers, CancellationToken ct);
}
