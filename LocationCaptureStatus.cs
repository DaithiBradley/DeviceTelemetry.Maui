namespace DeviceTelemetry.Maui;

/// <summary>
/// Describes why location was or was not included in a telemetry capture.
/// </summary>
public enum LocationCaptureStatus
{
    /// <summary>
    /// Location was not requested because <see cref="CaptureOptions.IncludeLocation"/> is <see langword="false"/>.
    /// </summary>
    NotRequested,

    /// <summary>
    /// A current or last-known location was captured.
    /// </summary>
    Ok,

    /// <summary>
    /// Location permission was denied or not granted.
    /// </summary>
    Denied,

    /// <summary>
    /// Location services were unavailable or returned no fix.
    /// </summary>
    Unavailable,

    /// <summary>
    /// The location request timed out. A last-known location may still be present.
    /// </summary>
    TimedOut,

    /// <summary>
    /// The capture was cancelled. This value is not normally observed because cancellation throws <see cref="OperationCanceledException"/>.
    /// </summary>
    Cancelled
}
