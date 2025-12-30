using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

// Add mapper class if needed (adding domain rules etc.)
internal sealed class RefreshTokenProjection
{
    public long Id { get; set; }               // Surrogate PK
    public string? TenantId { get; set; }

    public string TokenHash { get; set; } = default!;
    public AuthSessionId SessionId { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = default!;

    public bool IsRevoked => RevokedAt != null;
}
