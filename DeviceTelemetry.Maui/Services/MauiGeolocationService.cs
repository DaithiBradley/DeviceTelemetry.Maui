namespace DeviceTelemetry.Maui.Services;

/// <summary>
/// MAUI implementation of the geolocation service interface.
/// </summary>
public sealed class MauiGeolocationService : Interfaces.IGeolocationService
{
    /// <inheritdoc />
    public Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancellationToken = default)
        => Geolocation.GetLocationAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<Location?> GetLastKnownLocationAsync()
        => Geolocation.GetLastKnownLocationAsync();
}

