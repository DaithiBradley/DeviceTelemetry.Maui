using DeviceTelemetry.Maui.Dtos;

namespace DeviceTelemetry.Maui;

#if ANDROID
using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.OS;
using Android.Telephony;
using AndroidLocationManager = Android.Locations.LocationManager;

/// <summary>
/// Android-specific telemetry: GNSS quality and TelephonyManager network/SIM data.
/// </summary>
public static partial class DeviceTelemetryUtil
{
    /// <summary>
    /// Collects GNSS satellite quality via <see cref="GnssStatus"/>.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>GPS quality data, or null if unavailable.</returns>
    private static async partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsAndroidVersionAtLeast(24))
        {
            return null;
        }

        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? Application.Context;
            if (context is null)
            {
                return null;
            }

            if (context.GetSystemService(Context.LocationService) is not AndroidLocationManager locationManager)
            {
                return null;
            }

            if (!locationManager.IsProviderEnabled(AndroidLocationManager.GpsProvider))
            {
                return null;
            }

            var tcs = new TaskCompletionSource<GpsQualityDto?>(TaskCreationOptions.RunContinuationsAsynchronously);
#pragma warning disable CA1416 // Guarded by OperatingSystem.IsAndroidVersionAtLeast(24) above.
            var callback = new SatelliteStatusCallback(status => tcs.TrySetResult(MapGnssStatus(status)));
