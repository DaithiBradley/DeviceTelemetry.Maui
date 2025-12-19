namespace DeviceTelemetry.Maui.Interfaces;

/// <summary>
/// Interface for permission checking and requesting service.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks the status of a permission.
    /// </summary>
    /// <typeparam name="T">The permission type.</typeparam>
    /// <returns>The current permission status.</returns>
    Task<PermissionStatus> CheckStatusAsync<T>() where T : Permissions.BasePermission, new();

    /// <summary>
    /// Requests a permission.
    /// </summary>
    /// <typeparam name="T">The permission type.</typeparam>
    /// <returns>The permission status after the request.</returns>
    Task<PermissionStatus> RequestAsync<T>() where T : Permissions.BasePermission, new();
}

