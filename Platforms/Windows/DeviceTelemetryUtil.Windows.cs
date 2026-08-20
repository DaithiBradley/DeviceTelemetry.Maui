using DeviceTelemetry.Maui.Dtos;
using WindowsSystemPower = Windows.System.Power;
using WindowsNetworkingConnectivity = Windows.Networking.Connectivity;

namespace DeviceTelemetry.Maui;

#if WINDOWS
/// <summary>
/// Windows-specific telemetry: WinRT power APIs, WMI brightness/power plans, and network/SIM hints.
/// </summary>
public static partial class DeviceTelemetryUtil
{
    /// <summary>
    /// Android GPS quality is not available on Windows.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Always null.</returns>
    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct)
        => Task.FromResult<GpsQualityDto?>(null);

    /// <summary>
    /// Collects network and SIM card telemetry data for Windows devices.
    /// </summary>
    /// <param name="includeIdentifiers">Whether IMEI-like identifiers should be read from the WWAN registry.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Network telemetry data, or null if unavailable.</returns>
    private static partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(
        bool includeIdentifiers,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var dto = new NetworkTelemetryDto();
            GetNetworkInfoFromMobileBroadband(dto, includeIdentifiers);
            GetNetworkInfoFromWindowsRuntime(dto);

            if (HasCellularTelemetry(dto))
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

    /// <summary>
    /// Gets cellular carrier and identifier data from the Windows mobile broadband APIs.
    /// </summary>
    /// <param name="dto">The DTO to populate.</param>
    /// <param name="includeIdentifiers">Whether IMEI, IMSI, and phone number should be read.</param>
    private static void GetNetworkInfoFromMobileBroadband(NetworkTelemetryDto dto, bool includeIdentifiers)
    {
        try
        {
            var modem = Windows.Networking.NetworkOperators.MobileBroadbandModem.GetDefault();
            if (modem is null)
            {
                return;
            }

            try
            {
                var network = modem.CurrentNetwork;
                if (network != null)
                {
                    if (!string.IsNullOrWhiteSpace(network.RegisteredProviderName))
                    {
                        dto.CarrierName = network.RegisteredProviderName;
                    }

                    dto.NetworkType ??= MapMobileBroadbandDataClass(network.RegisteredDataClass.ToString());
                }
            }
            catch
            {
            }

            try
            {
                var info = modem.DeviceInformation;
                if (info is null)
                {
                    return;
                }

                if (!includeIdentifiers)
                {
                    return;
                }

                if (IsLikelyImei(info.DeviceId))
                {
                    dto.Imei = info.DeviceId;
                }

                if (!string.IsNullOrWhiteSpace(info.SubscriberId))
                {
                    dto.Imsi = info.SubscriberId;
                }

                var number = info.TelephoneNumbers?.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(number))
                {
                    dto.PhoneNumber = number;
                }
            }
            catch
            {
            }
        }
        catch
        {
            // No mobile broadband modem, or the WinRT APIs are unavailable.
        }
    }

    /// <summary>
    /// Maps a Windows mobile-broadband data-class name to a stable radio string.
    /// </summary>
    /// <param name="dataClass">The data class name.</param>
    /// <returns>A radio type string, or null when none is present.</returns>
    private static string? MapMobileBroadbandDataClass(string? dataClass)
    {
        if (string.IsNullOrWhiteSpace(dataClass) || dataClass == "None")
        {
            return null;
        }

        if (dataClass.Contains("Lte", StringComparison.OrdinalIgnoreCase))
        {
            return "LTE";
        }

        if (dataClass.Contains("Hsdpa", StringComparison.OrdinalIgnoreCase)
            || dataClass.Contains("Hsupa", StringComparison.OrdinalIgnoreCase)
            || dataClass.Contains("Umts", StringComparison.OrdinalIgnoreCase))
        {
            return "HSPA";
        }

        if (dataClass.Contains("Edge", StringComparison.OrdinalIgnoreCase))
        {
            return "EDGE";
        }

        if (dataClass.Contains("Gprs", StringComparison.OrdinalIgnoreCase))
        {
            return "GPRS";
        }

        if (dataClass.Contains("Nr", StringComparison.OrdinalIgnoreCase)
            || dataClass.Contains("5G", StringComparison.OrdinalIgnoreCase))
        {
            return "5G";
        }

        return dataClass;
    }

    /// <summary>
    /// Gets network information from Windows Runtime APIs. Only cellular (WWAN) profiles are recorded here; Wi-Fi and Ethernet belong on <see cref="ConnectivityDto"/>.
    /// </summary>
    /// <param name="dto">The DTO to populate.</param>
    private static void GetNetworkInfoFromWindowsRuntime(NetworkTelemetryDto dto)
    {
        try
        {
            var profile = WindowsNetworkingConnectivity.NetworkInformation.GetInternetConnectionProfile();
            if (profile == null)
            {
                return;
            }

            var networkAdapter = profile.NetworkAdapter;
            if (networkAdapter is null || networkAdapter.IanaInterfaceType != 243)
            {
                return;
            }

            dto.NetworkType ??= GetCellularNetworkType() ?? "Cellular";

            var connectionCost = profile.GetConnectionCost();
            if (connectionCost != null)
            {
                dto.IsRoaming = connectionCost.Roaming;
            }
        }
        catch
        {
            // Windows Runtime APIs might not be available.
        }
    }

    /// <summary>
    /// Gets cellular network type (LTE, 5G, etc.) from WMI adapter names.
    /// </summary>
    /// <returns>A network type string, or null if it cannot be determined.</returns>
    private static string? GetCellularNetworkType()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT Name FROM Win32_NetworkAdapter WHERE AdapterTypeID = 9");

            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (name == null)
                {
                    continue;
                }

                if (name.Contains("5G", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("NR", StringComparison.OrdinalIgnoreCase))
                {
                    return "5G";
                }

                if (name.Contains("LTE", StringComparison.OrdinalIgnoreCase)
                    || name.Contains("4G", StringComparison.OrdinalIgnoreCase))
                {
                    return "LTE";
                }

                if (name.Contains("3G", StringComparison.OrdinalIgnoreCase))
                {
                    return "3G";
                }

                if (name.Contains("HSPA", StringComparison.OrdinalIgnoreCase))
                {
                    return "HSPA";
                }
            }
        }
        catch
        {
            // WMI might not be available.
        }

        return null;
    }

    /// <summary>
    /// Collects Windows power telemetry data using Windows Runtime and WMI APIs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Windows power telemetry data, or null if unavailable.</returns>
    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            var powerPlan = GetActivePowerPlan();
            var dto = new WindowsPowerTelemetryDto
            {
                ScreenBrightnessPercent = GetScreenBrightness(),
                EnergySaverStatus = GetEnergySaverStatus(),
                PowerSupplyStatus = GetPowerSupplyStatus(),
                BatteryStatus = GetBatteryStatus(),
                ActivePowerPlanName = powerPlan.Name,
                ActivePowerPlanGuid = powerPlan.Guid
            };

            return Task.FromResult<WindowsPowerTelemetryDto?>(dto);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return Task.FromResult<WindowsPowerTelemetryDto?>(null);
        }
    }

    /// <summary>
    /// Gets the current screen brightness percentage from WMI (root\wmi).
    /// </summary>
    /// <returns>Brightness percentage (0-100), or null if unavailable.</returns>
    private static int? GetScreenBrightness()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\wmi",
                "SELECT CurrentBrightness FROM WmiMonitorBrightness WHERE Active = true");

            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                var brightness = obj["CurrentBrightness"];
                if (brightness is byte brightnessByte)
                {
                    return brightnessByte;
                }

                if (brightness != null && byte.TryParse(brightness.ToString(), out var parsed))
                {
                    return parsed;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the energy saver status.
    /// </summary>
    /// <returns>Energy saver status string.</returns>
    private static string GetEnergySaverStatus()
    {
        try
        {
            var status = WindowsSystemPower.PowerManager.EnergySaverStatus;
            return status switch
            {
                WindowsSystemPower.EnergySaverStatus.On => "On",
                WindowsSystemPower.EnergySaverStatus.Off => "Off",
                _ => "Unknown"
            };
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the power supply status.
    /// </summary>
    /// <returns>Power supply status string.</returns>
    private static string GetPowerSupplyStatus()
    {
        try
        {
            var status = WindowsSystemPower.PowerManager.PowerSupplyStatus;
            return status switch
            {
                WindowsSystemPower.PowerSupplyStatus.Adequate => "Adequate",
                WindowsSystemPower.PowerSupplyStatus.Inadequate => "Inadequate",
                WindowsSystemPower.PowerSupplyStatus.NotPresent => "NotPresent",
                _ => "Unknown"
            };
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the battery status from <c>PowerManager.BatteryStatus</c>.
    /// </summary>
    /// <returns>Battery status string.</returns>
    private static string GetBatteryStatus()
    {
        try
        {
            var status = WindowsSystemPower.PowerManager.BatteryStatus;
            return status switch
            {
                WindowsSystemPower.BatteryStatus.Charging => "Charging",
                WindowsSystemPower.BatteryStatus.Discharging => "Discharging",
                WindowsSystemPower.BatteryStatus.Idle => "Idle",
                WindowsSystemPower.BatteryStatus.NotPresent => "NotPresent",
                _ => "Unknown"
            };
        }
        catch
        {
            return "Unknown";
        }
    }

    /// <summary>
    /// Gets the active power plan from WMI in root\cimv2\power.
    /// </summary>
    /// <returns>Power plan name and GUID.</returns>
    private static (string? Name, Guid? Guid) GetActivePowerPlan()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                @"root\cimv2\power",
                "SELECT ElementName, InstanceID FROM Win32_PowerPlan WHERE IsActive = True");

            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                var name = obj["ElementName"]?.ToString();
                var instanceId = obj["InstanceID"]?.ToString();

                Guid? guid = null;
                if (instanceId != null && instanceId.Contains('{'))
                {
                    var guidStart = instanceId.IndexOf('{');
                    var guidEnd = instanceId.IndexOf('}', guidStart);
                    if (guidEnd > guidStart)
                    {
                        var guidString = instanceId.Substring(guidStart, guidEnd - guidStart + 1);
                        if (Guid.TryParse(guidString, out var parsedGuid))
                        {
                            guid = parsedGuid;
                        }
                    }
                }

                return (name, guid);
            }

            return (null, null);
        }
        catch
        {
            return (null, null);
        }
    }
}
#endif
