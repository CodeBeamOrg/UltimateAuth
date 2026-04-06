namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class IdentityInfo
{
    public string Tenant { get; set; } = default!;

    public string? UserKey { get; set; }

    public DateTimeOffset? AuthenticatedAt { get; set; }
}
