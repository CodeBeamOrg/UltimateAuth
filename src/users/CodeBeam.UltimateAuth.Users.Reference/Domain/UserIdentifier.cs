using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed record UserIdentifier
{
    public TenantKey Tenant { get; set; }

    public UserKey UserKey { get; init; }

    public UserIdentifierType Type { get; init; } // Email, Phone, Username
    public string Value { get; set; } = default!;

    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
