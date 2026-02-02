using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

/// <summary>
/// Immutable snapshot of relevant request and client context
/// captured at PKCE authorization time.
/// Used to ensure consistency and prevent flow confusion.
/// </summary>
public sealed class PkceContextSnapshot
{
    public PkceContextSnapshot(
        UAuthClientProfile clientProfile,
        TenantKey tenant,
        string? redirectUri,
        string? deviceId)
    {
        ClientProfile = clientProfile;
        Tenant = tenant;
        RedirectUri = redirectUri;
        DeviceId = deviceId;
    }

    /// <summary>
    /// Client profile resolved at runtime (e.g. BlazorWasm).
    /// </summary>
    public UAuthClientProfile ClientProfile { get; }

    /// <summary>
    /// Tenant context at the time of authorization.
    /// </summary>
    public TenantKey Tenant { get; }

    /// <summary>
    /// Redirect URI used during authorization.
    /// Must match during completion.
    /// </summary>
    public string? RedirectUri { get; }

    /// <summary>
    /// Optional device binding identifier.
    /// Enables future hard-binding of PKCE flows to devices.
    /// </summary>
    public string? DeviceId { get; }
}
