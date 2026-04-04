using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.EntityFrameworkCore;

public sealed class UserIdentifierProjection
{
    public Guid Id { get; set; }

    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; set; } = default!;

    public UserIdentifierType Type { get; set; }

    public string Value { get; set; } = default!;

    public string NormalizedValue { get; set; } = default!;

    public bool IsPrimary { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? VerifiedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long Version { get; set; }
}
