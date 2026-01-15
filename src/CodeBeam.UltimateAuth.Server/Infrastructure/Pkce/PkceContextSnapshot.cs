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
        string? tenantId,
        string? redirectUri,
        string? deviceId)
    {
        ClientProfile = clientProfile;
        TenantId = tenantId;
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
    public string? TenantId { get; }

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
