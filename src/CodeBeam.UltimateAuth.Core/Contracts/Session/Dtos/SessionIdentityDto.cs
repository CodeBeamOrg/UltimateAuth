namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class IdentityDto
{
    public string Tenant { get; set; } = default!;

    public string? UserKey { get; set; }

    public DateTimeOffset? AuthenticatedAt { get; set; }

}
