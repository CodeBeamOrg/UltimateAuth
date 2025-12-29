namespace CodeBeam.UltimateAuth.Tokens.EntityFrameworkCore;

internal sealed class RevokedTokenIdProjection
{
    public long Id { get; set; }
    public string? TenantId { get; set; }

    public string Jti { get; set; } = default!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset RevokedAt { get; set; }

    public byte[] RowVersion { get; set; } = default!;
}
