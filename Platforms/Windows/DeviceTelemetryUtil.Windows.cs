using DeviceTelemetry.Maui.Dtos;
using WindowsSystemPower = Windows.System.Power;
using WindowsNetworkingConnectivity = Windows.Networking.Connectivity;

namespace DeviceTelemetry.Maui;

// All the code in this file is only included on Windows.
#if WINDOWS
public static partial class DeviceTelemetryUtil
{
    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct)
        => Task.FromResult<GpsQualityDto?>(null);

    /// <summary>
    /// Collects network and SIM card telemetry data for Windows devices.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Network telemetry data, or null if unavailable.</returns>
    private static partial Task<NetworkTelemetryDto?> TryGetNetworkTelemetryAsync(CancellationToken ct)
    {
        try
        {
            var dto = new NetworkTelemetryDto();

            // Get network information using Windows Runtime APIs
            GetNetworkInfoFromWindowsRuntime(dto);

            // Get SIM card information using WMI (more reliable for SIM data)
            GetSimInfoFromWmi(dto);

            // Only return DTO if we have at least some information
            if (dto.CarrierName != null || dto.NetworkType != null || dto.Imei != null || 
                dto.MobileCountryCode != null || dto.MobileNetworkCode != null)
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

    /// <summary>
    /// Gets network information from Windows Runtime APIs.
    /// </summary>
    private static void GetNetworkInfoFromWindowsRuntime(NetworkTelemetryDto dto)
    {
        try
        {
            var profile = WindowsNetworkingConnectivity.NetworkInformation.GetInternetConnectionProfile();
            if (profile == null)
            {
                return;
            }

            // Get network connectivity level and type
            var connectivityLevel = profile.GetNetworkConnectivityLevel();
            var connectionCost = profile.GetConnectionCost();
            var dataPlanStatus = profile.GetDataPlanStatus();

            // Determine network type
            var networkAdapter = profile.NetworkAdapter;
            if (networkAdapter != null)
            {
                var ianaInterfaceType = networkAdapter.IanaInterfaceType;
                
                // 243 = WWAN (Wireless WAN - Cellular)
                if (ianaInterfaceType == 243)
                {
                    dto.NetworkType = "Cellular";
                    
                    // Try to get more specific network type from WMI
                    var specificType = GetCellularNetworkType();
                    if (specificType != null)
                    {
                        dto.NetworkType = specificType;
                    }
                }
                else if (ianaInterfaceType == 71) // WiFi
                {
                    dto.NetworkType = "WiFi";
                }
                else if (ianaInterfaceType == 6) // Ethernet
                {
                    dto.NetworkType = "Ethernet";
                }
                else
                {
                    dto.NetworkType = $"Type_{ianaInterfaceType}";
                }
            }

            // Get roaming status
            if (connectionCost != null)
            {
                dto.IsRoaming = connectionCost.Roaming;
            }

            // Get data plan information if available
            if (dataPlanStatus != null)
            {
                // Data plan status might contain carrier info
            }
        }
        catch
        {
            // Windows Runtime APIs might not be available
        }
    }

    /// <summary>
    /// Gets cellular network type (LTE, 5G, etc.) from WMI.
    /// </summary>
    private static string? GetCellularNetworkType()
    {
        try
        {
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_NetworkAdapter WHERE AdapterTypeID = 9");

            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                var name = obj["Name"]?.ToString();
                if (name != null)
                {
                    if (name.Contains("5G") || name.Contains("NR"))
                    {
                        return "5G";
                    }
                    else if (name.Contains("LTE") || name.Contains("4G"))
                    {
                        return "LTE";
                    }
                    else if (name.Contains("3G"))
                    {
                        return "3G";
                    }
                    else if (name.Contains("HSPA"))
                    {
                        return "HSPA";
                    }
                }
            }
        }
        catch
        {
            // WMI might not be available
        }

        return null;
    }

    /// <summary>
    /// Gets SIM card information from WMI.
    /// </summary>
    private static void GetSimInfoFromWmi(NetworkTelemetryDto dto)
    {
        try
        {
            // Get SIM information from Win32_POTSModemToSerialPort (for mobile broadband modems)
            using var modemSearcher = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_POTSModem");

            foreach (System.Management.ManagementObject modem in modemSearcher.Get())
            {
                var description = modem["Description"]?.ToString();
                var attachedTo = modem["AttachedTo"]?.ToString();
                
                if (description != null && 
                    (description.Contains("WWAN") || description.Contains("Cellular") || 
                     description.Contains("Mobile Broadband") || description.Contains("LTE") ||
                     description.Contains("5G")))
                {
                    if (dto.CarrierName == null && attachedTo != null)
                    {
                        dto.CarrierName = attachedTo;
                    }
                    break;
                }
            }

            // Try to get IMEI and carrier info from Win32_PnPEntity
            using var pnpSearcher = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE Description LIKE '%WWAN%' OR Description LIKE '%Cellular%' OR Description LIKE '%Mobile Broadband%'");

            foreach (System.Management.ManagementObject pnp in pnpSearcher.Get())
            {
                var description = pnp["Description"]?.ToString();
                var name = pnp["Name"]?.ToString();
                
                if (description != null)
                {
                    if (dto.NetworkType == null)
                    {
                        dto.NetworkType = description;
                    }
                    
                    // Try to extract carrier name from description
                    if (dto.CarrierName == null && name != null)
                    {
                        // Carrier name might be in the device name
                        dto.CarrierName = name;
                    }
                }
            }

            // Try to get IMEI from registry (if available)
            try
            {
                using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\WWAN\Profile");
                
                if (key != null)
                {
                    var subKeyNames = key.GetSubKeyNames();
                    foreach (var subKeyName in subKeyNames)
                    {
                        using var subKey = key.OpenSubKey(subKeyName);
                        if (subKey != null)
                        {
                            var profileName = subKey.GetValue("ProfileName")?.ToString();
                            if (profileName != null && dto.CarrierName == null)
                            {
                                dto.CarrierName = profileName;
                            }
                            
                            // Try to get IMEI
                            var deviceId = subKey.GetValue("DeviceID")?.ToString();
                            if (deviceId != null && deviceId.Length >= 15)
                            {
                                dto.Imei = deviceId;
                            }
                        }
                    }
                }
            }
            catch
            {
                // Registry access might fail
            }
        }
        catch
        {
            // WMI might not be available or SIM not present
        }
    }

    /// <summary>
    /// Collects Windows power telemetry data using Windows Runtime APIs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Windows power telemetry data, or null if unavailable.</returns>
    private static partial Task<WindowsPowerTelemetryDto?> TryGetWindowsPowerAsync(CancellationToken ct)
    {
        try
        {
            var dto = new WindowsPowerTelemetryDto();

            // Get screen brightness
            dto.ScreenBrightnessPercent = GetScreenBrightness();

            // Get energy saver status
            dto.EnergySaverStatus = GetEnergySaverStatus();

            // Get power supply status
            dto.PowerSupplyStatus = GetPowerSupplyStatus();

            // Get battery status
            dto.BatteryStatus = GetBatteryStatus();

            // Get active power plan
            var powerPlan = GetActivePowerPlan();
            dto.ActivePowerPlanName = powerPlan.Name;
            dto.ActivePowerPlanGuid = powerPlan.Guid;

            return Task.FromResult<WindowsPowerTelemetryDto?>(dto);
        }
        catch
        {
            // Return null if any errors occur
            return Task.FromResult<WindowsPowerTelemetryDto?>(null);
        }
    }

    /// <summary>
    /// Gets the current screen brightness percentage.
    /// </summary>
    /// <returns>Brightness percentage (0-100), or null if unavailable.</returns>
    private static int? GetScreenBrightness()
    {
        try
        {
            // Try to get brightness from registry (Windows 10/11)
            // The brightness is stored per display in the registry
            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Wmi\MonitorBrightness");
            
            if (key != null)
            {
                var valueNames = key.GetValueNames();
                if (valueNames.Length > 0)
                {
                    // Get the first display's brightness value
                    var brightnessValue = key.GetValue(valueNames[0]);
                    if (brightnessValue is int brightness && brightness >= 0 && brightness <= 100)
                    {
                        return brightness;
                    }
                }
            }

            // Alternative: Try to get from WMI
            try
            {
                using var searcher = new System.Management.ManagementObjectSearcher(
                    "SELECT CurrentBrightness FROM WmiMonitorBrightness WHERE Active = true");
                
                foreach (System.Management.ManagementObject obj in searcher.Get())
                {
                    var brightness = obj["CurrentBrightness"];
                    if (brightness != null && brightness is byte brightnessByte)
                    {
                        return brightnessByte; // WMI returns 0-100 as byte
                    }
                }
            }
            catch
            {
                // WMI might not be available or accessible
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
    /// Gets the battery status.
    /// </summary>
    /// <returns>Battery status string.</returns>
    private static string GetBatteryStatus()
    {
        try
        {
            // Note: PowerManager doesn't have a direct BatteryStatus property
            // We can infer from PowerSupplyStatus and EnergySaverStatus
            var powerSupply = WindowsSystemPower.PowerManager.PowerSupplyStatus;
            return powerSupply switch
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
    /// Gets the active power plan information.
    /// </summary>
    /// <returns>Power plan name and GUID.</returns>
    private static (string? Name, Guid? Guid) GetActivePowerPlan()
    {
        try
        {
            // Use WMI to get power plan information
            using var searcher = new System.Management.ManagementObjectSearcher(
                "SELECT * FROM Win32_PowerPlan WHERE IsActive = True");

            foreach (System.Management.ManagementObject obj in searcher.Get())
            {
                var name = obj["ElementName"]?.ToString();
                var instanceId = obj["InstanceID"]?.ToString();

                // Extract GUID from InstanceID (format: Microsoft:PowerPlan\{guid})
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

