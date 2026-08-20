namespace DeviceTelemetry.Maui.Dtos;

/// <summary>
/// Root payload returned by <see cref="DeviceTelemetryUtil.CaptureAsync(string, CancellationToken)"/>.
/// </summary>
public sealed class DeviceTelemetryDto
{
    /// <summary>
    /// Gets or sets the caller-supplied device identifier.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC timestamp when capture started.
    /// </summary>
    public DateTimeOffset CapturedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the location fix, or <see langword="null"/> when location was skipped, denied, or unavailable.
    /// </summary>
    public GeoFixDto? Location { get; set; }

    /// <summary>
    /// Gets or sets why location is present or missing.
    /// </summary>
    public LocationCaptureStatus LocationStatus { get; set; } = LocationCaptureStatus.NotRequested;

    /// <summary>
    /// Gets or sets battery charge, state, and power source.
    /// </summary>
    public BatteryDto Battery { get; set; } = new();

    /// <summary>
    /// Gets or sets Android GNSS quality, or <see langword="null"/> on other platforms or when unavailable.
    /// </summary>
    public GpsQualityDto? GpsQuality { get; set; }

    /// <summary>
    /// Gets or sets Windows power telemetry, or <see langword="null"/> on other platforms or when unavailable.
    /// </summary>
    public WindowsPowerTelemetryDto? WindowsPower { get; set; }

    /// <summary>
    /// Gets or sets network and SIM telemetry, or <see langword="null"/> when unavailable or skipped.
    /// </summary>
    public NetworkTelemetryDto? Network { get; set; }

    /// <summary>
    /// Gets or sets device model and OS information, or <see langword="null"/> when skipped or unavailable.
    /// </summary>
    public DeviceInfoDto? DeviceInfo { get; set; }

    /// <summary>
    /// Gets or sets connectivity access and profiles, or <see langword="null"/> when skipped or unavailable.
    /// </summary>
    public ConnectivityDto? Connectivity { get; set; }
}

/// <summary>
/// Geographic fix captured from MAUI geolocation.
/// </summary>
public sealed class GeoFixDto
{
    /// <summary>
    /// Gets or sets the latitude in decimal degrees.
    /// </summary>
    public double Latitude { get; set; }

    /// <summary>
    /// Gets or sets the longitude in decimal degrees.
    /// </summary>
    public double Longitude { get; set; }

    /// <summary>
    /// Gets or sets the horizontal accuracy in meters, when provided by the platform.
    /// </summary>
    public double? AccuracyMeters { get; set; }

    /// <summary>
    /// Gets or sets the altitude in meters, when provided by the platform.
    /// </summary>
    public double? AltitudeMeters { get; set; }

    /// <summary>
    /// Gets or sets the speed in meters per second, when provided by the platform.
    /// </summary>
    public double? SpeedMetersPerSecond { get; set; }

    /// <summary>
    /// Gets or sets the course in degrees, when provided by the platform.
    /// </summary>
    public double? CourseDegrees { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of the fix, when provided by the platform.
    /// </summary>
    public DateTimeOffset? FixTimestampUtc { get; set; }
}

/// <summary>
/// Battery charge information from MAUI <c>Battery</c>.
/// </summary>
public sealed class BatteryDto
{
    /// <summary>
    /// Gets or sets the charge level from 0 to 100, or <see langword="null"/> when the platform reports an unknown level.
    /// </summary>
    public int? LevelPercent { get; set; }

    /// <summary>
    /// Gets or sets the battery state (Charging, Discharging, Full, and so on).
    /// </summary>
    public string State { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the power source (AC, Usb, Wireless, Battery, Unknown).
    /// </summary>
    public string PowerSource { get; set; } = "Unknown";
}

/// <summary>
/// Android GNSS satellite quality. Always <see langword="null"/> on non-Android platforms.
/// </summary>
public sealed class GpsQualityDto
{
    /// <summary>
    /// Gets or sets the number of satellites in view.
    /// </summary>
    public int SatellitesInView { get; set; }

    /// <summary>
    /// Gets or sets the number of satellites used in the current fix.
    /// </summary>
    public int SatellitesUsedInFix { get; set; }

    /// <summary>
    /// Gets or sets the average carrier-to-noise density in dB-Hz, when satellites report C/N0.
    /// </summary>
    public double? AverageCn0DbHz { get; set; }

