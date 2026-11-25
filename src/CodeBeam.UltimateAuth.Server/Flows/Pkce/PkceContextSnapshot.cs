using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Flows;

/// <summary>
/// Immutable snapshot of relevant request and client context
/// captured at PKCE authorization time.
/// Used to ensure consistency and prevent flow confusion.
/// </summary>
public sealed class PkceContextSnapshot
{
    public PkceContextSnapshot(
        UAuthApplicationProfile applicationProfile,
        TenantKey tenant,
        string? redirectUri,
        DeviceContext device)
    {
        ApplicationProfile = applicationProfile;
        Tenant = tenant;
        RedirectUri = redirectUri;
        Device = device;
    }

    /// <summary>
    /// Application profile resolved at runtime (e.g. BlazorWasm).
    /// </summary>
    public UAuthApplicationProfile ApplicationProfile { get; }

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
    public DeviceContext Device { get; }
}
