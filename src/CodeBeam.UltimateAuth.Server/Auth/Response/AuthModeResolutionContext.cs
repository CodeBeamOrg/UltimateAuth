using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Server.Auth;

/// <summary>
/// Provides the context used to select an authentication mode.
/// </summary>
/// <remarks>
/// The application profile describes the client environment and is not proof of client identity.
/// Request headers, query values, and other client-supplied data must not be treated as trusted authentication or authorization evidence.
/// <br/><br/>
/// HttpContext may be unavailable for operations that are not associated with an HTTP request.
/// </remarks>
public sealed class AuthModeResolutionContext
{
    /// <summary>
    /// Gets the tenant for which the authentication mode is
    /// being resolved.
    /// </summary>
    public required TenantKey Tenant { get; init; }

    /// <summary>
    /// Gets the resolved application environment.
    /// This value must not be treated as authenticated identity.
    /// </summary>
    public required UAuthApplicationProfile ApplicationProfile { get; init; }

    /// <summary>
    /// Gets the current HTTP context, when available.
    /// </summary>
    /// <remarks>
    /// Endpoint metadata may be used to apply server-controlled
    /// routing policies. Request-supplied values are untrusted
    /// unless independently authenticated.
    /// </remarks>
    public HttpContext? HttpContext { get; init; }
}