#pragma warning restore CA1416

            try
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(30) && context.MainExecutor is not null)
                {
                    locationManager.RegisterGnssStatusCallback(context.MainExecutor, callback);
                }
                else
                {
                    locationManager.RegisterGnssStatusCallback(callback, new Handler(Looper.MainLooper!));
                }

                using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(TimeSpan.FromSeconds(3));
                using (timeoutCts.Token.Register(() => tcs.TrySetResult(null)))
                {
                    return await tcs.Task.WaitAsync(ct);
                }
            }
            finally
            {
                try
                {
                    locationManager.UnregisterGnssStatusCallback(callback);
                }
                catch
                {
                    // Unregister can throw if the callback was never registered.
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Maps an Android <see cref="GnssStatus"/> snapshot to a DTO.
    /// </summary>
    /// <param name="status">The GNSS status.</param>
    /// <returns>Mapped GPS quality.</returns>
    [SupportedOSPlatform("android24.0")]
    private static GpsQualityDto MapGnssStatus(GnssStatus status)
    {
        var inView = status.SatelliteCount;
        var used = 0;
        double cn0Sum = 0;
        var cn0Count = 0;

        for (var i = 0; i < inView; i++)
        {
            if (status.UsedInFix(i))
            {
                used++;
            }

            var cn0 = status.GetCn0DbHz(i);
            if (cn0 > 0)
            {
                cn0Sum += cn0;
                cn0Count++;
            }
        }

        double? averageCn0 = cn0Count > 0 ? cn0Sum / cn0Count : null;
        var qualityBand = averageCn0 switch
        {
            null when used == 0 => "Unknown",
            null => "Unknown",
            >= 30 => "High",
            >= 20 => "Medium",
            _ => "Low"
        };

        return new GpsQualityDto
        {
            SatellitesInView = inView,
            SatellitesUsedInFix = used,
            AverageCn0DbHz = averageCn0,
            QualityBand = qualityBand
        };
    }

    /// <summary>
    /// Collects network and SIM telemetry for Android devices.
    /// </summary>
    /// <param name="includeIdentifiers">Whether IMEI, IMSI, and phone number should be read.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Network telemetry data, or null if unavailable.</returns>
    private static async partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(
        bool includeIdentifiers,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var context = Platform.CurrentActivity?.ApplicationContext ?? Application.Context;
            if (context is null)
            {
                return null;
            }

            if (context.GetSystemService(Context.TelephonyService) is not TelephonyManager telephonyManager)
            {
                return null;
            }

            var dto = new NetworkTelemetryDto();

            try
            {
                dto.CarrierName = string.IsNullOrWhiteSpace(telephonyManager.NetworkOperatorName)
                ? null
                : telephonyManager.NetworkOperatorName;
            }
            catch
            {
            }

            try
            {
                var networkOperator = telephonyManager.NetworkOperator;
                if (!string.IsNullOrEmpty(networkOperator) && networkOperator.Length >= 5)
                {
                    dto.MobileCountryCode = networkOperator[..3];
                    dto.MobileNetworkCode = networkOperator[3..];
                }
            }
            catch
            {
            }

            try
            {
                dto.NetworkType = MapNetworkType(telephonyManager.NetworkType);
            }
            catch
            {
            }

            try
            {
                if (OperatingSystem.IsAndroidVersionAtLeast(28))
                {
                    var signalStrength = telephonyManager.SignalStrength;
                    if (signalStrength != null)
                    {
                        dto.SignalStrength = signalStrength.Level;
                        dto.SignalStrengthUnit = "Level";
                    }
                }
            }
            catch
            {
            }

            try
            {
                dto.IsRoaming = telephonyManager.IsNetworkRoaming;
            }
            catch
            {
            }

            try
            {
                dto.SimState = MapSimState(telephonyManager.SimState);
            }
            catch
            {
            }

            if (includeIdentifiers)
            {
                await TryPopulateAndroidIdentifiersAsync(telephonyManager, dto);
            }

            if (HasCellularTelemetry(dto))
            {
                return dto;
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads IMEI, IMSI, and line number only when <see cref="Permissions.Phone"/> is already granted.
    /// </summary>
    /// <param name="telephonyManager">The telephony manager.</param>
    /// <param name="dto">The DTO to populate.</param>
    /// <returns>A completed task.</returns>
    private static async Task TryPopulateAndroidIdentifiersAsync(
        TelephonyManager telephonyManager,
        NetworkTelemetryDto dto)
    {
        PermissionStatus phoneStatus;
        try
        {
            phoneStatus = await Permissions.CheckStatusAsync<Permissions.Phone>();
        }
        catch
        {
            return;
        }

        if (phoneStatus != PermissionStatus.Granted)
        {
            return;
        }

        try
        {
            var imei = OperatingSystem.IsAndroidVersionAtLeast(26)
                ? telephonyManager.Imei
                : telephonyManager.DeviceId;
            if (IsLikelyImei(imei))
            {
                dto.Imei = imei;
            }
        }
        catch
        {
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(telephonyManager.SubscriberId))
            {
                dto.Imsi = telephonyManager.SubscriberId;
            }
        }
        catch
        {
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(telephonyManager.Line1Number))
            {
                dto.PhoneNumber = telephonyManager.Line1Number;
            }
        }
        catch
        {
        }
    }

    /// <summary>
    /// Maps Android network type to a stable string.
    /// </summary>
    /// <param name="networkType">The Android network type.</param>
    /// <returns>A display string.</returns>
    private static string MapNetworkType(NetworkType networkType)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(29) && networkType == NetworkType.Nr)
        {
            return "5G";
        }

        return networkType switch
        {
            NetworkType.Lte => "LTE",
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

    /// <summary>
    /// Maps Android SIM state to a stable string.
    /// </summary>
    /// <param name="simState">The Android SIM state.</param>
    /// <returns>A display string.</returns>
    private static string MapSimState(SimState simState)
        => simState switch
        {
            SimState.Absent => "Absent",
            SimState.PinRequired => "PinRequired",
            SimState.PukRequired => "PukRequired",
            SimState.NetworkLocked => "NetworkLocked",
            SimState.Ready => "Ready",
            SimState.Unknown => "Unknown",
            _ => simState.ToString()
        };

    /// <summary>
    /// Windows power is not available on Android.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Always null.</returns>
    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct)
        => Task.FromResult<WindowsPowerTelemetryDto?>(null);

    /// <summary>
    /// GNSS status callback that forwards the first satellite snapshot.
    /// </summary>
    [SupportedOSPlatform("android24.0")]
    private sealed class SatelliteStatusCallback : GnssStatus.Callback
    {
        private readonly Action<GnssStatus> _onChanged;

        /// <summary>
        /// Initializes a new callback.
        /// </summary>
        /// <param name="onChanged">Invoked when satellite status changes.</param>
        public SatelliteStatusCallback(Action<GnssStatus> onChanged)
        {
            _onChanged = onChanged;
        }

        /// <inheritdoc />
        public override void OnSatelliteStatusChanged(GnssStatus status)
            => _onChanged(status);
    }
}
#endif
