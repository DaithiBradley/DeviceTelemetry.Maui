using DeviceTelemetry.Maui.Dtos;

namespace DeviceTelemetry.Maui;

// All the code in this file is only included on Android.
#if ANDROID
using Android.Telephony;
using Android.Content;
using Android.App;
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

    /// <summary>
    /// Collects network and SIM card telemetry data for Android devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Network telemetry data, or null if unavailable.</returns>
    private static partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(CancellationToken ct)
    {
        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? 
                         global::Android.App.Application.Context;
            
            if (context == null)
            {
                return Task.FromResult<NetworkTelemetryDto?>(null);
            }

            var telephonyManager = context.GetSystemService(Context.TelephonyService) as TelephonyManager;
            if (telephonyManager == null)
            {
                return Task.FromResult<NetworkTelemetryDto?>(null);
            }

            var dto = new NetworkTelemetryDto();

            // Get carrier name
            try
            {
                dto.CarrierName = telephonyManager.NetworkOperatorName;
            }
            catch { }

            // Get Mobile Country Code (MCC) and Mobile Network Code (MNC)
            try
            {
                var networkOperator = telephonyManager.NetworkOperator;
                if (!string.IsNullOrEmpty(networkOperator) && networkOperator.Length >= 5)
                {
                    dto.MobileCountryCode = networkOperator.Substring(0, 3);
                    dto.MobileNetworkCode = networkOperator.Substring(3);
                }
            }
            catch { }

            // Get network type
            try
            {
                var networkType = telephonyManager.NetworkType;
                dto.NetworkType = networkType switch
                {
                    NetworkType.Lte => "LTE",
                    NetworkType.Nr => "5G",
                    NetworkType.Hspa => "HSPA",
                    NetworkType.Hspap => "HSPA+",
                    NetworkType.Edge => "EDGE",
                    NetworkType.Gprs => "GPRS",
                    NetworkType.Cdma => "CDMA",
                    NetworkType.Evdo0 => "EVDO 0",
                    NetworkType.EvdoA => "EVDO A",
                    NetworkType.EvdoB => "EVDO B",
                    NetworkType.Unknown => "Unknown",
                    _ => networkType.ToString()
                };
            }
            catch { }

            // Get signal strength
            try
            {
                var signalStrength = telephonyManager.SignalStrength;
                if (signalStrength != null)
                {
                    // Signal strength is typically in dBm (negative values)
                    // Android provides level (0-4) which we can convert
                    var level = signalStrength.Level;
                    dto.SignalStrength = level;
                    dto.SignalStrengthUnit = "Level"; // Android provides level, not direct dBm
                }
            }
            catch { }

            // Get IMEI (requires READ_PHONE_STATE permission)
            try
            {
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                {
                    dto.Imei = telephonyManager.Imei;
                }
                else
                {
                    dto.Imei = telephonyManager.DeviceId;
                }
            }
            catch { }

            // Get IMSI (requires READ_PHONE_STATE permission)
            try
            {
                dto.Imsi = telephonyManager.SubscriberId;
            }
            catch { }

            // Get phone number (may not be available on all devices)
            try
            {
                dto.PhoneNumber = telephonyManager.Line1Number;
            }
            catch { }

            // Get roaming status
            try
            {
                dto.IsRoaming = telephonyManager.IsNetworkRoaming;
            }
            catch { }

            // Get SIM state
            try
            {
                var simState = telephonyManager.SimState;
                dto.SimState = simState switch
                {
                    SimState.Absent => "Absent",
                    SimState.PinRequired => "PinRequired",
                    SimState.PukRequired => "PukRequired",
                    SimState.NetworkLocked => "NetworkLocked",
                    SimState.Ready => "Ready",
                    SimState.Unknown => "Unknown",
                    _ => simState.ToString()
                };
            }
            catch { }

            // Only return DTO if we have at least some information
            if (dto.CarrierName != null || dto.NetworkType != null || dto.Imei != null || 
                dto.MobileCountryCode != null || dto.SimState != null)
            {
                return Task.FromResult<NetworkTelemetryDto?>(dto);
            }

            return Task.FromResult<NetworkTelemetryDto?>(null);
        }
        catch
        {
            return Task.FromResult<NetworkTelemetryDto?>(null);
        }
    }
}
#endif

