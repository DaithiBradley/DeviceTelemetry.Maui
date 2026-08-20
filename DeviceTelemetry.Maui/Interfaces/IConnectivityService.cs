namespace DeviceTelemetry.Maui.Interfaces;

/// <summary>
/// Abstraction over MAUI <c>Connectivity</c> so telemetry capture can be tested without a live network stack.
/// </summary>
public interface IConnectivityService
{
    /// <summary>
    /// Gets the current network access level.
    /// </summary>
    NetworkAccess NetworkAccess { get; }

    /// <summary>
    /// Gets the active connection profiles (WiFi, Cellular, Ethernet, and so on).
    /// </summary>
    IReadOnlyList<ConnectionProfile> ConnectionProfiles { get; }
}
