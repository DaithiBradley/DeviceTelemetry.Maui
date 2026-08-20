namespace DeviceTelemetry.Maui.Services;

/// <summary>
/// MAUI implementation of <see cref="Interfaces.IDeviceInfoService"/>.
/// </summary>
public sealed class MauiDeviceInfoService : Interfaces.IDeviceInfoService
{
    /// <inheritdoc />
    public string Model => DeviceInfo.Current.Model;

    /// <inheritdoc />
    public string Manufacturer => DeviceInfo.Current.Manufacturer;

    /// <inheritdoc />
    public string Name => DeviceInfo.Current.Name;

    /// <inheritdoc />
    public string VersionString => DeviceInfo.Current.VersionString;

    /// <inheritdoc />
    public string Platform => DeviceInfo.Current.Platform.ToString();

    /// <inheritdoc />
    public string Idiom => DeviceInfo.Current.Idiom.ToString();

    /// <inheritdoc />
    public string DeviceType => DeviceInfo.Current.DeviceType.ToString();
}
