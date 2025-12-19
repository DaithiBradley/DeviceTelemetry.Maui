namespace DeviceTelemetry.Maui.Services;

/// <summary>
/// MAUI implementation of the permission service interface.
/// </summary>
public sealed class MauiPermissionService : Interfaces.IPermissionService
{
    /// <inheritdoc />
    public Task<PermissionStatus> CheckStatusAsync<T>() where T : Permissions.BasePermission, new()
        => Permissions.CheckStatusAsync<T>();

    /// <inheritdoc />
    public Task<PermissionStatus> RequestAsync<T>() where T : Permissions.BasePermission, new()
        => Permissions.RequestAsync<T>();
}

