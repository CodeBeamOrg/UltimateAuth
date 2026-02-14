using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Represents the authenticated security snapshot returned by the server.
///
/// This object is immutable and represents a validated authentication state
/// at a specific point in time. It contains:
///
/// - Identity information (who the subject is)
/// - Authorization claims (what the subject can do)
///
/// This is not a live security context and must not be treated as a trust boundary.
/// Always validate on the server.
/// </summary>
public sealed record AuthStateSnapshot
{
    /// <summary>
    /// Authentication identity information such as UserKey, Tenant and authentication metadata.
    /// </summary>
    public required AuthIdentity Identity { get; init; }

    /// <summary>
    /// Authorization claims associated with the identity. These are security claims, not profile or display data.
    /// </summary>
    public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;
}
