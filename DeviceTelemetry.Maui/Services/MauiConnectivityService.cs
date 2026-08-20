namespace DeviceTelemetry.Maui.Services;

/// <summary>
/// MAUI implementation of <see cref="Interfaces.IConnectivityService"/>.
/// </summary>
public sealed class MauiConnectivityService : Interfaces.IConnectivityService
{
    /// <inheritdoc />
    public NetworkAccess NetworkAccess => Connectivity.Current.NetworkAccess;

    /// <inheritdoc />
    public IReadOnlyList<ConnectionProfile> ConnectionProfiles => [.. Connectivity.Current.ConnectionProfiles];
}
