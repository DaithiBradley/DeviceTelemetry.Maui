namespace DeviceTelemetry.Maui.Dtos
{

    public sealed class DeviceTelemetryDto
    {
        public string DeviceId { get; set; } = string.Empty;
        public DateTimeOffset CapturedAtUtc { get; set; }

        public GeoFixDto? Location { get; set; }
        public BatteryDto Battery { get; set; } = new BatteryDto();

        public GpsQualityDto? GpsQuality { get; set; }
        public WindowsPowerTelemetryDto? WindowsPower { get; set; }
        public NetworkTelemetryDto? Network { get; set; }
    }

    public sealed class GeoFixDto
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? AccuracyMeters { get; set; }
        public double? AltitudeMeters { get; set; }
        public double? SpeedMetersPerSecond { get; set; }
        public double? CourseDegrees { get; set; }
        public DateTimeOffset? FixTimestampUtc { get; set; }
    }

    public sealed class BatteryDto
    {
        public int LevelPercent { get; set; }
        public string State { get; set; } = "Unknown";
        public string PowerSource { get; set; } = "Unknown";
    }

    public sealed class GpsQualityDto
    {
        public int SatellitesInView { get; set; }
        public int SatellitesUsedInFix { get; set; }
        public double? AverageCn0DbHz { get; set; }
        public string QualityBand { get; set; } = "Unknown";
    }

    public sealed class WindowsPowerTelemetryDto
    {
        public int? ScreenBrightnessPercent { get; set; }
        public string EnergySaverStatus { get; set; } = "Unknown";
        public string PowerSupplyStatus { get; set; } = "Unknown";
        public string BatteryStatus { get; set; } = "Unknown";
        public string? ActivePowerPlanName { get; set; }
        public Guid? ActivePowerPlanGuid { get; set; }
    }

    public sealed class NetworkTelemetryDto
    {
        public string? CarrierName { get; set; }
        public string? MobileCountryCode { get; set; }
        public string? MobileNetworkCode { get; set; }
        public string? NetworkType { get; set; }
        public int? SignalStrength { get; set; }
        public string? SignalStrengthUnit { get; set; }
        public string? Imei { get; set; }
        public string? Imsi { get; set; }
        public string? PhoneNumber { get; set; }
        public bool? IsRoaming { get; set; }
        public string? SimState { get; set; }
    }


}