    /// <summary>
    /// Gets or sets a coarse quality band (High, Medium, Low, or Unknown).
    /// </summary>
    public string QualityBand { get; set; } = "Unknown";
}

/// <summary>
/// Windows power and brightness information. Always <see langword="null"/> on non-Windows platforms.
/// </summary>
public sealed class WindowsPowerTelemetryDto
{
    /// <summary>
    /// Gets or sets the screen brightness percentage from 0 to 100, when available.
    /// </summary>
    public int? ScreenBrightnessPercent { get; set; }

    /// <summary>
    /// Gets or sets the energy saver status (On, Off, or Unknown).
    /// </summary>
    public string EnergySaverStatus { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the power supply status (Adequate, Inadequate, NotPresent, or Unknown).
    /// </summary>
    public string PowerSupplyStatus { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the battery status (Charging, Discharging, Idle, NotPresent, or Unknown).
    /// </summary>
    public string BatteryStatus { get; set; } = "Unknown";

    /// <summary>
    /// Gets or sets the friendly name of the active Windows power plan, when available.
    /// </summary>
    public string? ActivePowerPlanName { get; set; }

    /// <summary>
    /// Gets or sets the GUID of the active Windows power plan, when available.
    /// </summary>
    public Guid? ActivePowerPlanGuid { get; set; }
}

/// <summary>
/// Cellular and network identity. IMEI, IMSI, and phone number are populated only when <see cref="CaptureOptions.IncludeIdentifiers"/> is <see langword="true"/>.
/// </summary>
public sealed class NetworkTelemetryDto
{
    /// <summary>
    /// Gets or sets the carrier or operator name.
    /// </summary>
    public string? CarrierName { get; set; }

    /// <summary>
    /// Gets or sets the mobile country code.
    /// </summary>
    public string? MobileCountryCode { get; set; }

    /// <summary>
    /// Gets or sets the mobile network code.
    /// </summary>
    public string? MobileNetworkCode { get; set; }

    /// <summary>
    /// Gets or sets the radio or interface type (LTE, 5G, WiFi, Ethernet, and so on).
    /// </summary>
    public string? NetworkType { get; set; }

    /// <summary>
    /// Gets or sets the signal strength value. Units depend on <see cref="SignalStrengthUnit"/>.
    /// </summary>
    public int? SignalStrength { get; set; }

    /// <summary>
    /// Gets or sets the unit for <see cref="SignalStrength"/> (for example Level).
    /// </summary>
    public string? SignalStrengthUnit { get; set; }

    /// <summary>
    /// Gets or sets the IMEI when identifiers are requested and the platform allows it.
    /// </summary>
    public string? Imei { get; set; }

    /// <summary>
    /// Gets or sets the IMSI when identifiers are requested and the platform allows it.
    /// </summary>
    public string? Imsi { get; set; }

    /// <summary>
    /// Gets or sets the phone number when identifiers are requested and the platform allows it.
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Gets or sets whether the device is roaming, when the platform reports it.
    /// </summary>
    public bool? IsRoaming { get; set; }

    /// <summary>
    /// Gets or sets the SIM state (Ready, Absent, and so on), when the platform reports it.
    /// </summary>
    public string? SimState { get; set; }
}

/// <summary>
/// Device model and operating system information from MAUI <c>DeviceInfo</c>.
/// </summary>
public sealed class DeviceInfoDto
{
    /// <summary>
    /// Gets or sets the device model name.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Gets or sets the manufacturer.
    /// </summary>
    public string? Manufacturer { get; set; }

    /// <summary>
    /// Gets or sets the user-facing device name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the OS version string.
    /// </summary>
    public string? VersionString { get; set; }

    /// <summary>
    /// Gets or sets the platform name.
    /// </summary>
    public string? Platform { get; set; }

    /// <summary>
    /// Gets or sets the idiom (Phone, Desktop, Tablet, and so on).
    /// </summary>
    public string? Idiom { get; set; }

    /// <summary>
    /// Gets or sets the device type (Physical or Virtual).
    /// </summary>
    public string? DeviceType { get; set; }
}

/// <summary>
/// Network access level and active connection profiles from MAUI <c>Connectivity</c>.
/// </summary>
public sealed class ConnectivityDto
{
    /// <summary>
    /// Gets or sets the network access level (Internet, ConstrainedInternet, Local, None, Unknown).
    /// </summary>
    public string? NetworkAccess { get; set; }

    /// <summary>
    /// Gets or sets the active connection profiles.
    /// </summary>
    public IReadOnlyList<string> ConnectionProfiles { get; set; } = [];
}
