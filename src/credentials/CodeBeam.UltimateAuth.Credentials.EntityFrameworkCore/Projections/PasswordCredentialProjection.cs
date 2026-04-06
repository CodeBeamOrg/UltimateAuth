using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore;

public sealed class PasswordCredentialProjection
{
    public Guid Id { get; set; }

    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; set; }

    public PasswordHash SecretHash { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public DateTimeOffset? ExpiresAt { get; set; }

    public Guid SecurityStamp { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public string? Source { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long Version { get; set; }
}
