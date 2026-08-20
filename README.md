# DeviceTelemetry.Maui

[![NuGet](https://img.shields.io/nuget/v/DeviceTelemetry.Maui.svg)](https://www.nuget.org/packages/DeviceTelemetry.Maui/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

Cross-platform device telemetry for .NET MAUI. Capture battery, location, device info, connectivity, GPS quality (Android), Windows power, and network/SIM details.

The library multi-targets **Android**, **iOS**, **Mac Catalyst**, and **Windows**. A given OS only builds the TFMs it can compile: Windows builds Android + Windows; macOS builds Android + iOS + Mac Catalyst.

## Installation

```bash
dotnet add package DeviceTelemetry.Maui
```

## Quick start

```csharp
using DeviceTelemetry.Maui;

var telemetry = await DeviceTelemetryUtil.CaptureAsync("device-123");

Console.WriteLine($"{telemetry.DeviceInfo?.Model}  {telemetry.Battery.LevelPercent}%");
Console.WriteLine(telemetry.LocationStatus);
Console.WriteLine(telemetry.Connectivity?.NetworkAccess);
```

IMEI, IMSI, and phone number are **off by default**. Turn them on only when the host app has already requested the required OS permission:

```csharp
var telemetry = await DeviceTelemetryUtil.CaptureAsync(
    "device-123",
    new CaptureOptions { IncludeIdentifiers = true });
```

## Capture options

| Option | Default | Meaning |
|---|---|---|
| `IncludeLocation` | `true` | Location fix and Android GPS quality |
| `IncludeNetwork` | `true` | Carrier / radio / SIM state |
| `IncludeIdentifiers` | `false` | IMEI, IMSI, phone number (not available on iOS) |
| `IncludeDeviceInfo` | `true` | Model, manufacturer, OS, idiom |
| `IncludeConnectivity` | `true` | `NetworkAccess` and connection profiles |
| `IncludeWindowsPower` | `true` | Brightness, energy saver, power plan (Windows only) |
| `LocationAccuracy` | `Medium` | MAUI geolocation accuracy |
| `LocationTimeout` | 30 seconds | Current-fix timeout |

`LocationStatus` tells the caller why location is present or missing: `Ok`, `Denied`, `Unavailable`, `TimedOut`, or `NotRequested`. Cancellation throws `OperationCanceledException`.

## Platform support

| Feature | Windows | Android | iOS | Mac Catalyst |
|---|---|---|---|---|
| Battery | Yes | Yes | Yes | Yes |
| Location | Yes | Yes | Yes | Yes |
| Device info / connectivity | Yes | Yes | Yes | Yes |
| GPS quality | — | Yes (GNSS) | — | — |
| Windows power | Yes | — | — | — |
| Network / carrier | Yes | Yes | Yes (CoreTelephony) | When cellular hardware is present |
| IMEI / IMSI / phone | Opt-in | Opt-in + `READ_PHONE_STATE` | Not available | Not available |

## Permissions

The host application owns manifests and Info.plist. Request what you actually collect.

### Android (`AndroidManifest.xml`)

```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
<uses-permission android:name="android.permission.ACCESS_COARSE_LOCATION" />
<!-- Only if you set IncludeIdentifiers = true -->
<uses-permission android:name="android.permission.READ_PHONE_STATE" />
```

On Android 10+, IMEI/IMSI are often unavailable to normal apps even with `READ_PHONE_STATE`. The library never prompts for phone permission; it reads identifiers only when that permission is already granted.

### iOS (`Info.plist`)

```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>This app needs location access to capture device telemetry.</string>
```

## Limitations

- Location requires user permission and may fall back to last-known when a current fix times out.
- Battery `LevelPercent` is null when the platform reports an unknown charge (MAUI uses `-1`).
- iOS does not expose IMEI, IMSI, phone number, or GNSS satellite counts to App Store apps.
- Android identifier APIs can throw `SecurityException` on API 29+; those fields stay null.
- Windows brightness and power-plan queries use WMI (`root\wmi` and `root\cimv2\power`) and may be null without admin rights or on devices that do not implement those classes.

## Testing

```bash
dotnet test
```

CI builds Windows + Android on `windows-latest`, builds iOS + Mac Catalyst + Android on `macos-latest`, runs tests, and publishes to NuGet.org only from `v*` tags.

## Changelog

### Version 1.2.0

- Multi-target Android, iOS, Mac Catalyst, and Windows
- `CaptureOptions` with identifier opt-in (default off)
- `LocationCaptureStatus`, `DeviceInfoDto`, and `ConnectivityDto`
- Android GNSS quality via `GnssStatus`
- iOS/Mac Catalyst carrier data via CoreTelephony
- Windows WMI namespace fixes and real `PowerManager.BatteryStatus`
- Cancellation is honored (`OperationCanceledException`)
- CI tests on every PR; NuGet publish on version tags only

### Version 1.1.0

- Network/SIM telemetry for Windows and Android

## License

MIT. See [LICENSE](LICENSE).
