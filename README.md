# DeviceTelemetry.Maui

[![NuGet](https://img.shields.io/nuget/v/DeviceTelemetry.Maui.svg)](https://www.nuget.org/packages/DeviceTelemetry.Maui)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Cross-platform device telemetry utilities for .NET MAUI applications. Capture battery status, location data, GPS quality, Windows power information, and network/SIM card details from your MAUI apps.

## Features

- 🔋 **Battery Information**: Charge level, state, and power source
- 📍 **Location Services**: GPS coordinates with accuracy, altitude, speed, and course
- 🛰️ **GPS Quality** (Android): Satellite information and signal quality
- ⚡ **Windows Power Telemetry**: Screen brightness, energy saver status, power plans
- 📱 **Network/SIM Telemetry**: Carrier information, network type, signal strength, IMEI/IMSI (where available)
- 🎯 **Cross-Platform**: Works on Windows, Android, and iOS
- ✅ **Fully Tested**: Comprehensive test coverage

## Installation

Install the package from NuGet:

```bash
dotnet add package DeviceTelemetry.Maui
```

Or via Package Manager:

```powershell
Install-Package DeviceTelemetry.Maui
```

## Quick Start

```csharp
using DeviceTelemetry.Maui;

// Capture device telemetry
var telemetry = await DeviceTelemetryUtil.CaptureAsync("device-123");

Console.WriteLine($"Device ID: {telemetry.DeviceId}");
Console.WriteLine($"Battery: {telemetry.Battery.LevelPercent}%");
Console.WriteLine($"Location: {telemetry.Location?.Latitude}, {telemetry.Location?.Longitude}");
Console.WriteLine($"Network: {telemetry.Network?.CarrierName} ({telemetry.Network?.NetworkType})");
```

## Usage

### Basic Usage

```csharp
using DeviceTelemetry.Maui;
using DeviceTelemetry.Maui.Dtos;

// Capture telemetry data
var telemetry = await DeviceTelemetryUtil.CaptureAsync(
    deviceId: "my-device-001",
    cancellationToken: CancellationToken.None);

// Access battery information
var batteryLevel = telemetry.Battery.LevelPercent;
var batteryState = telemetry.Battery.State; // "Charging", "Discharging", "Full", etc.
var powerSource = telemetry.Battery.PowerSource; // "AC", "Usb", "Wireless", etc.

// Access location (may be null if permission denied or unavailable)
if (telemetry.Location != null)
{
    var latitude = telemetry.Location.Latitude;
    var longitude = telemetry.Location.Longitude;
    var accuracy = telemetry.Location.AccuracyMeters;
    var altitude = telemetry.Location.AltitudeMeters;
    var speed = telemetry.Location.SpeedMetersPerSecond;
    var course = telemetry.Location.CourseDegrees;
    var timestamp = telemetry.Location.FixTimestampUtc;
}

// Access network/SIM information (may be null if no cellular connectivity)
if (telemetry.Network != null)
{
    var carrier = telemetry.Network.CarrierName;
    var networkType = telemetry.Network.NetworkType; // "LTE", "5G", "WiFi", etc.
    var signalStrength = telemetry.Network.SignalStrength;
    var isRoaming = telemetry.Network.IsRoaming;
    var mcc = telemetry.Network.MobileCountryCode;
    var mnc = telemetry.Network.MobileNetworkCode;
}

// Platform-specific data
var gpsQuality = telemetry.GpsQuality; // Android only
var windowsPower = telemetry.WindowsPower; // Windows only
var network = telemetry.Network; // Available on Windows and Android
```

### With Cancellation Token

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var telemetry = await DeviceTelemetryUtil.CaptureAsync(
        "device-123",
        cts.Token);
    
    // Process telemetry data
}
catch (OperationCanceledException)
{
    // Handle cancellation
}
```

### Error Handling

```csharp
try
{
    var telemetry = await DeviceTelemetryUtil.CaptureAsync("device-123");
    
    // Location may be null if:
    // - Permission denied
    // - GPS unavailable
    // - Location services disabled
    if (telemetry.Location == null)
    {
        Console.WriteLine("Location data unavailable");
    }
    
    // Platform-specific data may be null on non-target platforms
    if (telemetry.GpsQuality == null)
    {
        Console.WriteLine("GPS quality data only available on Android");
    }
    
    if (telemetry.WindowsPower == null)
    {
        Console.WriteLine("Windows power data only available on Windows");
    }
}
catch (Exception ex)
{
    // Handle unexpected errors
    Console.WriteLine($"Error capturing telemetry: {ex.Message}");
}
```

## API Reference

### `DeviceTelemetryUtil.CaptureAsync`

Captures device telemetry data asynchronously.

**Parameters:**
- `deviceId` (string): Unique identifier for the device
- `ct` (CancellationToken, optional): Cancellation token

**Returns:**
- `Task<DeviceTelemetryDto>`: Device telemetry data

**Example:**
```csharp
var telemetry = await DeviceTelemetryUtil.CaptureAsync("device-123");
```

### Data Transfer Objects

#### `DeviceTelemetryDto`

Main telemetry data container.

```csharp
public class DeviceTelemetryDto
{
    public string DeviceId { get; set; }
    public DateTimeOffset CapturedAtUtc { get; set; }
    public GeoFixDto? Location { get; set; }
    public BatteryDto Battery { get; set; }
    public GpsQualityDto? GpsQuality { get; set; }
    public WindowsPowerTelemetryDto? WindowsPower { get; set; }
}
```

#### `BatteryDto`

Battery information.

```csharp
public class BatteryDto
{
    public int LevelPercent { get; set; }        // 0-100
    public string State { get; set; }            // "Charging", "Discharging", "Full", "NotCharging", "Unknown"
    public string PowerSource { get; set; }      // "AC", "Usb", "Wireless", "Battery", "Unknown"
}
```

#### `GeoFixDto`

Location information.

```csharp
public class GeoFixDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? AccuracyMeters { get; set; }
    public double? AltitudeMeters { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
    public double? CourseDegrees { get; set; }
    public DateTimeOffset? FixTimestampUtc { get; set; }
}
```

#### `GpsQualityDto` (Android only)

GPS quality information.

```csharp
public class GpsQualityDto
{
    public int SatellitesInView { get; set; }
    public int SatellitesUsedInFix { get; set; }
    public double? AverageCn0DbHz { get; set; }
    public string QualityBand { get; set; }
}
```

#### `WindowsPowerTelemetryDto` (Windows only)

Windows power information.

```csharp
public class WindowsPowerTelemetryDto
{
    public int? ScreenBrightnessPercent { get; set; }
    public string EnergySaverStatus { get; set; }
    public string PowerSupplyStatus { get; set; }
    public string BatteryStatus { get; set; }
    public string? ActivePowerPlanName { get; set; }
    public Guid? ActivePowerPlanGuid { get; set; }
}
```

## Platform Support

| Feature | Windows | Android | iOS |
|---------|---------|---------|-----|
| Battery Information | ✅ | ✅ | ✅ |
| Location Services | ✅ | ✅ | ✅ |
| GPS Quality | ❌ | ✅ | ❌ |
| Windows Power Telemetry | ✅ | ❌ | ❌ |
| Network/SIM Telemetry | ✅ | ✅ | ❌ |

## Permissions

### Required Permissions

#### Android
Add to `AndroidManifest.xml`:
```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_BACKGROUND_LOCATION" />
<uses-permission android:name="android.permission.READ_PHONE_STATE" />
```

**Note**: `READ_PHONE_STATE` permission is required for IMEI, IMSI, and phone number access. Some information may be limited on Android 10+ due to privacy restrictions.

#### iOS
Add to `Info.plist`:
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>This app needs location access to capture device telemetry.</string>
```

#### Windows
Location permissions are handled automatically by the system.

## Limitations

1. **Location Data**:
   - Requires user permission (may be denied)
   - May be unavailable if GPS is disabled
   - Falls back to last known location if current location cannot be obtained
   - Accuracy depends on device capabilities and environment

2. **Platform-Specific Features**:
   - GPS Quality data is only available on Android
   - Windows Power Telemetry is only available on Windows
   - Network/SIM Telemetry is available on Windows and Android (not iOS)
   - These properties will be `null` on unsupported platforms

3. **Network/SIM Information**:
   - Requires cellular connectivity (SIM card) to be present
   - IMEI and IMSI access requires `READ_PHONE_STATE` permission on Android
   - Phone number may not be available on all devices
   - Some information may be restricted on Android 10+ due to privacy policies
   - Windows implementation may have limited SIM information depending on device drivers

4. **Battery Information**:
   - Accuracy may vary by platform
   - Some power source information may not be available on all devices

5. **Performance**:
   - Location capture may take several seconds
   - GPS acquisition can be slow, especially indoors
   - Consider using cancellation tokens for timeouts

5. **Privacy**:
   - Location data is sensitive - ensure proper handling and storage
   - Comply with local privacy regulations (GDPR, etc.)

## Best Practices

1. **Handle Null Values**: Always check for null before accessing location or platform-specific data
2. **Request Permissions**: Request location permissions before calling `CaptureAsync`
3. **Use Cancellation Tokens**: Set appropriate timeouts for location capture
4. **Error Handling**: Wrap calls in try-catch blocks
5. **Privacy**: Only capture and store telemetry data with user consent

## Example: Complete Usage

```csharp
using DeviceTelemetry.Maui;
using DeviceTelemetry.Maui.Dtos;

public class TelemetryService
{
    public async Task<DeviceTelemetryDto?> CaptureTelemetryAsync(
        string deviceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var telemetry = await DeviceTelemetryUtil.CaptureAsync(
                deviceId,
                cancellationToken);
            
            // Log telemetry data
            LogTelemetry(telemetry);
            
            return telemetry;
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Telemetry capture was cancelled");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error capturing telemetry: {ex.Message}");
            return null;
        }
    }
    
    private void LogTelemetry(DeviceTelemetryDto telemetry)
    {
        Console.WriteLine($"Device: {telemetry.DeviceId}");
        Console.WriteLine($"Captured: {telemetry.CapturedAtUtc}");
        Console.WriteLine($"Battery: {telemetry.Battery.LevelPercent}% ({telemetry.Battery.State})");
        
        if (telemetry.Location != null)
        {
            Console.WriteLine($"Location: {telemetry.Location.Latitude}, {telemetry.Location.Longitude}");
            Console.WriteLine($"Accuracy: {telemetry.Location.AccuracyMeters}m");
        }
        
        if (telemetry.Network != null)
        {
            Console.WriteLine($"Carrier: {telemetry.Network.CarrierName}");
            Console.WriteLine($"Network: {telemetry.Network.NetworkType}");
            Console.WriteLine($"Signal: {telemetry.Network.SignalStrength} {telemetry.Network.SignalStrengthUnit}");
            Console.WriteLine($"Roaming: {telemetry.Network.IsRoaming}");
        }
        
        #if ANDROID
        if (telemetry.GpsQuality != null)
        {
            Console.WriteLine($"GPS Satellites: {telemetry.GpsQuality.SatellitesUsedInFix}/{telemetry.GpsQuality.SatellitesInView}");
        }
        #endif
        
        #if WINDOWS
        if (telemetry.WindowsPower != null)
        {
            Console.WriteLine($"Power Plan: {telemetry.WindowsPower.ActivePowerPlanName}");
        }
        #endif
    }
}
```

## Testing

The library includes comprehensive unit tests. To run tests:

```bash
dotnet test
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For issues, questions, or feature requests, please open an issue on [GitHub](https://github.com/DaithiBradley/DeviceTelemetry.Maui/issues).

## Changelog

### Version 1.0.4
- Added Network/SIM telemetry support for Windows and Android
- Implemented carrier name, network type, signal strength detection
- Added IMEI, IMSI, MCC, MNC, and roaming status support
- Windows implementation uses Windows Runtime APIs and WMI
- Android implementation uses TelephonyManager APIs

### Version 1.0.3
- Updated workflow to publish on every push
- Added repository metadata to package
- Implemented Windows power telemetry collection
- Added screen brightness detection (Windows)

### Version 1.0.2
- Initial release
- Battery information capture
- Location services integration
- Android GPS quality support
- Windows power telemetry support

## Acknowledgments

Built with .NET MAUI and designed for cross-platform mobile and desktop applications.

