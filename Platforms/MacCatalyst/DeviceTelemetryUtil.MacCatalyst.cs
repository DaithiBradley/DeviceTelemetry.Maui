using DeviceTelemetry.Maui.Dtos;

namespace DeviceTelemetry.Maui;

#if MACCATALYST
using CoreTelephony;
using Foundation;

/// <summary>
/// Mac Catalyst-specific telemetry. GNSS and Windows power are not available; carrier data is collected when CoreTelephony provides it.
/// </summary>
public static partial class DeviceTelemetryUtil
{
    /// <summary>
    /// Android GPS quality is not available on Mac Catalyst.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Always null.</returns>
    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct)
        => Task.FromResult<GpsQualityDto?>(null);

    /// <summary>
    /// Windows power is not available on Mac Catalyst.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Always null.</returns>
    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct)
        => Task.FromResult<WindowsPowerTelemetryDto?>(null);

    /// <summary>
    /// Collects carrier information using CoreTelephony when the Mac has cellular hardware.
    /// </summary>
    /// <param name="includeIdentifiers">Ignored; identifiers are not exposed on Mac Catalyst.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Network telemetry data, or null if unavailable.</returns>
    private static partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(
        bool includeIdentifiers,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        _ = includeIdentifiers;

        try
        {
            using var networkInfo = new CTTelephonyNetworkInfo();
            var dto = new NetworkTelemetryDto();

            CTCarrier? carrier = null;
            if (networkInfo.ServiceSubscriberCellularProviders is { } providers)
            {
                foreach (var value in providers.Values)
                {
                    if (value is CTCarrier found)
                    {
                        carrier = found;
                        break;
                    }
                }
            }

            if (carrier != null)
            {
                dto.CarrierName = string.IsNullOrWhiteSpace(carrier.CarrierName) ? null : carrier.CarrierName;
                dto.MobileCountryCode = string.IsNullOrWhiteSpace(carrier.MobileCountryCode)
                    ? null
                    : carrier.MobileCountryCode;
                dto.MobileNetworkCode = string.IsNullOrWhiteSpace(carrier.MobileNetworkCode)
                    ? null
                    : carrier.MobileNetworkCode;
            }

            if (networkInfo.ServiceCurrentRadioAccessTechnology is { } radios)
            {
                foreach (var value in radios.Values)
                {
                    if (value is NSString radio)
                    {
                        dto.NetworkType = radio.ToString();
                        break;
                    }
                }
            }

            if (dto.CarrierName != null || dto.NetworkType != null || dto.MobileCountryCode != null)
            {
                return Task.FromResult<NetworkTelemetryDto?>(dto);
            }

            return Task.FromResult<NetworkTelemetryDto?>(null);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Task.FromResult<NetworkTelemetryDto?>(null);
        }
    }
}
#endif
