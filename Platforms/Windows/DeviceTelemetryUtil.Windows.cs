using DeviceTelemetry.Maui.Dtos;
using WindowsSystemPower = Windows.System.Power;

namespace DeviceTelemetry.Maui;

// All the code in this file is only included on Windows.
#if WINDOWS
public static partial class DeviceTelemetryUtil
{
    private static partial Task<GpsQualityDto?> TryGetAndroidGpsQualityAsync(CancellationToken ct)
        => Task.FromResult<GpsQualityDto?>(null);

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

