using CodeBeam.UltimateAuth.Core.Domain;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Core.Models;

public sealed record TokenIssueContext<TUserId>
{
    public string? TenantId { get; init; }

    public TUserId UserId { get; init; } = default!;

    public AuthSessionId? SessionId { get; init; }

    /// <summary>
    /// Claims to embed into the access token (JWT or stored metadata).
    /// </summary>
    public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();

    /// <summary>
    /// Indicates whether a refresh token should be issued.
    /// </summary>
    public bool IssueRefreshToken { get; init; } = true;
}
