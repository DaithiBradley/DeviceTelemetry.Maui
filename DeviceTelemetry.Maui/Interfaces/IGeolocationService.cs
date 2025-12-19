namespace DeviceTelemetry.Maui.Interfaces;

/// <summary>
/// Interface for geolocation service.
/// </summary>
public interface IGeolocationService
{
    /// <summary>
    /// Gets the current location asynchronously.
    /// </summary>
    /// <param name="request">The geolocation request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The location, or null if unavailable.</returns>
    Task<Location?> GetLocationAsync(GeolocationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the last known location.
    /// </summary>
    /// <returns>The last known location, or null if unavailable.</returns>
    Task<Location?> GetLastKnownLocationAsync();
}

